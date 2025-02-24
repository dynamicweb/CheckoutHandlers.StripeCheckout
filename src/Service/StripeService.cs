using Dynamicweb.Configuration;
using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Customer;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Error;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;
using Dynamicweb.Ecommerce.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Service;

/// <summary>
/// The service to interact with Stripe API
/// </summary>
internal sealed class StripeService
{
    public string SecretKey { get; set; }

    public StripeService(string secretKey)
    {
        SecretKey = secretKey;
    }

    /// <summary>
    /// Gets the error data for printing.
    /// </summary>
    /// <param name="error">Error response data</param>
    public static string GetErrorMessage(StripeError error)
    {
        var errorMessage = new StringBuilder(error.ErrorType switch
        {
            StripeErrorType.ApiError => "Api error.",
            StripeErrorType.IdempotencyError => "Idempotency error.",
            StripeErrorType.InvalidRequestError => "Invalid request error.",
            StripeErrorType.CardError => "Card error.",
            _ => "Unhandled exception."
        });

        if (!string.IsNullOrEmpty(error.Code))
            errorMessage.Append($" Error code: {error.Code}.");
        if (!string.IsNullOrEmpty(error.DeclineCode))
            errorMessage.Append($" Decline code: {error.DeclineCode}.");
        if (!string.IsNullOrEmpty(error.Message))
            errorMessage.Append($@" Message: ""{error.Message}"".");
        if (!string.IsNullOrEmpty(error.ParameterName))
            errorMessage.Append($" Parameter name: {error.ParameterName}.");
        if (!string.IsNullOrEmpty(error.LogUrl))
            errorMessage.Append($" Log url: {error.LogUrl}");

        return errorMessage.ToString();
    }

