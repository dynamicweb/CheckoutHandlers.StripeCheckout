using Dynamicweb.Updates;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Updates;

public sealed class StripeCheckoutUpdateProvider : UpdateProvider
{
    private static Stream GetResourceStream(string name)
    {
        string resourceName = $"Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Updates.{name}";

        return Assembly.GetAssembly(typeof(StripeCheckoutUpdateProvider)).GetManifestResourceStream(resourceName);
    }

    public override IEnumerable<Update> GetUpdates()
    {
        return new List<Update>()
        {
            new FileUpdate("09f1c431-5271-4648-8378-70de4a94bfb7", this, "/Files/Templates/eCom7/CheckoutHandler/Stripe/Error/checkouthandler_error.html", () => GetResourceStream("checkouthandler_error.html")),
            new FileUpdate("0f1d4d85-0c55-4e84-ad1a-bf8c790da7ed", this, "/Files/Templates/eCom7/CheckoutHandler/Stripe/Post/Post_custom.html", () => GetResourceStream("Post_custom.html")),
            new FileUpdate("a5d4363f-e9c6-4c2f-aec0-7fac62a7fe44", this, "/Files/Templates/eCom7/CheckoutHandler/Stripe/Post/Post_simple.html", () => GetResourceStream("Post_simple.html"))
        };
    }

    /*
     * IMPORTANT!
     * Use a generated GUID string as id for an update
     * - Execute command in C# interactive window: Guid.NewGuid().ToString()
     */
}