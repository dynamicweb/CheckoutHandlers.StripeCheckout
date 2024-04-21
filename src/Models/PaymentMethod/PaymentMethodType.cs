using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.StripeCheckout.Models.PaymentIntent;

[DataContract]
public enum PaymentMethodType
{
    [EnumMember(Value = "acss_debit")]
    AcssDebit,

    [EnumMember(Value = "affirm")]
    Affirm,

    [EnumMember(Value = "afterpay_clearpay")]
    AfterpayClearpay,

    [EnumMember(Value = "alipay")]
    Alipay,

    [EnumMember(Value = "amazon_pay")]
    AmazonPay,

    [EnumMember(Value = "au_becs_debit")]
    AuBecsDebit,

    [EnumMember(Value = "bacs_debit")]
    BacsDebit,

    [EnumMember(Value = "bancontact")]
    Bancontact,

    [EnumMember(Value = "blik")]
    Blik,

    [EnumMember(Value = "boleto")]
    Boleto,

    [EnumMember(Value = "card")]
    Card,

    [EnumMember(Value = "card_present")]
    CardPresent,

    [EnumMember(Value = "cashapp")]
    Cashapp,

    [EnumMember(Value = "customer_balance")]
    CustomerBalance,

    [EnumMember(Value = "eps")]
    Eps,

    [EnumMember(Value = "fpx")]
    Fpx,

    [EnumMember(Value = "giropay")]
    Giropay,

    [EnumMember(Value = "grabpay")]
    Grabpay,

    [EnumMember(Value = "ideal")]
    Ideal,

    [EnumMember(Value = "interac_present")]
    InteracPresent,

    [EnumMember(Value = "klarna")]
    Klarna,

    [EnumMember(Value = "konbini")]
    Konbini,

    [EnumMember(Value = "link")]
    Link,

    [EnumMember(Value = "mobilepay")]
    Mobilepay,

    [EnumMember(Value = "oxxo")]
    Oxxo,

    [EnumMember(Value = "p24")]
    P24,

    [EnumMember(Value = "paynow")]
    Paynow,

    [EnumMember(Value = "paypal")]
    Paypal,

    [EnumMember(Value = "pix")]
    Pix,

    [EnumMember(Value = "promptpay")]
    Promptpay,

    [EnumMember(Value = "revolut_pay")]
    RevolutPay,

    [EnumMember(Value = "sepa_debit")]
    SepaDebit,

    [EnumMember(Value = "sofort")]
    Sofort,

    [EnumMember(Value = "swish")]
    Swish,

    [EnumMember(Value = "us_bank_account")]
    UsBankAccount,

    [EnumMember(Value = "wechat_pay")]
    WechatPay,

    [EnumMember(Value = "zip")]
    Zip
}
