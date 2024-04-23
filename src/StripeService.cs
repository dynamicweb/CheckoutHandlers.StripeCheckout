using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;
using Dynamicweb.Ecommerce.ChecskoutHandlers.StripeCheckout.Models.Customer;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

/// <summary>
/// The service to interact with Stripe API
/// </summary>
internal class StripeService
{
    public string SecretKey { get; set; }

    public StripeService(string secretKey)
    {
        SecretKey = secretKey;
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

        return TempConverter.Deserialize<PaymentIntent>(response);
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

        return TempConverter.Deserialize<PaymentIntents>(response)?.Data;
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

        return TempConverter.Deserialize<Customer>(response);
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

        return TempConverter.Deserialize<Customer>(response);
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

        return TempConverter.Deserialize<PaymentIntent>(response);
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

        return TempConverter.Deserialize<PaymentIntent>(response);
    }

    /// <summary>
    /// Creates a PaymentMethod. Should be used for recurring orders only.
    /// </summary>
    /// <param name="parameters">Parameters</param>
    public PaymentMethod CreatePaymentMethod(Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreatePaymentMethod,
            Parameters = parameters
        });
        return TempConverter.Deserialize<PaymentMethod>(response);
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
        return TempConverter.Deserialize<PaymentMethod>(response);
    }

    /// <summary>
    /// Attaches a PaymentMethod object to a Customer.
    /// POST /payment_methods/{paymentMethodId}/attach
    /// </summary>
    /// <param name="paymentMethodId">Payment method id</param>
    /// <param name="customerId">Customer id</param>
    public void AttachPaymentMethod(string paymentMethodId, string customerId) => StripeRequest.SendRequest(SecretKey, new()
    {
        CommandType = ApiCommand.AttachPaymentMethod,
        OperatorId = paymentMethodId,
        Parameters = new Dictionary<string, object>
        {
            ["customer"] = customerId
        }
    });

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

        return TempConverter.Deserialize<Refund>(response);
    }

}
