using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.CustomerDetails;

[DataContract]
internal sealed class CustomerDetails
{
	[DataMember(Name = "address")]
	public Address Address { get; set; }

	[DataMember(Name = "email")]
	public string Email { get; set; }

	[DataMember(Name = "name")]
	public string Name { get; set; }

	[DataMember(Name = "phone")]
	public string Phone { get; set; }

	[DataMember(Name = "tax_exempt")]
	public string TaxExempt { get; set; }
}
