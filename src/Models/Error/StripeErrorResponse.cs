﻿using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Error;

[DataContract]
internal sealed class StripeErrorResponse
{
    [DataMember(Name = "error")]
    public StripeError Error { get; set; }
}
