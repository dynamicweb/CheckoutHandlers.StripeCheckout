using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Source;

[DataContract]
public class Source
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "account")]
    public string Account { get; set; }

    [DataMember(Name = "amount")]
    public int Amount { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "customer")]
    public string Customer { get; set; }

    [DataMember(Name = "brand")]
    public string Brand { get; set; }

    [DataMember(Name = "last4")]
    public string Last4 { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }
}
