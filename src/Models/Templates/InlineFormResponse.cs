using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Templates;

//This one for our templates interactions only
[DataContract]
internal sealed class InlineFormResponse
{
	[DataMember(Name = "clientSecret")]
	public string ClientSecret { get; set; }
}
