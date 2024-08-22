using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

[DataContract]
internal sealed class PaymentIntents
{
	[DataMember(Name = "data")]
	public IEnumerable<PaymentIntent> Data { get; set; }
}
