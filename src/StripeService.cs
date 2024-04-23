using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;
using Dynamicweb.Ecommerce.ChecskoutHandlers.StripeCheckout.Models.Customer;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal class StripeService
{
    public string SecretKey { get; set; }

    public StripeService(string secretKey)
    {
        SecretKey = secretKey;
    }

    public PaymentIntent GetPaymentIntent(string paymentIntentId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetPaymentIntent,
            OperatorId = paymentIntentId
        });

        return TempConverter.Deserialize<PaymentIntent>(response);
    }

    public IEnumerable<PaymentIntent> GetAllPaymentIntents()
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetAllPaymentIntents
        });

        return TempConverter.Deserialize<PaymentIntents>(response)?.Data;
    }

    public Customer GetCustomer(string customerId)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.GetCustomer,
            OperatorId = customerId
        });

        return TempConverter.Deserialize<Customer>(response);
    }

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

    public PaymentMethod CreatePaymentMethod(Dictionary<string, object> parameters)
    {
        string response = StripeRequest.SendRequest(SecretKey, new()
        {
            CommandType = ApiCommand.CreatePaymentMethod,
            Parameters = parameters
        });
        return TempConverter.Deserialize<PaymentMethod>(response);
    }

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

    public void AttachPaymentMethod(string paymentMethodId, string customerId) => StripeRequest.SendRequest(SecretKey, new()
    {
        CommandType = ApiCommand.AttachPaymentMethod,
        OperatorId = paymentMethodId,
        Parameters = new Dictionary<string, object>
        {
            ["customer"] = customerId
        }
    });

    public void DetachPaymentMethod(string paymentMethodId) => StripeRequest.SendRequest(SecretKey, new()
    {
        CommandType = ApiCommand.DetachPaymentMethod,
        OperatorId = paymentMethodId
    });

    public void DeleteCustomer(string customerId) => StripeRequest.SendRequest(SecretKey, new()
    {
        CommandType = ApiCommand.DeleteCustomer,
        OperatorId = customerId
    });

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
