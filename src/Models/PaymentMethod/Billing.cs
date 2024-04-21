using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Customer;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

[DataContract]
public class Billing
{
    [DataMember(Name = "address")]
    public Address Address { get; set; }

    [DataMember(Name = "email")]
    public string Email { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "phone")]
    public string Phone { get; set; }
}
