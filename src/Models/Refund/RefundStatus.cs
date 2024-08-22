using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;

[DataContract]
internal enum RefundStatus
{
	[EnumMember(Value = "pending")]
	Pending,

	[EnumMember(Value = "requires_action")]
	RequiresAction,

	[EnumMember(Value = "succeeded")]
	Succeeded,

	[EnumMember(Value = "failed")]
	Failed,

	[EnumMember(Value = "canceled")]
	Canceled
}
