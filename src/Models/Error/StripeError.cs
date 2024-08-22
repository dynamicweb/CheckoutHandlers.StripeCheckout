using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Error;

//See full object description in the documentation. Link: https://docs.stripe.com/api/errors
[DataContract]
internal sealed class StripeError
{
    [DataMember(Name = "type")]
    public StripeErrorType ErrorType { get; set; }

    [DataMember(Name = "code")]
    public string Code { get; set; }

    [DataMember(Name = "decline_code")]
    public string DeclineCode { get; set; }

    [DataMember(Name = "message")]
    public string Message { get; set; }

    [DataMember(Name = "param")]
    public string ParameterName { get; set; }

    [DataMember(Name = "request_log_url")]
    public string LogUrl { get; set; }
}
