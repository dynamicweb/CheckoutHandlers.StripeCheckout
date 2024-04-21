using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentMethod;
using System;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

//See full object description in the documentation. Link: https://docs.stripe.com/api/payment_methods/object
[DataContract]
public class PaymentMethod
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "billing_details")]
    public Billing BillingDetails { get; set; }

    [DataMember(Name = "customer")]
    public string Customer { get; set; }

    [DataMember(Name = "type")]
    public PaymentMethodType Type { get; set; }
       
    [DataMember(Name = "created")]
    public DateTime Created { get; set; }

    [DataMember(Name = "card")]
    public Card Card { get; set; }
}
