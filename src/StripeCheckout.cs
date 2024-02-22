using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Rendering;
using Dynamicweb.Security.UserManagement;
using Dynamicweb.SystemTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout
{
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

        private enum ErrorType
        {
            Undefined,
            SavedCard
        }

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
            get
            {
                return TemplateHelper.GetTemplateName(postTemplate);
            }
            set => postTemplate = value;
        }

        [AddInParameter("Error template"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=Templates/{ErrorTemplateFolder}")]
        public string ErrorTemplate
        {
            get
            {
                return TemplateHelper.GetTemplateName(errorTemplate);
            }
            set => errorTemplate = value;
        }

        [AddInParameter("Capture now"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Auto-captures a payment when it is authorized. Please note that it is illegal in some countries to capture payment before shipping any physical goods.;")]
        public bool CaptureNow { get; set; }

        [AddInParameter("Render inline form"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=Makes it possible to render this form inline in the checkout flow. Use the Ecom:Cart.PaymentInlineForm tag in the cart flow to render the form inline.;")]
        public bool RenderInline { get; set; }

        [AddInParameter("Test mode"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=When checked, test credentials are used – when unchecked, live credentials are used.;")]
        public bool TestMode { get; set; }

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public StripeCheckout()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        #endregion

        /// <summary>
		/// Starts order checkout procedure
        /// </summary>
		/// <param name="order">Order to be checked out</param>
		/// <returns>String representation of template output</returns>
        public override string StartCheckout(Order order)
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
        public override string Redirect(Order order)
        {
            try
            {
                LogEvent(null, "Redirected to Stripe CheckoutHandler");

                string action = Dynamicweb.Context.Current.Request["action"];
                switch (action)
                {
                    case "Approve":
                        return StateOk(order);

                    case "Complete":
                        StateComplete(order);
                        return null;

                    default:
                        string msg = string.Format("Unknown Stripe state: '{0}'", action);
                        LogError(order, msg);
                        return PrintErrorTemplate(order, msg);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
                return PrintErrorTemplate(order, ex.Message);
            }
        }

        private string StateOk(Order order)
        {
            LogEvent(order, "State ok");
            var user = UserContext.Current.User;
            var saveUserCard = true;
            var cardId = string.Empty;
            string cardName = Dynamicweb.Context.Current.Request["CardTokenName"];
            if (string.IsNullOrWhiteSpace(cardName) && !string.Equals(Dynamicweb.Context.Current.Request["ResetDraftCardName"], "true", StringComparison.InvariantCultureIgnoreCase))
            {
                cardName = order.SavedCardDraftName;
            }

            string token = Dynamicweb.Context.Current.Request["stripeToken"];
            string email = Dynamicweb.Context.Current.Request["stripeEmail"];
            string customerId = string.Empty;
            var card = new Dictionary<string, object>();

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Stripe token is not defined.");
            }

            var rqstCharge = new Dictionary<string, object>
            {
                { "amount", order.Price.PricePIP },
                { "currency", order.CurrencyCode },
                { "description", order.Id },
                { "capture_method", CaptureNow ? "automatic" : "manual"},
                { "confirm", true},
                { "return_url", $"{GetBaseUrl(order)}&stripeToken={token}&stripeEmail={email}&Action=Complete&CardTokenName={cardName}"}
            };

            if (Converter.ToBoolean(Context.Current.Request["SavedCardCreate"]) || !string.IsNullOrWhiteSpace(cardName) || order.IsRecurringOrderTemplate)
            {
                if (user != null)
                {
                    var tokens = Services.PaymentCard.GetByUserId(user.ID, order.PaymentMethodId);
                    if (tokens.Any())
                    {
                        customerId = tokens.First().Token.Split('|')[0];
                        var requestCard = new Dictionary<string, object> { { "source", token } };

                        try
                        {
                            card = ExecuteRequest(string.Format("customers/{0}/sources", customerId), requestCard);
                            cardId = Converter.ToString(card["id"]);
                        }
                        catch
                        {
                            LogEvent(order, "Couldn't use stripe customer: {0}. Will create new one.", customerId);
                        }
                    }
                }
                if (string.IsNullOrEmpty(cardId))
                {
                    var requestCustomer = new Dictionary<string, object> {
                        { "email", email },
                        { "card", token }
                    };

                    var customer = ExecuteRequest("customers", requestCustomer);
                    customerId = Converter.ToString(customer["id"]);
                    var sources = (dynamic)customer["sources"];
                    card = sources["data"].First.ToObject<Dictionary<string, object>>();
                    cardId = Converter.ToString(card["id"]);
                }

                rqstCharge["customer"] = customerId;
                rqstCharge["payment_method_types[0]"] = "card";
                rqstCharge["payment_method"] = cardId;
                rqstCharge["setup_future_usage"] = "off_session";
            }
            else
            {
                rqstCharge["payment_method_data[type]"] = "card";
                rqstCharge["payment_method_data[card][token]"] = token;
                saveUserCard = false;
            }

            if (string.IsNullOrEmpty(cardName))
            {
                cardName = order.Id;
            }

            if (card.ContainsKey("brand") && card.ContainsKey("last4"))
            {
                order.TransactionCardType = Converter.ToString(card["brand"]);
                order.TransactionCardNumber = HideCardNumber(Converter.ToString(card["last4"]));
            }

            if (saveUserCard)
            {
                if (user != null)
                {
                    var savedCard = Services.PaymentCard.CreatePaymentCard(
                        user.ID,
                        order.PaymentMethodId,
                        cardName,
                        order.TransactionCardType,
                        order.TransactionCardNumber,
                        string.Format("{0}|{1}", customerId, cardId)
                    );

                    order.SavedCardId = savedCard.ID;
                }
                Services.Orders.Save(order);
            }

            if (!order.IsRecurringOrderTemplate)
            {
                InitiatePaymentIntent(order, rqstCharge);
            }
            else
            {
                SetOrderComplete(order);
                CheckoutDone(order);
            }

            RedirectToCart(order);
            return null;
        }

        private string PrintErrorTemplate(Order order, string msg, ErrorType errorType = ErrorType.Undefined)
        {
            LogEvent(order, "Printing error template");

            order.TransactionAmount = 0;
            order.TransactionStatus = "Failed";
            order.Errors.Add(msg);
            Services.Orders.Save(order);

            Services.Orders.DowngradeToCart(order);
            order.CartV2StepIndex = 0;
            order.TransactionStatus = "";
            Dynamicweb.Ecommerce.Common.Context.SetCart(order);

            if (string.IsNullOrWhiteSpace(ErrorTemplate))
            {
                RedirectToCart(order);
            }

            var errorTemplate = new Template(TemplateHelper.GetTemplatePath(ErrorTemplate, ErrorTemplateFolder));
            errorTemplate.SetTag("CheckoutHandler:ErrorType", errorType.ToString());
            errorTemplate.SetTag("CheckoutHandler:ErrorMessage", msg);

            return Render(order, errorTemplate);
        }

        #region Stripe API

        private void InitiatePaymentIntent(Order order, Dictionary<string, object> chargeRequest)
        {
            if (order.Complete)
            {
                return;
            }

            try
            {
                LogEvent(order, "Starting payment process");
                var responce = ExecuteRequest("payment_intents", chargeRequest, "POST", string.Format("{0}:{1}", MerchantName, order.Id));
                var status = Converter.ToString(responce["status"]);

                if (status.Equals("requires_source_action"))
                {
                    LogEvent(order, "Stripe requested tranaction authorization");
                    var nextAction = (dynamic)responce["next_action"];
                    var redirectInfo = (dynamic)nextAction["redirect_to_url"];
                    var redirectUrl = Converter.ToString(redirectInfo["url"]);
                    Context.Current.Response.Redirect(redirectUrl);
                }

                CompleteOrder(order, responce);
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
            {
                throw new Exception("Called create charge, but order is not set complete.");
            }
        }

        private void CompleteOrder(Order order, Dictionary<string, object> intentResponce)
        {
            var charges = (dynamic)((dynamic)intentResponce["charges"])["data"];
            var chargeObject = (dynamic)charges[0];
            var chargeStatus = Converter.ToString(chargeObject["status"]);
            if (chargeStatus.Equals("succeeded"))
            {
                var transactionId = Converter.ToString(intentResponce["id"]);
                LogEvent(order, "Payment succeeded with transaction number {0}", transactionId);

                var transactionCardType = Converter.ToString(chargeObject["payment_method_details"]["card"]["brand"]);
                var transactionCardNumber = Converter.ToString(chargeObject["payment_method_details"]["card"]["last4"]);
                var captured = Converter.ToBoolean(chargeObject["captured"]);
                var transactionAmount = Converter.ToInt32(chargeObject["amount"]) / 100d;

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

        private void StateComplete(Order order)
        {
            try
            {
                LogEvent(order, "Retrieving payment intent object");
                var paymentIntentId = Converter.ToString(Dynamicweb.Context.Current.Request["payment_intent"]);
                string request = "payment_intents";
                if (!string.IsNullOrEmpty(paymentIntentId))
                {
                    request += $"/{paymentIntentId}";
                }
                var intentResponce = ExecuteRequest(request, null, "GET");
                CompleteOrder(order, intentResponce);
            }
            finally
            {
                CheckoutDone(order);
            }

            if (!order.Complete)
            {
                throw new Exception("Order completed at Stripe, but order is not set complete in dynamicweb side.");
            }

            RedirectToCart(order);
        }

        private Dictionary<string, object> ExecuteRequest(string function, Dictionary<string, object> body, string method = "POST", string idempotencyKey = null)
        {
            var request = WebRequest.Create(string.Format("https://api.stripe.com/v1/{0}", function));
            request.Method = method;
            request.Credentials = new NetworkCredential(TestMode ? TestSecretKey : LiveSecretKey, ""); ;
            request.PreAuthenticate = true;
            request.Timeout = 90 * 1000;
            request.ContentType = "application/x-www-form-urlencoded"; // "application/json;charset=UTF-8"; //
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers["Idempotency-Key"] = idempotencyKey;
            }

            if (body != null)
            {
                var strBody = SerializeToString(body);
                byte[] bytes = Encoding.UTF8.GetBytes(strBody);
                request.ContentLength = bytes.Length;
                using (Stream st = request.GetRequestStream())
                {
                    st.Write(bytes, 0, bytes.Length);
                }
            }

            string result = ExecuteRequest(request);

            return Converter.Deserialize<Dictionary<string, object>>(result); ;
        }

        private void ExecuteDeleteRequest(string function)
        {
            var request = WebRequest.Create(string.Format("https://api.stripe.com/v1/{0}", function));
            request.Method = "DELETE";
            request.Credentials = new NetworkCredential(TestMode ? TestSecretKey : LiveSecretKey, ""); ;
            request.PreAuthenticate = true;

            ExecuteRequest(request);
        }

        private string ExecuteRequest(WebRequest request)
        {
            string result = null;
            try
            {
                using (var response = request.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException wexc)
            {
                if (wexc.Response != null)
                {
                    string response;
                    using (StreamReader sr = new StreamReader(wexc.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        response = sr.ReadToEnd();
                    }
                    var json_error = Converter.Deserialize<Dictionary<string, object>>(response);
                    var error = (dynamic)json_error["error"];

                    var errorMessage = string.IsNullOrEmpty(Converter.ToString(error["code"])) ? Converter.ToString(error["message"]) : string.Format("Error code {0}: {1}", Converter.ToString(error["code"]), Converter.ToString(error["message"]));
                    throw new Exception(errorMessage);
                }
                throw;
            }

            return result;
        }

        private string SerializeToString(Dictionary<string, object> body)
        {
            var stringBuilder = new StringBuilder();

            foreach (KeyValuePair<string, object> pair in body)
            {
                stringBuilder.AppendFormat("{0}={1}&", pair.Key, WebUtility.UrlEncode(Converter.ToString(pair.Value)));
            }
            stringBuilder.Length--;

            return stringBuilder.ToString();
        }

        #endregion

        #region ISavedCard interface

        /// <summary>
        /// Deletes saved card
        /// </summary>
        /// <param name="savedCardID">Identifier of saved card to be deleted</param>
        public void DeleteSavedCard(int savedCardID)
        {
            var savedCard = Services.PaymentCard.GetById(savedCardID);
            if (savedCard != null)
            {
                var token = savedCard.Token.Split('|');
                try
                {
                    if (token.Length > 1)
                        ExecuteDeleteRequest(string.Format("customers/{0}/sources/{1}", token[0], token[1]));
                    else
                        ExecuteDeleteRequest(string.Format("customers/{0}", token[0]));
                }
                catch (Exception ex)
                {
                    LogError(null, ex, "Delete saved card exception: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Directs checkout handler to use saved card
        /// </summary>
        /// <param name="order">Order that should be processed using saved card information</param>
        /// <returns>Empty string, if operation succeeded, otherwise string template with exception mesage</returns>
        public string UseSavedCard(Order order)
        {
            try
            {
                UseSavedCardInternal(order);

                RedirectToCart(order);
                return string.Empty;
            }
            catch (System.Threading.ThreadAbortException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogEvent(order, ex.Message, DebuggingInfoType.UseSavedCard);
                return PrintErrorTemplate(order, ex.Message, ErrorType.SavedCard);
            }
        }

        /// <summary>
        /// Shows if order supports saving card
        /// </summary>
        /// <param name="order">Instance of order</param>
        /// <returns>True, if saving card is supported</returns>
        public bool SavedCardSupported(Order order)
        {
            return true;
        }

        private void UseSavedCardInternal(Order order)
        {
            var savedCard = Services.PaymentCard.GetById(order.SavedCardId);
            if (savedCard == null || order.CustomerAccessUserId != savedCard.UserID)
            {
                throw new PaymentCardTokenException("Token is incorrect.");
            }

            LogEvent(order, "Using saved card({0}) with id: {1}", savedCard.Identifier, savedCard.ID);

            if (order.IsRecurringOrderTemplate)
            {
                order.TransactionCardType = savedCard.CardType;
                order.TransactionCardNumber = savedCard.Identifier;
                SetOrderComplete(order);
                CheckoutDone(order);
            }
            else
            {
                var token = savedCard.Token.Split('|');
                var requestCharge = new Dictionary<string, object>
                {
                    { "amount", order.Price.PricePIP },
                    { "currency", order.CurrencyCode },
                    { "description", order.Id },
                    { "customer",  token[0] },
                    { "capture_method", CaptureNow ? "automatic" : "manual" },
                    { "confirm", true}
                };
                if (token.Length > 1)
                {
                    requestCharge["payment_method_types[0]"] = "card";
                    requestCharge["payment_method"] = token[1];
                    requestCharge["off_session"] = true;
                }
                try
                {
                    var intentResponce = ExecuteRequest("payment_intents", requestCharge, "POST", string.Format("{0}:{1}", MerchantName, order.Id));
                    CompleteOrder(order, intentResponce);
                }
                finally
                {
                    CheckoutDone(order);
                }

                if (!order.Complete)
                {
                    throw new Exception("Called create charge, but order is not set complete.");
                }

                RedirectToCart(order);
            }
        }

        #endregion

        #region IRemoteCapture

        /// <summary>
        /// Send capture request to transaction service
        /// </summary>
        /// <param name="order">Order to be captured</param>
        /// <returns>Response from transaction service</returns>
        public OrderCaptureInfo Capture(Order order)
        {
            return Capture(order, order.Price.PricePIP, true);
        }

        public OrderCaptureInfo Capture(Order order, long amount, bool final)
        {
            try
            {
                // Check order
                if (order == null)
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

                Dictionary<string, object> rqstCapture = null;
                // Amount to capture. Specify it explicitly when it less than order amount (can be splitted only once)
                if (amount <= order.Price.PricePIP)
                {
                    rqstCapture = new Dictionary<string, object>
                    {
                        ["amount"] = amount
                    };
                }

                var capture = ExecuteRequest(string.Format("payment_intents/{0}/capture", order.TransactionNumber), rqstCapture);

                LogEvent(order, "Remote capture status: {0}", capture["status"]);

                if (string.Compare(Converter.ToString(capture["status"]), "succeeded", true) == 0)
                {
                    if (order.Price.PricePIP == amount)
                    {
                        LogEvent(order, "Capture successful", DebuggingInfoType.CaptureResult);
                        return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
                    }
                    else
                    {
                        LogEvent(order, String.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Split capture(final)", amount / 100f), DebuggingInfoType.CaptureResult);
                        return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Split capture successful");
                    }
                }
                else
                {
                    // Not success
                    string infoTxt = string.Format("Remote Capture failed. Error code: {0}, mmessage: {1}", capture["failure_code"], capture["failure_message"]);

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
        public bool CaptureSupported(Order order)
        {
            return true;
        }

        /// <summary>
        /// Shows if partial capture of the order supported
        /// </summary>
        /// <param name="order">Not used</param>
        /// <returns>Always returns true</returns>
        public bool SplitCaptureSupported(Order order)
        {
            return true;
        }

        #endregion

        #region IRecurring

        /// <summary>
        /// Creates new payment for recurring order
        /// </summary>
        /// <param name="order">recurring order to be used for payment</param>
        /// <param name="initialOrder">Base order, used for creating current recurring order</param>
        public void Recurring(Order order, Order initialOrder)
        {
            if (order != null)
            {
                try
                {
                    UseSavedCardInternal(order);
                    LogEvent(order, "Recurring succeeded");
                }
                catch (Exception ex)
                {
                    LogEvent(order, "Recurring order failed for {0} (based on {1}). The payment failed with the message: {2}",
                        DebuggingInfoType.RecurringError, order.Id, initialOrder.Id, ex.Message);
                }
            }
        }

        /// <summary>
        /// Shows if order supports recurring payments
        /// </summary>
        /// <param name="order">Instance of order</param>
        /// <returns>True, if recurring payments are supported</returns>
        public bool RecurringSupported(Order order)
        {
            return true;
        }

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
                switch (parameterName)
                {
                    case "Post mode":
                        return new List<ParameterOption> {
                            new( "Auto", "Auto post (does not use the template)" ),
                            new( "Template", "Render template" )
                        };

                    case "Language":
                        return new List<ParameterOption> {
                            new( "auto", "auto" ),
                            new("zh", "Chinese" ),
                            new("nl", "Dutch" ),
                            new("en", "English" ),
                            new ( "fr", "French" ),
                            new ( "de", "German" ),
                            new("it", "Italian" ),
                            new("jp", "Japanese" ),
                            new("es", "Spanish" )
                        };

                    default:
                        throw new ArgumentException(string.Format("Unknown dropdown name: '{0}'", parameterName));
                }

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

                var reqeuestBody = new Dictionary<string, object> { ["charge"] = order.TransactionNumber };

                if (amount != null)
                {
                    reqeuestBody.Add("amount", amount.ToString());
                }

                var returnResponce = ExecuteRequest("refunds", reqeuestBody);
                LogEvent(order, "Remote return status: {0}", returnResponce["status"]);

                if (string.Compare(Converter.ToString(returnResponce["status"]), "succeeded", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    LogEvent(order, "Return successful", DebuggingInfoType.ReturnResult);
                    return null;
                }

                errorMessage = "Return operation failed.";
                // Not success
                if (returnResponce.ContainsKey("failure_code") && returnResponce.ContainsKey("failure_message"))
                {
                    errorMessage += $" Error code: {returnResponce["failure_code"]}, mmessage: {returnResponce["failure_message"]}";
                }

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
        public void PartialReturn(Order order, Order originalOrder)
        {
            ProceedReturn(originalOrder, order?.Price?.PricePIP);
        }

        public void FullReturn(Order order)
        {
            ProceedReturn(order);
        }

        private void ProceedReturn(Order order, long? amount = null)
        {
            // Check order
            if (order == null)
            {
                LogError(null, "Order not set");
                return;
            }

            var operationAmount = amount == null ? order.CaptureAmount : Converter.ToDouble(amount) / 100;

            if (order.CaptureInfo == null || order.CaptureInfo.State != OrderCaptureInfo.OrderCaptureState.Success || order.CaptureAmount <= 0.00)
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, "Order must be captured before return.", order.CaptureAmount, order);
                LogError(null, "Order must be captured before return.");
                return;
            }

            if (amount != null && order.CaptureAmount < operationAmount)
            {
                OrderReturnInfo.SaveReturnOperation(
                    OrderReturnOperationState.Failed,
                    $"Order captured amount({Services.Currencies.Format(order.Currency, order.CaptureAmount)}) less than amount requested for return{Services.Currencies.Format(order.Currency, operationAmount)}.",
                    operationAmount,
                    order);
                LogError(order, "Order captured amount less then amount requested for return.");
                return;
            }

            var errorMessage = Refund(order, amount);
            if (string.IsNullOrEmpty(errorMessage))
            {
                OrderReturnInfo.SaveReturnOperation(amount == null ? OrderReturnOperationState.FullyReturned : OrderReturnOperationState.PartiallyReturned, "Stripe has refunded payment.", operationAmount, order);
            }
            else
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorMessage, operationAmount, order);
            }

        }
        #endregion

        #region ICancelOrder
        public bool CancelOrder(Order order)
        {
            var errorMessage = Refund(order);
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
                return RenderPaymentForm(order, formTemplate);
            }

            return string.Empty;
        }

        private string RenderPaymentForm(Order order, Template formTemplate)
        {
            try
            {
                var formValues = new Dictionary<string, string> {
                        { "publishablekey", TestMode ? TestPublishableKey : LivePublishableKey },
                        { "language", Language },
                        { "name", MerchantName },
                        { "image", string.Format("/Files/{0}", MerchantLogo) },
                        { "description", string.Format("Order: {0}", order.Id) },
                        { "currency", order.CurrencyCode },
                        { "amount", order.Price.PricePIP.ToString() },
                        { "email", order.CustomerEmail }
                    };

                foreach (var formValue in formValues)
                {
                    formTemplate.SetTag(string.Format("Stripe.{0}", formValue.Key), formValue.Value);
                }

                if (order.DoSaveCardToken)
                {
                    formTemplate.SetTag("SavedCardCreate", "true");
                }

                // Render and return
                return Render(order, formTemplate);
            }
            catch (System.Threading.ThreadAbortException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Unhandled exception with message: {0}", ex.Message);
                return PrintErrorTemplate(order, ex.Message);
            }
        }

       
        #endregion
    }
}
