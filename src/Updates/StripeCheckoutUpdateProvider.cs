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
            new FileUpdate("3c1ac6b0-25a6-4aad-b463-b8c69e20cede", this, "/Files/Templates/eCom7/CheckoutHandler/Stripe/Error/checkouthandler_error.cshtml", () => GetResourceStream("checkouthandler_error.cshtml")),
            new FileUpdate("9c179793-48cd-4c21-baff-2318918ead21", this, "/Files/Templates/eCom7/CheckoutHandler/Stripe/Post/Post.cshtml", () => GetResourceStream("Post.cshtml")),
            new FileUpdate("ac0b3e40-34ad-4373-a823-8f77f3f20adf", this, "/Files/Templates/eCom7/CheckoutHandler/Stripe/Post/Post_inline.cshtml", () => GetResourceStream("Post_inline.cshtml"))
        };
    }

    /*
     * IMPORTANT!
     * Use a generated GUID string as id for an update
     * - Execute command in C# interactive window: Guid.NewGuid().ToString()
     */
}