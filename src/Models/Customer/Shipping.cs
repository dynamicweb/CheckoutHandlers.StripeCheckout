using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Customer;

[DataContract]
public class Shipping
{
    [DataMember(Name = "address")]
    public Address Address { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "phone")]
    public string Phone { get; set; }    
}
