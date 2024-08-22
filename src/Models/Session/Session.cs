using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.CustomerDetails;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.LineItems;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

//See full object description in the documentation. Link: https://docs.stripe.com/api/checkout/sessions/object
[DataContract]
internal sealed class Session
{
	[DataMember(Name = "id")]
	public string Id { get; set; }

	[DataMember(Name = "client_reference_id")]
	public string ClientReferenceId { get; set; }

	[DataMember(Name = "currency")]
	public string Currency { get; set; }

	[DataMember(Name = "customer")]
	public string Customer { get; set; }

	[DataMember(Name = "customer_email")]
	public string CustomerEmail { get; set; }

	[DataMember(Name = "line_items")]
	public LineItems LineItems { get; set; }

	[DataMember(Name = "mode")]
	public SessionMode Mode { get; set; }

	[DataMember(Name = "payment_intent")]
	public string PaymentIntentId { get; set; }

	[DataMember(Name = "payment_status")]
	public string PaymentStatus { get; set; }

	[DataMember(Name = "return_url")]
	public string ReturnUrl { get; set; }

	[DataMember(Name = "status")]
	public string Status { get; set; }

	[DataMember(Name = "success_url")]
	public string SuccessUrl { get; set; }

	[DataMember(Name = "url")]
	public string Url { get; set; }

	[DataMember(Name = "allow_promotion_codes")]
	public bool? AllowPromotionCodes { get; set; }

	[DataMember(Name = "amount_subtotal")]
	public int? AmountSubtotal { get; set; }

	[DataMember(Name = "amount_total")]
	public int? AmountTotal { get; set; }

	[DataMember(Name = "billing_address_collection")]
	public string BillingAddressCollection { get; set; }

	[DataMember(Name = "cancel_url")]
	public string CancelUrl { get; set; }

	[DataMember(Name = "client_secret")]
	public string ClientSecret { get; set; }

	[DataMember(Name = "created")]
	public long Created { get; set; }

	[DataMember(Name = "customer_creation")]
	public string CustomerCreation { get; set; }

	[DataMember(Name = "customer_details")]
	public CustomerDetails CustomerDetails { get; set; }

	[DataMember(Name = "expires_at")]
	public long ExpiresAt { get; set; }

	[DataMember(Name = "invoice")]
	public string InvoiceId { get; set; }

	[DataMember(Name = "livemode")]
	public bool Livemode { get; set; }

	[DataMember(Name = "locale")]
	public string Locale { get; set; }

	[DataMember(Name = "payment_link")]
	public string PaymentLink { get; set; }

	[DataMember(Name = "payment_method_collection")]
	public string PaymentMethodCollectionType { get; set; }

	[DataMember(Name = "payment_method_types")]
	public IEnumerable<string> PaymentMethodTypes { get; set; }

	[DataMember(Name = "recovered_from")]
	public string RecoveredFromId { get; set; }

	[DataMember(Name = "redirect_on_completion")]
	public string RedirectOnCompletion { get; set; }

	[DataMember(Name = "setup_intent")]
	public string SetupIntentId { get; set; }

	[DataMember(Name = "submit_type")]
	public string SubmitType { get; set; }

	[DataMember(Name = "subscription")]
	public string Subscription { get; set; }

	[DataMember(Name = "ui_mode")]
	public string UiMode { get; set; }
}