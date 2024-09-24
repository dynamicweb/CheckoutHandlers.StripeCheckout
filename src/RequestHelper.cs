using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Templates;
using Dynamicweb.Frontend;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

internal static class RequestHelper
{ 
    public static bool IsAjaxRequest()
    {
        return "application/json".Equals(Context.Current.Request.Headers["Content-Type"], StringComparison.OrdinalIgnoreCase);
    }

    public static StreamOutputResult EndRequest(string errorMessage)
    {
        string json = Converter.SerializeCompact(new ErrorResponse { ErrorMessage = errorMessage });

        return SendJson(json);
    }

    public static StreamOutputResult SendJson(string json) => new StreamOutputResult
    {
        ContentStream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? string.Empty)),
        ContentType = "application/json"
    };
}