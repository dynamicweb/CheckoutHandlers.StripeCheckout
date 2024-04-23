using Dynamicweb.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

/// <summary>
/// Send request to Stripe and get response operations.
/// </summary>
internal sealed class StripeRequest
{
    private static readonly string BaseAddress = "https://api.stripe.com";

    public string SecretKey { get; set; }

    public StripeRequest(string secretKey)
    {
        SecretKey = secretKey;
    }

    public static string SendRequest(string secretKey, CommandConfiguration configuration)
    {
        using (var messageHandler = GetMessageHandler())
        {
            using (var client = new HttpClient(messageHandler))
            {
                client.BaseAddress = new Uri(BaseAddress);
                client.Timeout = new TimeSpan(0, 0, 0, 90);
                if (!string.IsNullOrWhiteSpace(configuration.IdempotencyKey))
                    client.DefaultRequestHeaders.Add("Idempotency-Key", configuration.IdempotencyKey);

                string apiCommand = GetCommandLink(configuration.CommandType, configuration.OperatorId, configuration.OperatorSecondId);
                Task<HttpResponseMessage> requestTask = configuration.CommandType switch
                {
                    //POST
                    ApiCommand.CreateCustomer or
                    ApiCommand.CreatePaymentIntent or
                    ApiCommand.CapturePaymentIntent or
                    ApiCommand.CreatePaymentMethod or
                    ApiCommand.AttachPaymentMethod or
                    ApiCommand.DetachPaymentMethod or
                    ApiCommand.CreateRefund => client.PostAsync(apiCommand, new FormUrlEncodedContent(GetParameters(configuration.Parameters))),
                    //GET
                    ApiCommand.GetAllPaymentIntents or
                    ApiCommand.GetPaymentIntent or
                    ApiCommand.GetCustomerPaymentMethod or
                    ApiCommand.GetCustomer => client.GetAsync(apiCommand),
                    //DELETE
                    ApiCommand.DeleteCustomer => client.DeleteAsync(apiCommand),
                    _ => throw new NotSupportedException($"Unknown operation was used. The operation code: {configuration.CommandType}.")
                };

                try
                {
                    using (HttpResponseMessage response = requestTask.GetAwaiter().GetResult())
                    {
                        string data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        if (!response.IsSuccessStatusCode)
                        {
                            var content = TempConverter.Deserialize<Dictionary<string, JsonObject>>(data);

                            string errorMessage = "Unhandled exception. Operation failed.";
                            if (content.TryGetValue("error", out JsonObject error))
                            {
                                errorMessage = string.IsNullOrEmpty(Converter.ToString(error["code"]))
                                    ? Converter.ToString(error["message"])
                                    : $"Error code: {Converter.ToString(error["code"])}. {Converter.ToString(error["message"])}";
                            }

                            throw new Exception(errorMessage);
                        }

                        return data;
                    }
                }
                catch (HttpRequestException requestException)
                {
                    throw new Exception($"An error occurred during Stripe request. Error code: {requestException.StatusCode}");
                }
            }
        }

        HttpMessageHandler GetMessageHandler() => new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            PreAuthenticate = true,
            Credentials = new NetworkCredential(secretKey, string.Empty)
        };

        Dictionary<string, string> GetParameters(Dictionary<string, object> parameters)
        {
            if (parameters?.Count is null or 0)
                return new();

            return parameters.ToDictionary(x => x.Key, y => parameters[y.Key]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }
    }

    public string SendRequest(CommandConfiguration configuration) => SendRequest(SecretKey, configuration);

    private static string GetCommandLink(ApiCommand command, string operatorId, string operatorSecondId)
    {
        return command switch
        {
            ApiCommand.CreateCustomer => GetCommandLink("customers"),
            ApiCommand.GetCustomer => GetCommandLink($"customers/{operatorId}"),
            ApiCommand.DeleteCustomer => GetCommandLink($"customers/{operatorId}"),
            ApiCommand.CreatePaymentIntent or ApiCommand.GetAllPaymentIntents => GetCommandLink("payment_intents"),
            ApiCommand.GetPaymentIntent => GetCommandLink($"payment_intents/{operatorId}"),
            ApiCommand.CapturePaymentIntent => GetCommandLink($"payment_intents/{operatorId}/capture"),
            ApiCommand.CreatePaymentMethod => GetCommandLink("payment_methods"),
            ApiCommand.GetCustomerPaymentMethod => GetCommandLink($"customers/{operatorId}/payment_methods/{operatorSecondId}"),
            ApiCommand.AttachPaymentMethod => GetCommandLink($"payment_methods/{operatorId}/attach"),
            ApiCommand.DetachPaymentMethod => GetCommandLink($"payment_methods/{operatorId}/detach"),
            ApiCommand.CreateRefund => GetCommandLink("refunds"),
            _ => throw new NotSupportedException($"The api command is not supported. Command: {command}")
        };

        string GetCommandLink(string gateway) => $"{BaseAddress}/v1/{gateway}";
    }
}
