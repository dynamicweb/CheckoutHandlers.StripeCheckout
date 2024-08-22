using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.LineItems;

[DataContract]
internal sealed class LineItem
{
	[DataMember(Name = "id")]
	public string Id { get; set; }

	[DataMember(Name = "amount_discount")]
	public int AmountDiscount { get; set; }

	[DataMember(Name = "amount_subtotal")]
	public int AmountSubtotal { get; set; }

	[DataMember(Name = "amount_tax")]
	public int AmountTax { get; set; }

	[DataMember(Name = "amount_total")]
	public int AmountTotal { get; set; }

	[DataMember(Name = "currency")]
	public string Currency { get; set; }

	[DataMember(Name = "description")]
	public string Description { get; set; }

	[DataMember(Name = "price")]
	public PriceInformation Price { get; set; }

	[DataMember(Name = "quantity")]
	public int? Quantity { get; set; }

	[DataMember(Name = "has_more")]
	public bool HasMore { get; set; }

	[DataMember(Name = "url")]
	public string Url { get; set; }
}