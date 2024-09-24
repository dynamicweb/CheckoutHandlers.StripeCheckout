using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.LineItems;

[DataContract]
internal sealed class PriceInformation
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "active")]
    public bool Active { get; set; }

    [DataMember(Name = "billing_scheme")]
    public string BillingScheme { get; set; }

    [DataMember(Name = "created")]
    public long Created { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "livemode")]
    public bool Livemode { get; set; }

    [DataMember(Name = "lookup_key")]
    public string LookupKey { get; set; }

    [DataMember(Name = "nickname")]
    public string Nickname { get; set; }

    [DataMember(Name = "product")]
    public string Product { get; set; }

    [DataMember(Name = "tax_behavior")]
    public string TaxBehavior { get; set; }

    [DataMember(Name = "tiers_mode")]
    public string TiersMode { get; set; }

    [DataMember(Name = "type")]
    public string PriceType { get; set; }

    [DataMember(Name = "unit_amount")]
    public int? UnitAmount { get; set; }

    [DataMember(Name = "unit_amount_decimal")]
    public string UnitAmountDecimal { get; set; }
}
