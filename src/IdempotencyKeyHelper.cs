namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal static class IdempotencyKeyHelper
{
    public static string GetKey(ApiCommand command, string merchantName, string orderId)
    {
        //Add new keys if needed. Idempotency keys could be used for any operations, except GET requests.
        return command switch
        {
            ApiCommand.CreatePaymentIntent => GetKey(),
            ApiCommand.CreatePaymentMethod => GetKey() + "PM",
            ApiCommand.CreateSetupIntent => GetKey() + "SI",
            _ => string.Empty
        };

        string GetKey() => GetBaseKey(merchantName, orderId);
    }

    private static string GetBaseKey(string merchantName, string orderId) => $"{merchantName}:{orderId}";
}
