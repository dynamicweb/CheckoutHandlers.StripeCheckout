using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Service;

/// <summary>
/// Send request to Stripe and get response operations.
/// </summary>
internal static class StripeRequest
{
	private static readonly string BaseAddress = "https://api.stripe.com";

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
					ApiCommand.UpdateCustomer or
					ApiCommand.CreatePaymentIntent or
					ApiCommand.CapturePaymentIntent or
					ApiCommand.CancelPaymentIntent or
					ApiCommand.CreateSetupIntent or
					ApiCommand.ConfirmSetupIntent or
					ApiCommand.CreatePaymentMethod or
					ApiCommand.AttachPaymentMethod or
					ApiCommand.DetachPaymentMethod or
					ApiCommand.CreateRefund or
					ApiCommand.CreateSession => client.PostAsync(apiCommand, new FormUrlEncodedContent(GetParameters(configuration.Parameters))),
					//GET
					ApiCommand.GetAllPaymentIntents or
					ApiCommand.GetPaymentIntent or
					ApiCommand.GetSetupIntent or
					ApiCommand.GetPaymentMethod or
					ApiCommand.GetCustomerPaymentMethod or
					ApiCommand.GetCustomer or
					ApiCommand.GetSession => client.GetAsync(apiCommand),
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
							string errorMessage = "Unhandled exception. Operation failed.";

							var errorResponse = Converter.Deserialize<StripeErrorResponse>(data);
							if (errorResponse.Error is not null)
								errorMessage = StripeService.GetErrorMessage(errorResponse.Error);

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

			Dictionary<string, string> convertedParameters = parameters.ToDictionary(x => x.Key, y => parameters[y.Key]?.ToString() ?? string.Empty);

			return convertedParameters.Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)).ToDictionary(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static string GetCommandLink(ApiCommand command, string operatorId, string operatorSecondId)
	{
		return command switch
		{
			ApiCommand.CreateCustomer => GetCommandLink("customers"),
			ApiCommand.GetCustomer or ApiCommand.UpdateCustomer => GetCommandLink($"customers/{operatorId}"),
			ApiCommand.DeleteCustomer => GetCommandLink($"customers/{operatorId}"),
			ApiCommand.CreatePaymentIntent or ApiCommand.GetAllPaymentIntents => GetCommandLink("payment_intents"),
			ApiCommand.GetPaymentIntent => GetCommandLink($"payment_intents/{operatorId}"),
			ApiCommand.CapturePaymentIntent => GetCommandLink($"payment_intents/{operatorId}/capture"),
			ApiCommand.CancelPaymentIntent => GetCommandLink($"payment_intents/{operatorId}/cancel"),
			ApiCommand.CreateSetupIntent => GetCommandLink("setup_intents"),
			ApiCommand.GetSetupIntent => GetCommandLink($"setup_intents/{operatorId}"),
			ApiCommand.ConfirmSetupIntent => GetCommandLink($"/setup_intents/{operatorId}/confirm"),
			ApiCommand.CreatePaymentMethod => GetCommandLink("payment_methods"),
			ApiCommand.GetPaymentMethod => GetCommandLink($"payment_methods/{operatorId}"),
			ApiCommand.GetCustomerPaymentMethod => GetCommandLink($"customers/{operatorId}/payment_methods/{operatorSecondId}"),
			ApiCommand.AttachPaymentMethod => GetCommandLink($"payment_methods/{operatorId}/attach"),
			ApiCommand.DetachPaymentMethod => GetCommandLink($"payment_methods/{operatorId}/detach"),
			ApiCommand.CreateRefund => GetCommandLink("refunds"),
			ApiCommand.CreateSession => GetCommandLink("checkout/sessions"),
			ApiCommand.GetSession => GetCommandLink($"checkout/sessions/{operatorId}"),
			_ => throw new NotSupportedException($"The api command is not supported. Command: {command}")
		};

		string GetCommandLink(string gateway) => $"{BaseAddress}/v1/{gateway}";
	}
}
