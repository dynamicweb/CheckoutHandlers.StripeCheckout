using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentMethod;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;
using Dynamicweb.Ecommerce.ChecskoutHandlers.StripeCheckout.Models.Customer;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Frontend;
using Dynamicweb.Rendering;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

/// <summary>
/// StripeCheckout Payment Window Checkout Handler
/// </summary>
[
    AddInName("Stripe checkout"),
    AddInDescription("Checkout handler for Stripe 1.1")
]
public class StripeCheckout : CheckoutHandler, ISavedCard, IParameterOptions, IRecurring, IRemotePartialFinalOnlyCapture, ICancelOrder, IFullReturn, IPartialReturn
{
    private const string PostTemplateFolder = "eCom7/CheckoutHandler/Stripe/Post";
    private const string ErrorTemplateFolder = "eCom7/CheckoutHandler/Stripe/Error";
    private string errorTemplate;
    private string postTemplate;

    #region Addin parameters

    [AddInParameter("Test Secret key"), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
    public string TestSecretKey { get; set; }

    [AddInParameter("Test Publishable key "), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
    public string TestPublishableKey { get; set; }

    [AddInParameter("Live Secret key"), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
    public string LiveSecretKey { get; set; }

    [AddInParameter("Live Publishable key "), AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true; ")]
    public string LivePublishableKey { get; set; }

    [AddInParameter("Language"), AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true; none=false; SortBy=Value;")]
    public string Language { get; set; }

    [AddInParameter("Merchant name"), AddInParameterEditor(typeof(TextParameterEditor), "")]
    public string MerchantName { get; set; }

    [AddInParameter("Merchant logo"), AddInParameterEditor(typeof(FileManagerEditor), "NewGUI=true; useimagesfolder=true; showfullpath=true; extensions=gif,jpg,png;")]
    public string MerchantLogo { get; set; }

    [AddInParameter("Post template"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=Templates/{PostTemplateFolder}")]
    public string PostTemplate
    {
        get => TemplateHelper.GetTemplateName(postTemplate);
        set => postTemplate = value;
    }

    [AddInParameter("Error template"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=Templates/{ErrorTemplateFolder}")]
    public string ErrorTemplate
    {
        get => TemplateHelper.GetTemplateName(errorTemplate);
        set => errorTemplate = value;
    }

    [AddInParameter("Capture now"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Auto-captures a payment when it is authorized. Please note that it is illegal in some countries to capture payment before shipping any physical goods.;")]
    public bool CaptureNow { get; set; }

    [AddInParameter("Render inline form"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Makes it possible to render this form inline in the checkout flow. Use the Ecom:Cart.PaymentInlineForm tag in the cart flow to render the form inline.;")]
    public bool RenderInline { get; set; }

    [AddInParameter("Test mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=When checked, test credentials are used – when unchecked, live credentials are used.;")]
    public bool TestMode { get; set; }

    #endregion

    /// <summary>
    /// Default constructor
    /// </summary>
    public StripeCheckout()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }

    private string GetSecretKey() => TestMode ? TestSecretKey : LiveSecretKey;

    /// <summary>
    /// Starts order checkout procedure
    /// </summary>
    /// <param name="order">Order to be checked out</param>
    /// <param name="parameters">Checkout parameters</param>
    public override OutputResult BeginCheckout(Order order, CheckoutParameters parameters)
    {
        LogEvent(order, "Checkout started");

        var formTemplate = new Template(TemplateHelper.GetTemplatePath(PostTemplate, PostTemplateFolder));
        return RenderPaymentForm(order, formTemplate);
    }

    /// <summary>
    /// Redirects user to to Stripe CheckoutHandler step
    /// </summary>
    /// <param name="order">Order for processing</param>
    /// <returns>String representation of template output</returns>
    public override OutputResult HandleRequest(Order order)
    {
        try
        {
            LogEvent(null, "Redirected to Stripe CheckoutHandler");

            string action = Context.Current.Request["action"];
            switch (action)
            {
                case "Approve":
                    return StateOk(order);

                case "Complete":
                    return StateComplete(order);

                default:
                    string msg = string.Format("Unknown Stripe state: '{0}'", action);
                    LogError(order, msg);
                    return PrintErrorTemplate(order, msg);
            }
        }
        catch (System.Threading.ThreadAbortException)
        {
            return ContentOutputResult.Empty;
        }
        catch (Exception ex)
        {
            LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
            return PrintErrorTemplate(order, ex.Message);
        }
    }

    private OutputResult StateOk(Order order)
    {
        LogEvent(order, "State ok");

        User user = UserContext.Current.User;

        string token = Context.Current.Request["stripeToken"];
        string email = Context.Current.Request["stripeEmail"];

        if (string.IsNullOrEmpty(token))
            throw new Exception("Stripe token is not defined.");

        var cardSettings = new BasePaymentCardSettings(order);
        Customer customer = cardSettings.IsSaveNeeded ? SendCustomerRequest() : null;

        if (!order.IsRecurringOrderTemplate)
            return InitiatePaymentIntent(order, token, email, cardSettings, customer?.Id);
        else
            return CreatePaymentMethod(order, token, cardSettings, customer.Id);

        Customer SendCustomerRequest()
        {
            var service = new StripeService(GetSecretKey());

            IEnumerable<PaymentCardToken> tokens = Services.PaymentCard.GetByUserId(user?.ID ?? 0, order.PaymentMethodId);
            if (tokens.Any())
            {
                string customerId = tokens.First().Token.Split('|')[0];
                try
                {
                    return service.GetCustomer(customerId);
                }
                catch
                {
                    LogEvent(order, "Couldn't use stripe customer: {0}. The new customer will be created.", customerId);
                }
            }

            return service.CreateCustomer(email, user?.UserName);
        }
    }

    private OutputResult InitiatePaymentIntent(Order order, string token, string email, BasePaymentCardSettings cardSettings, string customerId)
    {
        if (order.Complete)
            return PassToCart(order);

        try
        {
            LogEvent(order, "Starting payment process");

            var parameters = new Dictionary<string, object>
            {
                ["amount"] = order.Price.PricePIP,
                ["currency"] = order.CurrencyCode,
                ["description"] = order.Id,
                ["capture_method"] = CaptureNow ? "automatic" : "manual",
                ["confirm"] = true,
                ["return_url"] = $"{GetBaseUrl(order)}&stripeToken={token}&stripeEmail={email}&Action=Complete&CardTokenName={cardSettings.Name}"
            };

            if (cardSettings.IsSaveNeeded)
            {
                parameters["customer"] = customerId;
                parameters["setup_future_usage"] = "off_session";
            }

            parameters["payment_method_data[type]"] = "card";
            parameters["payment_method_data[card][token]"] = token;

            var service = new StripeService(GetSecretKey());
            PaymentIntent paymentIntent = service.CreatePaymentIntent($"{MerchantName}:{order.Id}", parameters);
            PaymentMethod paymentMethod = string.IsNullOrEmpty(customerId) 
                ? service.GetPaymentMethod(paymentIntent.PaymentMethodId)
                : service.GetPaymentMethod(paymentIntent.CustomerId, paymentIntent.PaymentMethodId);
            SaveTransactionInformation(order, paymentMethod, cardSettings, customerId);

            if (paymentIntent.Status is PaymentIntentStatus.RequiresPaymentMethod)
            {
                LogEvent(order, "Stripe requested transaction authorization");
                var nextAction = paymentIntent.NextAction;
                var redirectInfo = nextAction.RedirectToUrl;
                string redirectUrl = redirectInfo.Url;

                return new RedirectOutputResult { RedirectUrl = redirectUrl };
            }

            CompleteOrder(order, paymentIntent, paymentMethod);
            CheckoutDone(order);
        }
        catch (System.Threading.ThreadAbortException)
        {
            //do nothing, payment redirected to authorize transaction
        }
        catch
        {
            CheckoutDone(order);
        }

        if (!order.Complete)
            throw new Exception("Called create charge, but order is not set complete.");

        return PassToCart(order);
    }

    private OutputResult CreatePaymentMethod(Order order, string token, BasePaymentCardSettings cardSettings, string customerId)
    {
        LogEvent(order, "Creating Stripe payment method.");
        var service = new StripeService(GetSecretKey());

        var parameters = new Dictionary<string, object>
        {
            ["type"] = "card",
            ["card[token]"] = token
        };

        LogEvent(order, "Attaching Stripe payment method to a customer.");

        PaymentMethod paymentMethod = service.CreatePaymentMethod(parameters);
        service.AttachPaymentMethod(paymentMethod.Id, customerId);
        SaveTransactionInformation(order, paymentMethod, cardSettings, customerId);

        SetOrderComplete(order);
        CheckoutDone(order);

        return PassToCart(order);
    }

    private void SaveTransactionInformation(Order order, PaymentMethod paymentMethod, BasePaymentCardSettings cardSettings, string customerId)
    {
        Card card = paymentMethod.Card;

        if (!string.IsNullOrEmpty(card.Brand) && !string.IsNullOrEmpty(card.Last4))
        {
            order.TransactionCardType = card.Brand;
            order.TransactionCardNumber = HideCardNumber(Converter.ToString(card.Last4));
        }

        if (cardSettings.IsSaveNeeded)
        {
            if (UserContext.Current.User is User user)
            {
                var paymentCard = new PaymentCardToken
                {
                    UserID = user.ID,
                    PaymentID = order.PaymentMethodId,
                    Name = cardSettings.Name,
                    CardType = order.TransactionCardType,
                    Identifier = order.TransactionCardNumber,
                    Token = string.Format("{0}|{1}", customerId, paymentMethod.Id),
                    UsedDate = DateTime.Now,
                    ExpirationMonth = card.ExpirationMonth,
                    ExpirationYear = card.ExpirationYear
                };
                Services.PaymentCard.Save(paymentCard);
                order.SavedCardId = paymentCard.ID;
            }
            Services.Orders.Save(order);
        }
    }

    private OutputResult PrintErrorTemplate(Order order, string msg, ErrorType errorType = ErrorType.Undefined)
    {
        LogEvent(order, "Printing error template");

        order.TransactionAmount = 0;
        order.TransactionStatus = "Failed";
        order.Errors.Add(msg);
        Services.Orders.Save(order);

        Services.Orders.DowngradeToCart(order);
        order.TransactionStatus = "";
        Common.Context.SetCart(order);

        if (string.IsNullOrWhiteSpace(ErrorTemplate))
            return PassToCart(order);

        var errorTemplate = new Template(TemplateHelper.GetTemplatePath(ErrorTemplate, ErrorTemplateFolder));
        errorTemplate.SetTag("CheckoutHandler:ErrorType", errorType.ToString());
        errorTemplate.SetTag("CheckoutHandler:ErrorMessage", msg);

        return new ContentOutputResult
        {
            Content = Render(order, errorTemplate)
        };
    }

    private void CompleteOrder(Order order, PaymentIntent paymentIntent, PaymentMethod paymentMethod = null)
    {
        if (paymentIntent is null)
            throw new Exception("Payment intent was not found.");

        if (paymentMethod is null)
        {
            var service = new StripeService(GetSecretKey());
            paymentMethod = string.IsNullOrEmpty(paymentIntent.CustomerId) 
                ? service.GetPaymentMethod(paymentIntent.PaymentMethodId)
                : service.GetPaymentMethod(paymentIntent.CustomerId, paymentIntent.PaymentMethodId);
        }

        if (paymentIntent.Status is PaymentIntentStatus.Succeeded or PaymentIntentStatus.RequiresCapture)
        {
            string transactionId = paymentIntent.Id;
            LogEvent(order, "Payment succeeded with transaction number {0}", transactionId);

            string transactionCardType = paymentMethod.Card?.Brand;
            string transactionCardNumber = paymentMethod.Card?.Last4;
            bool captured = paymentIntent.AmountCapturable == 0;
            double transactionAmount = paymentIntent.Amount / 100d;

            order.TransactionCardType = transactionCardType;
            order.TransactionCardNumber = HideCardNumber(transactionCardNumber);
            order.TransactionAmount = transactionAmount;
            order.TransactionStatus = "Succeeded";

            if (captured)
            {
                LogEvent(order, "Autocapturing order", DebuggingInfoType.CaptureResult);
                order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Autocapture successful");
                order.CaptureAmount = order.TransactionAmount;
            }

            SetOrderComplete(order, transactionId);
        }
    }

    private OutputResult StateComplete(Order order)
    {
        try
        {
            LogEvent(order, "Retrieving payment intent object");
            string paymentIntentId = Converter.ToString(Context.Current.Request["payment_intent"]);
            bool getAllPaymentIntents = string.IsNullOrEmpty(paymentIntentId);

            var service = new StripeService(GetSecretKey());

            PaymentIntent paymentIntent = getAllPaymentIntents
                ? service.GetAllPaymentIntents()?.FirstOrDefault()
                : service.GetPaymentIntent(paymentIntentId);

            CompleteOrder(order, paymentIntent);
        }
        finally
        {
            CheckoutDone(order);
        }

        if (!order.Complete)
            throw new Exception("Order completed at Stripe, but order is not set complete in dynamicweb side.");

        return PassToCart(order);
    }

    #region ISavedCard interface

    /// <summary>
    /// Deletes saved card
    /// </summary>
    /// <param name="savedCardID">Identifier of saved card to be deleted</param>
    public void DeleteSavedCard(int savedCardID)
    {
        if (Services.PaymentCard.GetById(savedCardID) is not PaymentCardToken savedCard)
            return;

        string[] token = savedCard.Token.Split('|');
        try
        {
            var service = new StripeService(GetSecretKey());
            if (token.Length > 1)
                service.DetachPaymentMethod(token[1]);
            else
                service.DeleteCustomer(token[0]);
        }
        catch (Exception ex)
        {
            LogError(null, ex, "Delete saved card exception: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Directs checkout handler to use saved card
    /// </summary>
    /// <param name="order">Order that should be processed using saved card information</param>
    /// <returns>Empty string, if operation succeeded, otherwise string template with exception mesage</returns>
    public string UseSavedCard(Order order)
    {
        /*PassToCart part doesn't work because of changes in Redirect behavior.
        * We need to return RedirectOutputResult as OutputResult, and handle output result to make it work.
        * It means, that we need to change ISavedCard.UseSavedCard method, probably create new one (with OutputResult as returned type)
        * To make it work (temporarily), we use Response.Redirect here                 
        */

        try
        {
            if (UseSavedCardInternal(order, true) is RedirectOutputResult redirectResult)
                RedirectToCart(redirectResult);

            return string.Empty;
        }
        catch (System.Threading.ThreadAbortException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            LogEvent(order, ex.Message, DebuggingInfoType.UseSavedCard);
            OutputResult errorResult = PrintErrorTemplate(order, ex.Message, ErrorType.SavedCard);

            if (errorResult is ContentOutputResult contentErrorResult)
                return contentErrorResult.Content;

            if (errorResult is RedirectOutputResult redirectErrorResult)
                RedirectToCart(redirectErrorResult);
        }

        return string.Empty;
    }

    /// <summary>
    /// Shows if order supports saving card
    /// </summary>
    /// <param name="order">Instance of order</param>
    /// <returns>True, if saving card is supported</returns>
    public bool SavedCardSupported(Order order) => true;

    private OutputResult UseSavedCardInternal(Order order, bool alwaysPassToCart)
    {
        PaymentCardToken savedCard = Services.PaymentCard.GetById(order.SavedCardId);
        if (savedCard is null || order.CustomerAccessUserId != savedCard.UserID)
            throw new PaymentCardTokenException("Token is incorrect.");

        LogEvent(order, "Using saved card({0}) with id: {1}", savedCard.Identifier, savedCard.ID);

        if (order.IsRecurringOrderTemplate)
        {
            order.TransactionCardType = savedCard.CardType;
            order.TransactionCardNumber = savedCard.Identifier;
            SetOrderComplete(order);
            CheckoutDone(order);

            if (alwaysPassToCart)
                return PassToCart(order);
        }
        else
        {
            string[] token = savedCard.Token.Split('|');
            var parameters = new Dictionary<string, object>
            {
                { "amount", order.Price.PricePIP },
                { "currency", order.CurrencyCode },
                { "description", order.Id },
                { "customer",  token[0] },
                { "capture_method", CaptureNow ? "automatic" : "manual" },
                { "confirm", true }
            };

            if (token.Length > 1)
            {
                parameters["payment_method_types[0]"] = "card";
                parameters["payment_method"] = token[1];
                parameters["off_session"] = true;
            }
            try
            {
                var service = new StripeService(GetSecretKey());
                PaymentIntent paymentIntent = service.CreatePaymentIntent($"{MerchantName}:{order.Id}", parameters);
                CompleteOrder(order, paymentIntent);
            }
            finally
            {
                CheckoutDone(order);
            }

            if (!order.Complete)
                throw new Exception("Called create charge, but order is not set complete.");

            return PassToCart(order);
        }

        return ContentOutputResult.Empty;
    }

    #endregion

    #region IRemoteCapture

    /// <summary>
    /// Send capture request to transaction service
    /// </summary>
    /// <param name="order">Order to be captured</param>
    /// <returns>Response from transaction service</returns>
    public OrderCaptureInfo Capture(Order order) => Capture(order, order.Price.PricePIP, true);

    public OrderCaptureInfo Capture(Order order, long amount, bool final)
    {
        try
        {
            // Check order
            if (order is null)
            {
                LogError(null, "Order not set");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Order not set");
            }
            else if (string.IsNullOrEmpty(order.Id))
            {
                LogError(null, "Order id not set");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Order id not set");
            }
            else if (string.IsNullOrEmpty(order.TransactionNumber))
            {
                LogError(null, "Transaction number not set");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Transaction number not set");
            }
            else if (amount > order.Price.PricePIP)
            {
                LogError(null, "Amount to capture should be less or equal to order total");
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Amount to capture should be less or equal to order total");
            }

            Dictionary<string, object> parameters = null;
            // Amount to capture. Specify it explicitly when it less than order amount (can be splitted only once)
            if (amount <= order.Price.PricePIP)
            {
                parameters = new Dictionary<string, object>
                {
                    ["amount_to_capture"] = amount
                };
            }

            var service = new StripeService(GetSecretKey());
            PaymentIntent paymentIntent = service.CapturePaymentIntent(order.TransactionNumber, parameters);

            LogEvent(order, "Remote capture status: {0}", paymentIntent.Status.ToString());

            if (paymentIntent.Status is PaymentIntentStatus.Succeeded)
            {
                if (order.Price.PricePIP == amount)
                {
                    LogEvent(order, "Capture successful", DebuggingInfoType.CaptureResult);
                    return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
                }
                else
                {
                    LogEvent(order, string.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Split capture(final)", amount / 100f), DebuggingInfoType.CaptureResult);
                    return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Split capture successful");
                }
            }
            else
            {
                // Not success
                string infoTxt = string.Format("Remote Capture failed. The reason: {0}", paymentIntent.CancellationReason);

                LogEvent(order, infoTxt, DebuggingInfoType.CaptureResult);
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, infoTxt);
            }
        }
        catch (Exception ex)
        {
            LogError(order, ex, "Unexpected error during capture: {0}", ex.Message);
            return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, "Unexpected error during capture");
        }
    }

    /// <summary>
    /// Shows if capture supported
    /// </summary>
    /// <param name="order">This object is not used in current implementation</param>
    /// <returns>This method always return 'true' value</returns>
    public bool CaptureSupported(Order order) => true;

    /// <summary>
    /// Shows if partial capture of the order supported
    /// </summary>
    /// <param name="order">Not used</param>
    /// <returns>Always returns true</returns>
    public bool SplitCaptureSupported(Order order) => true;

    #endregion

    #region IRecurring

    /// <summary>
    /// Creates new payment for recurring order
    /// </summary>
    /// <param name="order">recurring order to be used for payment</param>
    /// <param name="initialOrder">Base order, used for creating current recurring order</param>
    public void Recurring(Order order, Order initialOrder)
    {
        if (order is null)
            return;

        try
        {
            UseSavedCardInternal(order, false);
            LogEvent(order, "Recurring succeeded");
        }
        catch (Exception ex)
        {
            LogEvent(order, "Recurring order failed for {0} (based on {1}). The payment failed with the message: {2}",
                DebuggingInfoType.RecurringError, order.Id, initialOrder.Id, ex.Message);
        }
    }

    /// <summary>
    /// Shows if order supports recurring payments
    /// </summary>
    /// <param name="order">Instance of order</param>
    /// <returns>True, if recurring payments are supported</returns>
    public bool RecurringSupported(Order order) => true;

    #endregion

    #region IDropDownOptions

    /// <summary>
    /// Gets options according to behavior mode
    /// </summary>
    /// <param name="behaviorMode"></param>
    /// <returns>Key-value pairs of settings</returns>
    public IEnumerable<ParameterOption> GetParameterOptions(string parameterName)
    {
        try
        {
            return parameterName switch
            {
                "Post mode" => new List<ParameterOption>
                {
                    new("Auto", "Auto post (does not use the template)"),
                    new("Template", "Render template")
                },
                "Language" => new List<ParameterOption>
                {
                    new("auto", "auto"),
                    new("zh", "Chinese"),
                    new("nl", "Dutch"),
                    new("en", "English"),
                    new("fr", "French"),
                    new("de", "German"),
                    new("it", "Italian"),
                    new("jp", "Japanese"),
                    new("es", "Spanish")
                },
                _ => throw new ArgumentException(string.Format("Unknown dropdown name: '{0}'", parameterName))
            };
        }
        catch (Exception ex)
        {
            LogError(null, ex, "Unhandled exception with message: {0}", ex.Message);
            return null;
        }
    }

    #endregion

    private string Refund(Order order, long? amount = null)
    {
        string errorMessage;
        try
        {

            if (string.IsNullOrEmpty(order.Id))
            {
                errorMessage = "Order id not set";
                LogError(null, errorMessage);
                return errorMessage;
            }

            if (string.IsNullOrEmpty(order.TransactionNumber))
            {
                errorMessage = "Transaction number not set";
                LogError(null, errorMessage);
                return errorMessage;
            }

            var parameters = new Dictionary<string, object> { ["payment_intent"] = order.TransactionNumber };

            if (amount is not null)
                parameters.Add("amount", amount.ToString());

            var service = new StripeService(GetSecretKey());
            Refund refund = service.CreateRefund(parameters);

            LogEvent(order, "Remote return status: {0}", refund.Status);

            if (string.Compare(Converter.ToString(refund.Status), "succeeded", StringComparison.OrdinalIgnoreCase) == 0)
            {
                LogEvent(order, "Return successful", DebuggingInfoType.ReturnResult);
                return null;
            }

            errorMessage = "Return operation failed.";
            // Not success
            if (!string.IsNullOrEmpty(refund.FailureReason))
                errorMessage += $"Reason: {refund.FailureReason}";

            LogEvent(order, errorMessage, DebuggingInfoType.ReturnResult);
            return errorMessage;
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error during return: {ex.Message}";
            LogError(order, ex, errorMessage);
            return errorMessage;
        }
    }

    #region IPartialReturn, IFullReturn

    public void PartialReturn(Order order, Order originalOrder) => ProceedReturn(originalOrder, order?.Price?.PricePIP);

    public void FullReturn(Order order) => ProceedReturn(order);

    private void ProceedReturn(Order order, long? amount = null)
    {
        // Check order
        if (order is null)
        {
            LogError(null, "Order not set");
            return;
        }

        double operationAmount = amount is null ? order.CaptureAmount : Converter.ToDouble(amount) / 100;

        if (order.CaptureInfo is null || order.CaptureInfo.State is not OrderCaptureInfo.OrderCaptureState.Success || order.CaptureAmount <= 0.00)
        {
            OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, "Order must be captured before return.", order.CaptureAmount, order);
            LogError(null, "Order must be captured before return.");
            return;
        }

        if (amount is not null && order.CaptureAmount < operationAmount)
        {
            OrderReturnInfo.SaveReturnOperation
            (
                OrderReturnOperationState.Failed,
                $"Order captured amount({Services.Currencies.Format(order.Currency, order.CaptureAmount)}) less than amount requested for return{Services.Currencies.Format(order.Currency, operationAmount)}.",
                operationAmount,
                order
            );
            LogError(order, "Order captured amount less then amount requested for return.");
            return;
        }

        var errorMessage = Refund(order, amount);
        if (string.IsNullOrEmpty(errorMessage))
        {
            var operationState = amount is null ? OrderReturnOperationState.FullyReturned : OrderReturnOperationState.PartiallyReturned;
            OrderReturnInfo.SaveReturnOperation(operationState, "Stripe has refunded payment.", operationAmount, order);
        }
        else
            OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorMessage, operationAmount, order);
    }

    #endregion

    #region ICancelOrder

    public bool CancelOrder(Order order)
    {
        string errorMessage = Refund(order);
        return string.IsNullOrEmpty(errorMessage);
    }

    #endregion

    #region RenderInlineForm

    public override string RenderInlineForm(Order order)
    {
        if (RenderInline)
        {
            LogEvent(order, "Render inline form");
            var formTemplate = new Template(TemplateHelper.GetTemplatePath(PostTemplate, PostTemplateFolder));

            OutputResult outputResult = RenderPaymentForm(order, formTemplate);

            if (outputResult is ContentOutputResult paymentFormData)
                return paymentFormData.Content;
            if (outputResult is RedirectOutputResult)
                return "Unhandled exception. Please see logs to find the problem.";
        }

        return string.Empty;
    }

    private OutputResult RenderPaymentForm(Order order, Template formTemplate)
    {
        try
        {
            var formValues = new Dictionary<string, string>
            {
                ["publishablekey"] = TestMode ? TestPublishableKey : LivePublishableKey,
                ["language"] = Language,
                ["name"] = MerchantName,
                ["image"] = string.Format("/Files/{0}", MerchantLogo),
                ["description"] = string.Format("Order: {0}", order.Id),
                ["currency"] = order.CurrencyCode,
                ["amount"] = order.Price.PricePIP.ToString(),
                ["email"] = order.CustomerEmail
            };

            foreach ((string key, string value) in formValues)
                formTemplate.SetTag(string.Format("Stripe.{0}", key), value);

            if (order.DoSaveCardToken)
                formTemplate.SetTag("SavedCardCreate", "true");

            // Render and return
            return new ContentOutputResult
            {
                Content = Render(order, formTemplate)
            };
        }
        catch (System.Threading.ThreadAbortException)
        {
            return ContentOutputResult.Empty;
        }
        catch (Exception ex)
        {
            LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
            return PrintErrorTemplate(order, ex.Message);
        }
    }

    #endregion

    /// <summary>
    /// A temporary method to maintain previous behavior. Redirects to cart by Response.Redirect. Please remove it when the needed changes will be done.
    /// </summary>
    private void RedirectToCart(RedirectOutputResult redirectResult) => Context.Current.Response.Redirect(redirectResult.RedirectUrl, redirectResult.IsPermanent);
}
