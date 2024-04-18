using Dynamicweb.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

/// <summary>
/// Send request to Stripe and get response operations.
/// </summary>
internal class StripeRequest
{
    private static readonly string BaseAddress = "https://api.stripe.com";

    public string SecretKey { get; set; }

    public StripeRequest(string secretKey)
    {
        SecretKey = secretKey;
    }

    public static Dictionary<string, object> SendRequest(string secretKey, CommandConfiguration configuration)
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
                    ApiCommand.CreateRefund or
                    ApiCommand.AttachSource => client.PostAsync(apiCommand, new FormUrlEncodedContent(GetParameters(configuration.Parameters))),
                    //GET
                    ApiCommand.GetAllPaymentIntents or
                    ApiCommand.GetPaymentIntent => client.GetAsync(apiCommand),
                    //DELETE
                    ApiCommand.DeleteCustomer or
                    ApiCommand.DetachSource => client.DeleteAsync(apiCommand),
                    _ => throw new NotSupportedException($"Unknown operation was used. The operation code: {configuration.CommandType}.")
                };

                try
                {
                    using (HttpResponseMessage response = requestTask.GetAwaiter().GetResult())
                    {
                        string data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        var content = Converter.Deserialize<Dictionary<string, object>>(data);
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorMessage = "Unhandled exception. Operation failed.";
                            if (content.TryGetValue("error", out dynamic error))
                            {
                                errorMessage = string.IsNullOrEmpty(Converter.ToString(error["code"]))
                                    ? Converter.ToString(error["message"])
                                    : $"Error code: {Converter.ToString(error["code"])}. {Converter.ToString(error["message"])}";
                            }

                            throw new Exception(errorMessage);
                        }

                        return content;
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
    }

    public Dictionary<string, object> SendRequest(CommandConfiguration configuration) => SendRequest(SecretKey, configuration);

    private static Dictionary<string, string> GetParameters(Dictionary<string, object> parameters)
    {
        if (parameters?.Count is null or 0)
            return new();

        var formParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, object value) in parameters)
            formParameters[key] = WebUtility.UrlEncode(value?.ToString() ?? string.Empty);

        return formParameters;
    }

    private static string GetCommandLink(ApiCommand command, string operatorId, string operatorSecondId)
    {
        return command switch
        {
            ApiCommand.CreateCustomer => GetCommandLink("customers"),
            ApiCommand.DeleteCustomer => GetCommandLink($"customers/{operatorId}"),
            ApiCommand.CreatePaymentIntent or ApiCommand.GetAllPaymentIntents => GetCommandLink("payment_intents"),
            ApiCommand.GetPaymentIntent => GetCommandLink($"payment_intents/{operatorId}"),
            ApiCommand.CapturePaymentIntent => GetCommandLink($"payment_intents/{operatorId}/capture"),
            ApiCommand.CreateRefund => GetCommandLink("refunds"),
            ApiCommand.AttachSource => GetCommandLink($"customers/{operatorId}/sources"),
            ApiCommand.DetachSource => GetCommandLink($"customers/{operatorId}/sources/{operatorSecondId}"),
            _ => string.Empty
        };

        string GetCommandLink(string gateway) => $"{BaseAddress}/v1/{gateway}";
    }
}
