using Microsoft.Net.Http.Headers;
using Nop.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Nop.Plugin.Payments.CardPay
{
    public class CardPayHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly CardPayPaymentSettings _cardPayPaymentSettings;

        public CardPayHttpClient(HttpClient client,
            CardPayPaymentSettings cardPayPaymentSettings)
        {
            //configure client
            client.Timeout = TimeSpan.FromMilliseconds(5000);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"nopCommerce-{NopVersion.CurrentVersion}");

            _httpClient = client;
            _cardPayPaymentSettings = cardPayPaymentSettings;
        }
    }
}