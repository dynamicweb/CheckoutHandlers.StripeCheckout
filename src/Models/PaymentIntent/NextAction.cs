using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

[DataContract]
internal sealed class NextAction
{
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "redirect_to_url")]
    public RedirectToUrl RedirectToUrl { get; set; }
}
