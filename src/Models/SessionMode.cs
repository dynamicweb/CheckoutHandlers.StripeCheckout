using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models;

[DataContract]
internal enum SessionMode
{
    [EnumMember(Value = "setup")]
    Setup,

    [EnumMember(Value = "payment")]
    Payment
}
