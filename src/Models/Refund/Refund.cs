using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;

//See full object description in the documentation. Link: https://docs.stripe.com/api/refunds/object
[DataContract]
internal sealed class Refund
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "amount")]
    public int Amount { get; set; }

    [DataMember(Name = "charge")]
    public string Charge { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "payment_intent")]
    public string PaymentIntentId { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "balance_transaction")]
    public string BalanceTransaction { get; set; }

    [DataMember(Name = "failure_balance_transaction")]
    public string FailureBalanceTransaction { get; set; }

    [DataMember(Name = "failure_reason")]
    public string FailureReason { get; set; }
}
