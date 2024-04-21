using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Source;

[DataContract]
public class Sources
{
    [DataMember(Name = "data")]
    public IEnumerable<Source> Data { get; set; }
}
