using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

[DataContract]
internal sealed class RedirectToUrl
{
	[DataMember(Name = "return_url")]
	public string ReturnUrl { get; set; }

	[DataMember(Name = "url")]
	public string Url { get; set; }
}
