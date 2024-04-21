using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

[DataContract]
public enum PaymentIntentStatus
{
    [EnumMember(Value = "canceled")]
    Canceled,

    [EnumMember(Value = "processing")]
    Processing,

    [EnumMember(Value = "requires_action")]
    RequiresAction,

    [EnumMember(Value = "requires_capture")]
    RequiresCapture,

    [EnumMember(Value = "requires_confirmation")]
    RequiresConfirmation,

    [EnumMember(Value = "requires_payment_method")]
    RequiresPaymentMethod,

    [EnumMember(Value = "succeeded")]
    Succeeded
}
