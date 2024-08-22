using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Service;

internal sealed class SessionCreateOptions
{
	public SessionMode Mode { get; set; }

	public bool AutomaticCapture { get; set; }

	public bool SavePaymentMethod { get; set; }

	public bool EmbeddedForm { get; set; }

	public string CustomerId { get; set; }

	public string CompleteUrl { get; set; }

	public string Language { get; set; }

	public bool AutomaticPaymentMethods { get; set; }

	public IEnumerable<string> PaymentMethods { get; set; }
}
