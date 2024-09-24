using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.LineItems;

[DataContract]
internal sealed class LineItems
{
    [DataMember(Name = "has_more")]
    public bool HasMore { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "data")]
    public IEnumerable<LineItem> Data { get; set; }
}