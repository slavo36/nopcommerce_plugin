using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.CardPay.Models
{
    public class ConfigurationModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }
        public string Mid { get; set; }
        public bool Mid_OverrideForStore { get; set; }
        public string Key { get; set; }
        public bool Key_OverrideForStore { get; set; }
        public string ReturnUrl { get; set; }
        public bool ReturnUrl_OverrideForStore { get; set; }
    }
}
