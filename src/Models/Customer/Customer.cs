using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Customer;

//See full object description in the documentation. Link: https://docs.stripe.com/api/customers/object
[DataContract]
internal sealed class Customer
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "email")]
    public string Email { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "phone")]
    public string Phone { get; set; }

    [DataMember(Name = "balance")]
    public int Balance { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "delinquent")]
    public bool? Delinquent { get; set; }

    [DataMember(Name = "livemode")]
    public bool Livemode { get; set; }

    [DataMember(Name = "deleted")]
    public bool Deleted { get; set; }
}
