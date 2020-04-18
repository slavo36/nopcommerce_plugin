using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Payments.CardPay
{
    public class CardPayProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly CardPayHttpClient _cardPayHttpClient;
        private readonly CardPayPaymentSettings _cardPayPaymentSettings;

        #endregion

        public CardPayProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            CardPayHttpClient cardPayHttpClient,
            CardPayPaymentSettings cardPayPaymentSettings
            )
        {
            _currencySettings = currencySettings;
            _checkoutAttributeParser = checkoutAttributeParser;
            _currencyService = currencyService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _taxService = taxService;
            _webHelper = webHelper;
            _cardPayHttpClient = cardPayHttpClient;
            _cardPayPaymentSettings = cardPayPaymentSettings;
        }
        public bool SupportCapture => false;

        public bool SupportPartiallyRefund => false;

        public bool SupportRefund => false;

        public bool SupportVoid => false;

        public RecurringPaymentType RecurringPaymentType =>  RecurringPaymentType.NotSupported;

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => false;

        public string PaymentMethodDescription => "Platba cez CardPay";


        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        public bool CanRePostProcessPayment(Order order)
        {
            return true;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentCardPay/Configure";
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentCardPay";
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var mid = _cardPayPaymentSettings.Mid;
            var amt = postProcessPaymentRequest.Order.OrderTotal.ToString("####.00", CultureInfo.InvariantCulture); ;
            var curr = "978";
            var vs = postProcessPaymentRequest.Order.Id;
            var rurl = _cardPayPaymentSettings.ReturnUrl;
            var ipc = _httpContextAccessor.HttpContext.Connection.LocalIpAddress;
            var name = RemoveDiacritics(postProcessPaymentRequest.Order.Customer.ShippingAddress.FirstName + " " + postProcessPaymentRequest.Order.Customer.ShippingAddress.LastName);
            var timestamp = DateTimeFormat(DateTime.Now);
            var toSign = mid + amt + curr + vs + rurl + ipc + name + timestamp; 
            var hmac = CardPaySign(toSign, _cardPayPaymentSettings.Key);
            var url = string.Format("https://moja.tatrabanka.sk/cgi-bin/e-commerce/start/cardpay?MID={0}&AMT={1}&CURR={2}&VS={3}&RURL={4}&IPC={5}&NAME={6}&TIMESTAMP={7}&HMAC={8}", mid, amt, curr, vs, rurl, ipc, name, timestamp, hmac);

            _httpContextAccessor.HttpContext.Response.Redirect(url);
        }

        #region CardPay_helpers
        public string ByteArrayToHexString(byte[] byteArray)
        {
            StringBuilder hex = new StringBuilder(byteArray.Length * 2);
            foreach (byte b in byteArray)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        public byte[] HexStringTo64ByteArray(string hex)
        {
            int numOfChars = hex.Length;
            byte[] bytes = new byte[64];
            for (int i = 0; i < numOfChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        public string CardPaySign(string toSign, string hexEncryptKey)
        {
            byte[] toHash = Encoding.ASCII.GetBytes(toSign);
            byte[] key = HexStringTo64ByteArray(hexEncryptKey);
            byte[] hash;
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                hash = hmac.ComputeHash(toHash);
            }

            return ByteArrayToHexString(hash);
        }

        private string DateTimeFormat(DateTime dt)
        {
            var sb = new StringBuilder();
            sb.Append(dt.Day.ToString("00"));
            sb.Append(dt.Month.ToString("00"));
            sb.Append(dt.Year);
            sb.Append(dt.Hour.ToString("00"));
            sb.Append(dt.Minute.ToString("00"));
            sb.Append(dt.Second.ToString("00"));
            return sb.ToString();
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
        #endregion

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }
    }
}
