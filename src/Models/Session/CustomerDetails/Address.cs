﻿using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Session.CustomerDetails;

[DataContract]
internal sealed class Address
{
    [DataMember(Name = "city")]
    public string City { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "line1")]
    public string Line1 { get; set; }

    [DataMember(Name = "line2")]
    public string Line2 { get; set; }

    [DataMember(Name = "postal_code")]
    public string PostalCode { get; set; }

    [DataMember(Name = "state")]
    public string State { get; set; }
}
