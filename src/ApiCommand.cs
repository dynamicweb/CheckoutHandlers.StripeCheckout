namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal enum ApiCommand
{
    /// <summary>
    /// Creates a customer of your business. Use it to create recurring charges and track payments that belong to the same customer.
    /// POST /customers
    /// </summary>
    CreateCustomer,

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
    /// Creates new refund. Refund objects allow you to refund a previously created charge that isn’t refunded yet. Funds are refunded to the credit or debit card that’s initially charged.
    /// POST /refunds
    /// </summary>
    CreateRefund,

    /// <summary>
    /// Attaches a Source object to a Customer. The source must be in a chargeable or pending state. Source objects allow you to accept a variety of payment methods. They represent a customer’s payment instrument.
    /// POST /customers/{operatorId}/sources
    /// </summary>
    AttachSource,

    /// <summary>
    /// Detaches a Source object from a Customer. The status of a source is changed to consumed when it is detached and it can no longer be used to create a charge.
    /// DELETE /customers/{operatorId}/sources/{operatorSecondId}
    /// </summary>
    DetachSource,
}
