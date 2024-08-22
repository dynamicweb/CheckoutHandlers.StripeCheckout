using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentMethod;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

//See full object description in the documentation. Link: https://docs.stripe.com/api/payment_methods/object
[DataContract]
internal sealed class PaymentMethod
{
	[DataMember(Name = "id")]
	public string Id { get; set; }

	[DataMember(Name = "customer")]
	public string Customer { get; set; }

	[DataMember(Name = "type")]
	public string Type { get; set; }

	[DataMember(Name = "card")]
	public Card Card { get; set; }
}
