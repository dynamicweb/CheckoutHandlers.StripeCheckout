using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Orders;
using System;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal sealed class BasePaymentCardSettings
{
    public string Name { get; set; }

    public bool IsSaveNeeded { get; set; }

    private Order Order { get; set; }

    public BasePaymentCardSettings(Order order)
    {
        Order = order;

        Name = GetCardName();
        IsSaveNeeded = Converter.ToBoolean(Context.Current.Request["SavedCardCreate"]) || !string.IsNullOrWhiteSpace(Name) || order.IsRecurringOrderTemplate;
        if (Context.Current.Request["SaveCard"] is string doSaveCard && IsSaveNeeded)
            IsSaveNeeded = Converter.ToBoolean(doSaveCard);
    }

    private string GetCardName()
    {
        string cardName = Context.Current.Request["CardTokenName"];
        string resetDraftCardName = Context.Current.Request["ResetDraftCardName"];
        if (string.IsNullOrWhiteSpace(cardName) && !string.Equals(resetDraftCardName, "true", StringComparison.OrdinalIgnoreCase))
            cardName = Order.SavedCardDraftName;

        return cardName;
    }
}
