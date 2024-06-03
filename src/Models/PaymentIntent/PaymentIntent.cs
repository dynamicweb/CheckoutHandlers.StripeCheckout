using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

//See full object description in the documentation. Link: https://docs.stripe.com/api/payment_intents/object
[DataContract]
internal sealed class PaymentIntent
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "amount")]
    public int Amount { get; set; }

    [DataMember(Name = "client_secret")]
    public string ClientSecret { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "customer")]
    public string CustomerId { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "payment_method")]
    public string PaymentMethodId { get; set; }

    [DataMember(Name = "receipt_email")]
    public string ReceiptEmail { get; set; }

    [DataMember(Name = "status")]
    public PaymentIntentStatus Status { get; set; }

    [DataMember(Name = "amount_capturable")]
    public int AmountCapturable { get; set; }

    [DataMember(Name = "amount_received")]
    public int AmountReceived { get; set; }

    [DataMember(Name = "livemode")]
    public bool Livemode { get; set; }

    [DataMember(Name = "invoice")]
    public string Invoice { get; set; }

    [DataMember(Name = "next_action")]
    public NextAction NextAction { get; set; }

    [DataMember(Name = "cancellation_reason")]
    public string CancellationReason { get; set; }

    [DataMember(Name = "last_payment_error")]
    public StripeError LastPaymentError { get; set; }
}
