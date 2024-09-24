using Dynamicweb.Core;
using System;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

[Serializable]
internal sealed class CardTokenKey : IEquatable<CardTokenKey>
{
    internal const string IdSeparator = "|";

    public string CustomerId { get; set; }

    public string PaymentMethodId { get; set; }

    public CardTokenKey(string customerId, string paymentMethodId)
    {
        Ensure.NotNullOrEmpty(customerId, $"Card token parse error: Customer id cannot be empty");
        Ensure.NotNullOrEmpty(paymentMethodId, $"Card token parse error: Payment method id cannot be empty");

        CustomerId = customerId;
        PaymentMethodId = paymentMethodId;
    }

    public static CardTokenKey Parse(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var parts = key.Split(new string[] { IdSeparator }, StringSplitOptions.None);

        return new CardTokenKey(parts[0], parts[1]);
    }

    public override string ToString() => $"{CustomerId}{IdSeparator}{PaymentMethodId}";

    public override bool Equals(object obj) => Equals(obj as CardTokenKey);

    public bool Equals(CardTokenKey other)
    {
        return other is object
            && string.Equals(CustomerId, other.CustomerId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(PaymentMethodId, other.PaymentMethodId, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => CustomerId.GetHashCode() ^ PaymentMethodId.GetHashCode();
}
