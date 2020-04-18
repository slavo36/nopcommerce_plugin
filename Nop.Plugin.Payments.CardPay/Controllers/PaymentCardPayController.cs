using Castle.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.CardPay.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.CardPay.Controllers
{
    public class PaymentCardPayController : BasePaymentController
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public PaymentCardPayController(IGenericAttributeService genericAttributeService,
           IOrderProcessingService orderProcessingService,
           IOrderService orderService,
           IPaymentPluginManager paymentPluginManager,
           IPermissionService permissionService,
           ILocalizationService localizationService,
           INotificationService notificationService,
           ISettingService settingService,
           IStoreContext storeContext,
           IWebHelper webHelper,
           IWorkContext workContext,
           ShoppingCartSettings shoppingCartSettings)
        {
            _genericAttributeService = genericAttributeService;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _shoppingCartSettings = shoppingCartSettings;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var cardPayStandardPaymentSettings = _settingService.LoadSetting<CardPayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                Mid = cardPayStandardPaymentSettings.Mid,
                Key = cardPayStandardPaymentSettings.Key,
                ReturnUrl = cardPayStandardPaymentSettings.ReturnUrl,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.CardPay/Views/Configure.cshtml", model);

            model.Mid_OverrideForStore = _settingService.SettingExists(cardPayStandardPaymentSettings, x => x.Mid, storeScope);
            model.Key_OverrideForStore = _settingService.SettingExists(cardPayStandardPaymentSettings, x => x.Key, storeScope);
            model.ReturnUrl_OverrideForStore = _settingService.SettingExists(cardPayStandardPaymentSettings, x => x.ReturnUrl, storeScope);

            return View("~/Plugins/Payments.CardPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var cardPayStandardPaymentSettings = _settingService.LoadSetting<CardPayPaymentSettings>(storeScope);

            //save settings
            cardPayStandardPaymentSettings.Mid = model.Mid;
            cardPayStandardPaymentSettings.Key = model.Key;
            cardPayStandardPaymentSettings.ReturnUrl = model.ReturnUrl;


            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(cardPayStandardPaymentSettings, x => x.Mid, model.Mid_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cardPayStandardPaymentSettings, x => x.Key, model.Key_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cardPayStandardPaymentSettings, x => x.ReturnUrl, model.Key_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
