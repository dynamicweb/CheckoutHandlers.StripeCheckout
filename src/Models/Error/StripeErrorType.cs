using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Error;

[DataContract]
internal enum StripeErrorType
{
    [EnumMember(Value = "api_error")]
    ApiError,

    [EnumMember(Value = "card_error")]
    CardError,

    [EnumMember(Value = "idempotency_error")]
    IdempotencyError,

    [EnumMember(Value = "invalid_request_error")]
    InvalidRequestError
}
