namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Service;

internal sealed class PaymentIntentCreateOptions
{
	public string PaymentMethodId { get; set; }

	public string CustomerId { get; set; }

	public bool AutomaticCapture { get; set; }
}
