using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Customer;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Source;
using System;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.ChecskoutHandlers.StripeCheckout.Models.Customer;

//See full object description in the documentation. Link: https://docs.stripe.com/api/customers/object
[DataContract]
public class Customer
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "address")]
    public Address Address { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "email")]
    public string Email { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "phone")]
    public string Phone { get; set; }

    [DataMember(Name = "shipping")]
    public Shipping Shipping { get; set; }

    [DataMember(Name = "balance")]
    public int Balance { get; set; }

    [DataMember(Name = "created")]
    public DateTime Created { get; set; }

    [DataMember(Name = "currency")]
    public string Currency { get; set; }

    [DataMember(Name = "default_source")]
    public string DefaultSource { get; set; }

    [DataMember(Name = "delinquent")]
    public bool? Delinquent { get; set; }

    [DataMember(Name = "invoice_prefix")]
    public string InvoicePrefix { get; set; }

    [DataMember(Name = "livemode")]
    public bool Livemode { get; set; }

    [DataMember(Name = "sources")]
    public Sources Sources { get; set; }
}
