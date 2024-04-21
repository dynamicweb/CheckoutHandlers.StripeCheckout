using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentMethod;

[DataContract]
public sealed class Card
{
    [DataMember(Name = "brand")]
    public string Brand { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "exp_month")]
    public int ExpMonth { get; set; }

    [DataMember(Name = "exp_year")]
    public int ExpYear { get; set; }

    [DataMember(Name = "last4")]
    public string Last4 { get; set; }

    [DataMember(Name = "funding")]
    public string Funding { get; set; }
}
