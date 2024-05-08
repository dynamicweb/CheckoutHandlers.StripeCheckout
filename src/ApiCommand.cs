namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal enum ApiCommand
{
    /// <summary>
    /// Creates a customer of your business. Use it to create recurring charges and track payments that belong to the same customer.
    /// POST /customers
    /// </summary>
    CreateCustomer,

    /// <summary>
    /// Retrieves a Customer object.
    /// GET /customers/{operatorId}
    /// </summary>
    GetCustomer,

    /// <summary>
    /// Permanently deletes a customer. It cannot be undone. Also immediately cancels any active subscriptions on the customer.
    /// DELETE /customers/{operatorId}
    /// </summary>
    DeleteCustomer,

    /// <summary>
    /// Creates a PaymentIntent object. A PaymentIntent guides you through the process of collecting a payment from your customer.
    /// POST /payment_intents
    /// </summary>
    CreatePaymentIntent,

    /// <summary>
    /// Retrieves the details of a PaymentIntent that has previously been created.
    /// GET /payment_intents/{operatorId}
    /// </summary>
    GetPaymentIntent,

    /// <summary>
    /// Returns a list of PaymentIntents
    /// GET /payment_intents
    /// </summary>
    GetAllPaymentIntents,

    /// <summary>
    /// Captures the funds of an existing uncaptured PaymentIntent when its status is requires_capture.
    /// POST /payment_intents/{operatorId}/capture
    /// </summary>
    CapturePaymentIntent,

    /// <summary>
    /// Cancels the PaymentIntent. You can cancel a PaymentIntent object when it’s in one of these statuses: requires_payment_method, requires_capture, requires_confirmation, requires_action or, in rare cases, processing.
    /// POST /payment_intents/{operatorId}/cancel
    /// </summary>
    CancelPaymentIntent,

    /// <summary>
    /// Creates a PaymentMethod object.
    /// POST /payment_methods
    /// </summary>
    CreatePaymentMethod,

    /// <summary>
    /// Retrieves a PaymentMethod object attached to the StripeAccount. To retrieve a payment method attached to a Customer, you should use Retrieve a Customer’s PaymentMethods
    /// POST /payment_methods/{operatorId}
    /// </summary>
    GetPaymentMethod,

    /// <summary>
    /// Retrieves a PaymentMethod object for a given Customer.
    /// GET /customers/{operatorId}/payment_methods/{operatorSecondId}
    /// </summary>
    GetCustomerPaymentMethod,

    /// <summary>
    /// Attaches a PaymentMethod object to a Customer.
    /// POST /payment_methods/{operatorId}/attach
    /// </summary>
    AttachPaymentMethod,

    /// <summary>
    /// Detaches a PaymentMethod object from a Customer. After a PaymentMethod is detached, it can no longer be used for a payment or re-attached to a Customer.
    /// POST /payment_methods/{operatorId}/detach
    /// </summary>
    DetachPaymentMethod,

    /// <summary>
    /// Creates new refund. Refund objects allow you to refund a previously created charge that isn’t refunded yet. Funds are refunded to the credit or debit card that’s initially charged.
    /// POST /refunds
    /// </summary>
    CreateRefund
}
