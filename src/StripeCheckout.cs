﻿using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentMethod;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Refund;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.Templates;
using Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Service;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Extensibility;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Frontend;
using Dynamicweb.Rendering;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout;

/// <summary>
/// StripeCheckout Payment Window Checkout Handler
/// </summary>
[
    AddInName("Stripe checkout"),
    AddInDescription("Checkout handler for Stripe 2.0")
]
public class StripeCheckout : CheckoutHandler, ISavedCard, IParameterOptions, IRecurring, IRemotePartialFinalOnlyCapture, ICancelOrder, IFullReturn, IPartialReturn
{
    private const string PostTemplateFolder = "eCom7/CheckoutHandler/Stripe/Post";
    private const string ErrorTemplateFolder = "eCom7/CheckoutHandler/Stripe/Error";
    private string errorTemplate;
    private string postTemplate;
    private PostModes PostMode { get; set; } = PostModes.Auto;

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
     
    // <summary>
    /// Gets or sets post mode indicates how user will be redirected to Stripe service
    /// </summary>
    [AddInParameter("Post mode"), AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true; none=false; SortBy=Value; infoText=Manages the post mode. You can either do a direct redirect to Stripe payment form, either render the template on separate page or as inline form template.;")]
    public string PostModeSelection
    {
        get => PostMode.ToString();
        set
        {            
            PostMode = value switch
            {
                nameof(PostModes.Auto) => PostModes.Auto,
                nameof(PostModes.Template) => PostModes.Template,
                nameof(PostModes.InlineTemplate) => PostModes.InlineTemplate,
                _ => throw new NotSupportedException($"Unknown value of post mode was used. The value: {value}")
            };
        }
    }

    [AddInParameter("Test mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=When checked, test credentials are used – when unchecked, live credentials are used.;")]
    public bool TestMode { get; set; }

    [AddInParameter("Save cards"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Allow Stripe to save payment methods data to use them in Dynamicweb (as saved cards). Works only for Card (Stripe payment method). You need to activate this setting to create recurring orders.")]
    public bool SaveCards { get; set; } = true;

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
    /// The list of payment methods available for future usage, like saving the payment card to pay later using its data
    /// </summary>
    private HashSet<string> PaymentMethodsForFutureUsage => new(StringComparer.OrdinalIgnoreCase) { "card" };

    /// <summary>
    /// Starts order checkout procedure
    /// </summary>
    /// <param name="order">Order to be checked out</param>
    /// <param name="parameters">Checkout parameters</param>
    public override OutputResult BeginCheckout(Order order, CheckoutParameters parameters)
    {
        LogEvent(order, "Checkout started");

        if (PostMode is not PostModes.Auto)
        {
            var formTemplate = new Template(TemplateHelper.GetTemplatePath(PostTemplate, PostTemplateFolder));
            return RenderPaymentForm(order, formTemplate);
        }

        return CreateSession(order, order.IsRecurringOrderTemplate);
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
                case "CreateSession":
                    return CreateSession(order, order.IsRecurringOrderTemplate);

                case "Complete":
                    return StateComplete(order);

                case "CompleteSetup":
                    return StateCompleteSetup(order);

                default:
                    string msg = string.Format("Unknown Stripe state: '{0}'", action);
                    LogError(order, msg);
                    return PrintErrorTemplate(order, msg);
            }
        }
        catch (ThreadAbortException)
        {
            return ContentOutputResult.Empty;
        }
        catch (Exception ex)
        {
            LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
            return PrintErrorTemplate(order, ex.Message);
        }
    }

    private OutputResult CreateSession(Order order, bool recurring)
    {
        try
        {
            if (recurring && !SaveCards)
                throw new Exception("Please activate 'Save cards' in Stripe settings to create the recurring order.");

            var cardSettings = new BasePaymentCardSettings(order);
            string action = recurring ? "CompleteSetup" : "Complete";

            PaymentCardToken savedCard = Services.PaymentCard.GetByUserId(order.CustomerAccessUserId).FirstOrDefault(card => !string.IsNullOrEmpty(card.Token));
            string[] cardToken = savedCard?.Token?.Split('|');           

            var service = new StripeService(GetSecretKey());
            string idempotencyKey = IdempotencyKeyHelper.GetKey(ApiCommand.CreateSession, MerchantName, order.Id);

            Session session = service.CreateSession(idempotencyKey, order, new()
            {
                AutomaticCapture = CaptureNow,
                SavePaymentMethod = cardSettings.IsSaveNeeded && SaveCards,
                CustomerId = cardToken?.ElementAtOrDefault(0),
                Language = Language,
                Mode = recurring ? SessionMode.Setup : SessionMode.Payment,
                EmbeddedForm = PostMode is PostModes.InlineTemplate,
                AutomaticPaymentMethods = !recurring,
                PaymentMethods = recurring ? PaymentMethodsForFutureUsage : null,
                CompleteUrl = $"{GetBaseUrl(order)}&Action={action}&CardTokenName={cardSettings.Name}&session_id={{CHECKOUT_SESSION_ID}}"
            });

            if (PostMode is not PostModes.InlineTemplate)
                return new RedirectOutputResult { RedirectUrl = session.Url };

            var response = new InlineFormResponse { ClientSecret = session.ClientSecret };
            return RequestHelper.SendJson(Converter.Serialize(response));
        }
        catch (Exception ex)
        {
            string message = "Exception during session creation: " + ex.Message;
            if (RequestHelper.IsAjaxRequest())
                return RequestHelper.EndRequest(message);

            throw new Exception(message);
        }
    }

    private void SavePaymentCard(Order order, PaymentMethod paymentMethod, BasePaymentCardSettings cardSettings)
    {
        if (cardSettings?.IsSaveNeeded is not true || !SaveCards)
            return;

        if (UserContext.Current.User is User user && PaymentMethodsForFutureUsage.Contains(paymentMethod.Type))
        {
            var paymentCard = new PaymentCardToken
            {
                UserID = user.ID,
                PaymentID = order.PaymentMethodId,
                Name = cardSettings.Name,
                Token = new CardTokenKey(paymentMethod.Customer, paymentMethod.Id).ToString(),
                UsedDate = DateTime.Now,
                CardType = paymentMethod.Type,
                Identifier = paymentMethod.Type
            };

            if (paymentMethod.Card is Card card)
            {
                paymentCard.CardType = card.Brand;
                paymentCard.Identifier = HideCardNumber(card.Last4);
                paymentCard.ExpirationMonth = card.ExpirationMonth;
                paymentCard.ExpirationYear = card.ExpirationYear;
            }

            Services.PaymentCard.Save(paymentCard);
            order.SavedCardId = paymentCard.ID;
            order.TransactionCardType = paymentCard.CardType;
            order.TransactionCardNumber = paymentCard.Identifier;

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

    private void CompleteOrder(Order order, PaymentIntent paymentIntent, BasePaymentCardSettings cardSettings)
    {
        if (paymentIntent is null)
            throw new Exception("Payment intent (Stripe object) was not found.");

        var service = new StripeService(GetSecretKey());
        PaymentMethod paymentMethod = service.GetPaymentMethod(paymentIntent.PaymentMethodId);

        SavePaymentCard(order, paymentMethod, cardSettings);
        order.TransactionCardType = paymentMethod.Card?.Brand ?? "Unknown";
        order.TransactionCardNumber = HideCardNumber(paymentMethod.Card?.Last4);

        if (paymentIntent.Status is PaymentIntentStatus.Succeeded or PaymentIntentStatus.RequiresCapture)
        {
            string transactionId = paymentIntent.Id;
            LogEvent(order, "Payment succeeded with transaction number {0}", transactionId);

            bool captured = paymentIntent.AmountCapturable == 0;
            order.TransactionAmount = paymentIntent.Amount / 100d;
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
            string sessionId = Converter.ToString(Context.Current.Request["session_id"]);

            var service = new StripeService(GetSecretKey());
            Session session = service.GetSession(sessionId);
            PaymentIntent paymentIntent = service.GetPaymentIntent(session.PaymentIntentId);

            if (paymentIntent.LastPaymentError is not null)
            {
                string errorMessage = StripeService.GetErrorMessage(paymentIntent.LastPaymentError);
                throw new Exception(errorMessage);
            }

            var cardSettings = new BasePaymentCardSettings(order);
            CompleteOrder(order, paymentIntent, cardSettings);
        }
        finally
        {
            CheckoutDone(order);
        }

        if (!order.Complete)
            throw new Exception("Order completed at Stripe, but order is not set complete in dynamicweb side.");

        return PassToCart(order);
    }

    private OutputResult StateCompleteSetup(Order order)
    {
        if (!order.IsRecurringOrderTemplate)
            throw new Exception("SetupIntent (Stripe object) is only for recurring orders setup.");

        try
        {
            LogEvent(order, "Retrieving setup intent object");

            var service = new StripeService(GetSecretKey());
            string setupIntentId = Converter.ToString(Context.Current.Request["setup_intent"]);

            if (string.IsNullOrEmpty(setupIntentId))
            {
                string sessionId = Converter.ToString(Context.Current.Request["session_id"]);
                Session session = service.GetSession(sessionId);
                setupIntentId = session.SetupIntentId;
            }

            SetupIntent setupIntent = service.GetSetupIntent(setupIntentId);

            if (setupIntent.LastSetupError is not null)
            {
                string errorMessage = StripeService.GetErrorMessage(setupIntent.LastSetupError);
                throw new Exception(errorMessage);
            }

            PaymentMethod paymentMethod = service.GetPaymentMethod(setupIntent.CustomerId, setupIntent.PaymentMethodId);
            var cardSettings = new BasePaymentCardSettings(order);
            SavePaymentCard(order, paymentMethod, cardSettings);

            if (setupIntent.Status is PaymentIntentStatus.Succeeded)
                SetOrderComplete(order);
        }
        finally
        {
            CheckoutDone(order);
        }

        if (!order.Complete)
            throw new Exception("Setup intent (Stripe object) was created, but order is not set complete.");

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
            if (UseSavedCardInternal(order, false) is RedirectOutputResult redirectResult)
                RedirectToCart(redirectResult);

            return string.Empty;
        }
        catch (ThreadAbortException)
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

    private OutputResult UseSavedCardInternal(Order order, bool recurringOrderPayment)
    {
        PaymentCardToken savedCard = Services.PaymentCard.GetById(order.SavedCardId);
        if (savedCard is null || order.CustomerAccessUserId != savedCard.UserID)
            throw new PaymentCardTokenException("Token is incorrect.");

        LogEvent(order, "Using saved card({0}) with id: {1}", savedCard.Identifier, savedCard.ID);

        var cardTokenKey = CardTokenKey.Parse(savedCard.Token);
        try
        {
            var service = new StripeService(GetSecretKey());

            if (!order.IsRecurringOrderTemplate || recurringOrderPayment)
            {
                var idempotencyKey = IdempotencyKeyHelper.GetKey(ApiCommand.CreatePaymentIntent, MerchantName, order.Id);
                PaymentIntent paymentIntent = service.CreateOffPaymentIntent(idempotencyKey, order, new()
                {
                    AutomaticCapture = CaptureNow,
                    CustomerId = cardTokenKey.CustomerId,
                    PaymentMethodId = cardTokenKey.PaymentMethodId
                });
                CompleteOrder(order, paymentIntent, null);
                CheckoutDone(order);
            }
            else
            {
                var idempotencyKey = IdempotencyKeyHelper.GetKey(ApiCommand.CreateSetupIntent, MerchantName, order.Id);
                SetupIntent setupIntent = service.CreateOffSetupIntent(idempotencyKey, new()
                {
                    CustomerId = cardTokenKey.CustomerId,
                    PaymentMethodId = cardTokenKey.PaymentMethodId
                });
                if (setupIntent.Status is not PaymentIntentStatus.Succeeded)
                    throw new Exception("Something went wrong during setup intent creation using saved card data. Probably the payment method is outdated or wasn't configured for off payments. Try to create recurring order using new card data.");

                return new RedirectOutputResult { RedirectUrl = $"{GetBaseUrl(order)}&Action=CompleteSetup&setup_intent={setupIntent.Id}&SaveCard=false" };
            }
        }
        catch
        {
            CheckoutDone(order);
        }

        if (!order.Complete)
            throw new Exception("Called create charge, but order is not set complete.");

        return PassToCart(order);
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
                double capturedAmount = amount / 100d;
                if (order.Price.PricePIP == amount)
                {
                    LogEvent(order, string.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Capture successful", capturedAmount), DebuggingInfoType.CaptureResult);
                    return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
                }
                else
                {
                    LogEvent(order, string.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Split capture(final)", capturedAmount), DebuggingInfoType.CaptureResult);
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
            UseSavedCardInternal(order, true);
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
                _ when CompareNames(nameof(Language), parameterName) =>
                [
                    new("Auto", "auto"),
                    new("Chinese", "zh"),
                    new("Dutch", "nl"),
                    new("English", "en"),
                    new("French", "fr"),
                    new("German", "de"),
                    new("Italian", "it"),
                    new("Spanish", "es")
                ],
                _ when CompareNames(nameof(PostModeSelection), parameterName) =>
                [
                    new("Auto", nameof(PostModes.Auto)) { Hint = "Makes a direct redirect to Stripe payment form" },
                    new("Template", nameof(PostModes.Template)) { Hint = "Renders the selected template before redirecting to Stripe payment form" },
                    new("Inline template", nameof(PostModes.InlineTemplate)) { Hint = "Renders the form inline in the checkout flow. Use the Ecom:Cart.PaymentInlineForm tag in the cart flow to render the form inline" }
                ],
                _ => throw new ArgumentException(string.Format("Unknown dropdown name: '{0}'", parameterName))
            };
        }
        catch (Exception ex)
        {
            LogError(null, ex, "Unhandled exception with message: {0}", ex.Message);
            return null;
        }

        bool CompareNames(string propertyName, string parameterName) => GetPropertyLabel(propertyName).Equals(parameterName, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    private string Refund(Order order, long? amount = null)
    {
        try
        {
            if (string.IsNullOrEmpty(order.Id))
                return Error("Order id not set");

            if (string.IsNullOrEmpty(order.TransactionNumber))
                return Error("Transaction number not set");

            var parameters = new Dictionary<string, object>
            {
                ["payment_intent"] = order.TransactionNumber,
                ["amount"] = amount
            };

            var service = new StripeService(GetSecretKey());
            Refund refund = service.CreateRefund(parameters);

            LogEvent(order, "Remote return status: {0}", refund.Status);

            if (refund.Status is RefundStatus.Succeeded)
                return Result("Return successful");

            if (refund.Status is RefundStatus.Pending)
                return Result("Return successful. It may take a few days for the money to reach the customer's bank account.");

            if (refund.Status is RefundStatus.RequiresAction)
                return Result("Additional action is required to complete the refund. See logs in the Stripe account.");

            string message = "Return operation failed.";
            // Not success
            if (!string.IsNullOrEmpty(refund.FailureReason))
                message += $" Reason: {refund.FailureReason}";

            return Error(message);
        }
        catch (Exception ex)
        {
            return Error($"Unexpected error during return: {ex.Message}");
        }

        string Error(string errorMessage)
        {
            LogError(null, errorMessage);
            return errorMessage;
        }

        string Result(string message)
        {
            LogEvent(order, message, DebuggingInfoType.ReturnResult);
            return null;
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
        string errorMessage = order.CaptureInfo.State is OrderCaptureInfo.OrderCaptureState.Success
            ? Refund(order)
            : CancelPaymentIntent(order);

        return string.IsNullOrEmpty(errorMessage);
    }

    private string CancelPaymentIntent(Order order)
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

            var service = new StripeService(GetSecretKey());
            PaymentIntent paymentIntent = service.CancelPaymentIntent(order.TransactionNumber);
            if (paymentIntent.Status is PaymentIntentStatus.Canceled)
            {
                LogEvent(order, "Cancel operation successful.");
                return null;
            }

            errorMessage = "Cancel operation failed.";
            LogEvent(order, errorMessage);
            return errorMessage;
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error during cancel operation: {ex.Message}.";
            LogError(order, ex, errorMessage);
            return errorMessage;
        }
    }

    #endregion

    #region RenderInlineForm

    public override string RenderInlineForm(Order order)
    {
        if (PostMode is PostModes.InlineTemplate)
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
            var cardSettings = new BasePaymentCardSettings(order);

            var logoPath = MerchantLogo;
            if (!MerchantLogo.StartsWith("/Files/", StringComparison.OrdinalIgnoreCase))
                logoPath = string.Format("/Files/{0}", MerchantLogo);

            var formValues = new Dictionary<string, string>
            {
                ["publishablekey"] = TestMode ? TestPublishableKey : LivePublishableKey,
                ["language"] = Language,
                ["name"] = MerchantName,
                ["image"] = logoPath,
                ["description"] = string.Format("Order: {0}", order.Id),
                ["currency"] = order.CurrencyCode,
                ["amount"] = order.Price.PricePIP.ToString(),
                ["email"] = order.CustomerEmail,
                ["cardName"] = cardSettings.Name
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
        catch (ThreadAbortException)
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

    private string GetPropertyLabel(string propertyName)
    {
        var property = TypeHelper.GetProperty(typeof(StripeCheckout), propertyName);
        var attribute = TypeHelper.GetCustomAttribute<AddInParameterAttribute>(property);

        if (!string.IsNullOrEmpty(attribute.Name))
            return attribute.Name;

        return property.Name;
    }
}
