using Nop.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.CardPay
{
    public class CardPayPaymentSettings : ISettings
    {
        public string Mid { get; set; }
        public string Key { get; set; }
        public string ReturnUrl { get; set; }
    }
}
