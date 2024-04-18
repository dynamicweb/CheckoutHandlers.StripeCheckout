using System;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal class CommandConfiguration
{
    /// <summary>
    /// Stripe command. See operation urls in <see cref="StripeRequest"/> and <see cref="ApiCommand"/>
    /// </summary>
    public ApiCommand CommandType { get; set; }

    /// <summary>
    /// Command operator id, like https://api.stripe.com/v1/customers/{OperatorId}/sources
    /// </summary>
    public string OperatorId { get; set; }

    /// <summary>
    /// Command operator second id, like https://api.stripe.com/v1/customers/{OperatorId}/sources/{OperatorSecondId}
    /// </summary>
    public string OperatorSecondId { get; set; }

    /// <summary>
    /// The API supports idempotency for safely retrying requests without accidentally performing the same operation twice. 
    /// When creating or updating an object, use an idempotency key. 
    /// Then, if a connection error occurs, you can safely repeat the request without risk of creating a second object or performing the update twice.
    /// </summary>
    public string IdempotencyKey { get; set; }

    /// <summary>
    /// Parameters for x-www-form-urlencoded format which is used in Stripe
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
