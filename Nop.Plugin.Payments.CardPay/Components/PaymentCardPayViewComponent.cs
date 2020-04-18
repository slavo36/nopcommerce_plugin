using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CardPay.Components
{
    [ViewComponent(Name = "PaymentCardPay")]
    public class PaymentCardPayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.CardPay/Views/PaymentInfo.cshtml");
        }
    }
}