    /// <summary>
    /// Retrieves the details of a PaymentIntent that has previously been created.
    /// GET /payment_intents/{paymentIntentId}
    /// </summary>
    /// <param name="paymentIntentId">The id of payment intent</param>
    public PaymentIntent GetPaymentIntent(string paymentIntentId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetPaymentIntent,
            OperatorId = paymentIntentId
        });

        return Converter.Deserialize<PaymentIntent>(response);
    }

    /// <summary>
    /// Returns a list of PaymentIntents
    /// GET /payment_intents
    /// </summary>
    public IEnumerable<PaymentIntent> GetAllPaymentIntents()
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetAllPaymentIntents
        });

        return Converter.Deserialize<PaymentIntents>(response)?.Data;
    }

    /// <summary>
    /// Retrieves a Customer object.
    /// GET /customers/{customerId}
    /// </summary>
    /// <param name="customerId">The customer id</param>
    public Customer GetCustomer(string customerId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetCustomer,
            OperatorId = customerId
        });

        return Converter.Deserialize<Customer>(response);
    }

    /// <summary>
    /// Creates a customer.
    /// </summary>
    /// <param name="email">Required - customer email.</param>
    /// <param name="userName">Optional - customer name.</param>
    public Customer CreateCustomer(string email, string userName)
    {
        var parameters = new Dictionary<string, object>
        {
            ["email"] = email
        };
        if (!string.IsNullOrEmpty(userName))
            parameters["name"] = userName;

        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreateCustomer,
            Parameters = parameters
        });

        return Converter.Deserialize<Customer>(response);
    }

    /// <summary>
    /// Updates a customer
    /// </summary>
    /// <param name="customerId">Customer</param>
    /// <param name="parameters">Parameters</param>
    /// <returns></returns>
    public Customer UpdateCustomer(string customerId, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.UpdateCustomer,
            OperatorId = customerId,
            Parameters = parameters
        });

        return Converter.Deserialize<Customer>(response);
    }

    /// <summary>
    /// Creates a PaymentIntent object. A PaymentIntent guides you through the process of collecting a payment from your customer.
    /// POST /payment_intents
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key</param>
    /// <param name="parameters">Parameters</param>
    public PaymentIntent CreatePaymentIntent(string idempotencyKey, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreatePaymentIntent,
            Parameters = parameters,
            IdempotencyKey = idempotencyKey
        });

        return Converter.Deserialize<PaymentIntent>(response);
    }

    /// <summary>
    /// Creates a PaymentIntent based on saved card data and order.
    /// POST /payment_intents
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key</param>
    /// <param name="order">The order</param>
    /// <param name="options">Creation options</param>
    public PaymentIntent CreateOffPaymentIntent(string idempotencyKey, Order order, PaymentIntentCreateOptions options)
    {
        var parameters = new Dictionary<string, object>
        {
            { "amount", order.Price.PricePIP },
            { "currency", order.CurrencyCode },
            { "description", order.Id },
            { "customer",  options.CustomerId },
            { "capture_method", options.AutomaticCapture ? "automatic_async" : "manual" },
            { "confirm", true },
            { "payment_method", options.PaymentMethodId },
            { "off_session", true }
        };

        SetShippingParameters(order, parameters, "shipping");

        return CreatePaymentIntent(idempotencyKey, parameters);
    }

    /// <summary>
    /// Captures the funds of an existing uncaptured PaymentIntent when its status is requires_capture.
    /// POST /payment_intents/{paymentIntentId}/capture
    /// </summary>
    /// <param name="paymentIntentId">Payment intent id.</param>
    /// <param name="parameters">Parameters.</param>
    public PaymentIntent CapturePaymentIntent(string paymentIntentId, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CapturePaymentIntent,
            OperatorId = paymentIntentId,
            Parameters = parameters
        });

        return Converter.Deserialize<PaymentIntent>(response);
    }

    /// <summary>
    /// Cancels the PaymentIntent. You can cancel a PaymentIntent object when it’s in one of these statuses: requires_payment_method, requires_capture, requires_confirmation, requires_action or, in rare cases, processing.
    /// After it’s canceled, no additional charges are made by the PaymentIntent and any operations on the PaymentIntent fail with an error. 
    /// For PaymentIntents with a status of requires_capture, the remaining amount_capturable is automatically refunded.
    /// POST /payment_intents/{operatorId}/cancel
    /// </summary>
    /// <param name="paymentIntentId">Payment intent id.</param>
    /// <param name="cancellationReason">Cancellation reason. By default is "requested_by_customer".</param>
    public PaymentIntent CancelPaymentIntent(string paymentIntentId, string cancellationReason = "requested_by_customer")
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CancelPaymentIntent,
            OperatorId = paymentIntentId,
            Parameters = new Dictionary<string, object>
            {
                ["cancellation_reason"] = cancellationReason
            }
        });

        return Converter.Deserialize<PaymentIntent>(response);
    }

    /// <summary>
    /// Creates a SetupIntent object. After you create the SetupIntent, attach a payment method and confirm it to collect any required permissions to charge the payment method later.
    /// POST /setup_intents
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key</param>
    /// <param name="parameters">Parameters</param>
    public SetupIntent CreateSetupIntent(string idempotencyKey, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreateSetupIntent,
            Parameters = parameters,
            IdempotencyKey = idempotencyKey
        });

        return Converter.Deserialize<SetupIntent>(response);
    }


    /// <summary>
    /// Creates a SetupIntent based on saved card data.
    /// POST /setup_intents
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key</param>
    /// <param name="order">The order</param>
    /// <param name="options">Creation options</param>
    public SetupIntent CreateOffSetupIntent(string idempotencyKey, PaymentIntentCreateOptions options)
    {
        var parameters = new Dictionary<string, object>()
        {
            ["confirm"] = true,
            ["customer"] = options.CustomerId,
            ["payment_method"] = options.PaymentMethodId,
            ["usage"] = "off_session",
            ["automatic_payment_methods[enabled]"] = true,
            ["automatic_payment_methods[allow_redirects]"] = "never",
        };

        return CreateSetupIntent(idempotencyKey, parameters);
    }

    /// <summary>
    /// Gets a SetupIntent object. Retrieves the details of a SetupIntent that has previously been created.
    /// GET /setup_intents/{operatorId}
    /// </summary>
    /// <param name="setupIntentId">Setup intent id</param>
    public SetupIntent GetSetupIntent(string setupIntentId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetSetupIntent,
            OperatorId = setupIntentId
        });

        return Converter.Deserialize<SetupIntent>(response);
    }

    /// <summary>
    /// Confirms a SetupIntent object. Confirms that your customer intends to set up the current or provided payment method.
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key</param>
    /// <param name="parameters">Parameters</param>
    /// <returns></returns>
    public SetupIntent ConfirmSetupIntent(string idempotencyKey, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.ConfirmSetupIntent,
            Parameters = parameters,
            IdempotencyKey = idempotencyKey
        });

        return Converter.Deserialize<SetupIntent>(response);
    }

    /// <summary>
    /// Creates a PaymentMethod. Should be used for recurring orders only.
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key</param>
    /// <param name="parameters">Parameters</param>
    public PaymentMethod CreatePaymentMethod(string idempotencyKey, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreatePaymentMethod,
            Parameters = parameters,
            IdempotencyKey = idempotencyKey
        });
        return Converter.Deserialize<PaymentMethod>(response);
    }

    /// <summary>
    /// Retrieves a PaymentMethod object for a given Customer.
    /// GET /customers/{customerId}/payment_methods/{paymentMethodId}
    /// </summary>
    /// <param name="customerId">Customer id</param>
    /// <param name="paymentMethodId">Payment method id</param>
    public PaymentMethod GetPaymentMethod(string customerId, string paymentMethodId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetCustomerPaymentMethod,
            OperatorId = customerId,
            OperatorSecondId = paymentMethodId
        });
        return Converter.Deserialize<PaymentMethod>(response);
    }

    /// <summary>
    /// Retrieves a PaymentMethod object attached to the StripeAccount. To retrieve a payment method attached to a Customer, use <see cref="GetPaymentMethod(string, string)"/>
    /// POST /payment_methods/{operatorId}
    /// </summary>
    /// <param name="paymentMethodId">Payment method id</param>
    public PaymentMethod GetPaymentMethod(string paymentMethodId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetPaymentMethod,
            OperatorId = paymentMethodId,
        });
        return Converter.Deserialize<PaymentMethod>(response);
    }

    /// <summary>
    /// Attaches a PaymentMethod object to a Customer.
    /// POST /payment_methods/{paymentMethodId}/attach
    /// </summary>
    /// <param name="paymentMethodId">Payment method id</param>
    /// <param name="customerId">Customer id</param>
    public PaymentMethod AttachPaymentMethod(string paymentMethodId, string customerId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.AttachPaymentMethod,
            OperatorId = paymentMethodId,
            Parameters = new Dictionary<string, object>
            {
                ["customer"] = customerId
            }
        });
        return Converter.Deserialize<PaymentMethod>(response);
    }

    /// <summary>
    /// Detaches a PaymentMethod object from a Customer. After a PaymentMethod is detached, it can no longer be used for a payment or re-attached to a Customer.
    /// POST /payment_methods/{paymentMethodId}/detach
    /// </summary>
    /// <param name="paymentMethodId">Payment method id.</param>
    public void DetachPaymentMethod(string paymentMethodId) => StripeRequest.SendRequest(SecretKey, new()
    {
        CommandType = ApiCommand.DetachPaymentMethod,
        OperatorId = paymentMethodId
    });

    /// <summary>
    /// Permanently deletes a customer. It cannot be undone. Also immediately cancels any active subscriptions on the customer.
    /// DELETE /customers/{customerId}
    /// </summary>
    /// <param name="customerId">Customer id.</param>
    public void DeleteCustomer(string customerId) => StripeRequest.SendRequest(SecretKey, new()
    {
        CommandType = ApiCommand.DeleteCustomer,
        OperatorId = customerId
    });

    /// <summary>
    /// Creates new refund. Refund objects allow you to refund a previously created charge that isn’t refunded yet. Funds are refunded to the credit or debit card that’s initially charged.
    /// POST /refunds
    /// </summary>
    /// <param name="parameters">Parameters</param>
    public Refund CreateRefund(Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreateRefund,
            Parameters = parameters
        });

        return Converter.Deserialize<Refund>(response);
    }

    /// <summary>
    /// Creates new Session for checkout. 
    /// POST /checkout/sessions
    /// </summary>
    /// <param name="parameters">Parameters</param>
    public Session CreateSession(string idempotencyKey, Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreateSession,
            Parameters = parameters,
            IdempotencyKey = idempotencyKey
        });

        return Converter.Deserialize<Session>(response);
    }

    /// <summary>
    /// Creates new Session for checkout. It uses the Order data to form the parameters. 
    /// POST /checkout/sessions
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="options">Create options</param>
    public Session CreateSession(string idempotencyKey, Order order, SessionCreateOptions options)
    {
        var parameters = new Dictionary<string, object>
        {
            ["mode"] = options.Mode is SessionMode.Setup ? "setup" : "payment",
            ["locale"] = options.Language,
            ["client_reference_id"] = order.AutoId,
            [options.EmbeddedForm ? "return_url" : "success_url"] = options.CompleteUrl,
            ["currency"] = order.CurrencyCode,
        };

        SetPaymentMethodParameters(parameters, options);

        if (options.Mode is SessionMode.Payment)
        {
            parameters["payment_intent_data[capture_method]"] = options.AutomaticCapture ? "automatic_async" : "manual";
            parameters["payment_intent_data[description]"] = order.Id;

            SetProductsParameters(parameters, order);
            SetShippingParameters(order, parameters, "payment_intent_data[shipping]");
        }
        else
            parameters["setup_intent_data[description]"] = $"Order id: {order.Id}. Total amount: {order.Price.PriceWithVATFormatted}";

        if (options.EmbeddedForm)
            parameters["ui_mode"] = "embedded";

        if (!string.IsNullOrWhiteSpace(options.CustomerId))
            parameters["customer"] = options.CustomerId;
        else
        {
            parameters["customer_email"] = order.CustomerEmail;
            parameters["customer_creation"] = options.SavePaymentMethod ? "always" : "if_required";
        }

        return CreateSession(idempotencyKey, parameters);
    }

    private void SetProductsParameters(Dictionary<string, object> parameters, Order order)
    {
        string itemParameter = $"line_items[0]";
        parameters[$"{itemParameter}[quantity]"] = 1;

        string priceParameter = $"{itemParameter}[price_data]";
        parameters[$"{priceParameter}[currency]"] = order.CurrencyCode;
        parameters[$"{priceParameter}[unit_amount]"] = order.Price.PricePIP;

        string productParameter = $"{priceParameter}[product_data]";
        parameters[$"{productParameter}[name]"] = "Total amount";
    }

    private void SetPaymentMethodParameters(Dictionary<string, object> parameters, SessionCreateOptions options)
    {
        if (options.SavePaymentMethod && options.Mode is SessionMode.Payment)
        {
            string paymentOptionsParameter = "payment_method_options";
            parameters[$"{paymentOptionsParameter}[card][setup_future_usage]"] = "off_session";
        }

        if (options.PaymentMethods?.Any() is true && !options.AutomaticPaymentMethods)
        {
            for (int i = 0; i < options.PaymentMethods.Count(); i++)
                parameters[$"payment_method_types[{i}]"] = options.PaymentMethods.ElementAt(i);
        }
    }

    /// <summary>
    /// Gets the session by session id.
    /// GET /checkout/sessions/{operatorId}
    /// </summary>
    /// <param name="parameters">Parameters</param>
    public Session GetSession(string sessionId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetSession,
            OperatorId = sessionId
        });

        return Converter.Deserialize<Session>(response);
    }

    private void SetShippingParameters(Order order, Dictionary<string, object> parameters, string shippingParameter)
    {
        if (string.IsNullOrWhiteSpace(GetAddress()) && SystemConfiguration.Instance.GetBoolean("/Globalsettings/Ecom/Cart/CopyCustomerFieldsToDelivery"))
            Services.Carts.CopyCustomerFieldsToDelivery(order);

        //name is required parameter, so we should fill it by something
        string recipientName = !string.IsNullOrWhiteSpace(order.DeliveryName) ? order.DeliveryName : order.DeliveryMiddleName;
        if (string.IsNullOrWhiteSpace(recipientName))
            recipientName = "Recipient";

        parameters[$"{shippingParameter}[name]"] = recipientName;
        parameters[$"{shippingParameter}[phone]"] = order.DeliveryPhone;

        string addressParameter = $"{shippingParameter}[address]";

        //line 1 is required parameter
        parameters[$"{addressParameter}[line1]"] = GetAddress();
        parameters[$"{addressParameter}[line2]"] = order.DeliveryAddress2;
        parameters[$"{addressParameter}[state]"] = order.DeliveryRegion;
        parameters[$"{addressParameter}[city]"] = order.DeliveryCity;
        parameters[$"{addressParameter}[country]"] = order.DeliveryCountryCode;
        parameters[$"{addressParameter}[postal_code]"] = order.DeliveryZip;

        string GetAddress() => !string.IsNullOrWhiteSpace(order.DeliveryAddress) ? order.DeliveryAddress : order.DeliveryAddress2;
    }
}
