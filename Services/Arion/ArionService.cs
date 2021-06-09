using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bitar.Services
{

    public class ArionService
    {
        private readonly ILogger<ArionService> _logger;
        private readonly HttpClient _client;
        private ArionCurrencyPair CurrencyPair;

        public ArionService(ILogger<ArionService> logger, HttpClient client)
        {
            _logger = logger;
            client.BaseAddress = new Uri("https://arionbanki.is/Webservice/PortalCurrency.ashx");
            _client = client;
        }

        public async Task<List<Asset>> GetArionRates(string pair, string dateFrom, string dateTo)
        {
            try
            {
                string coin;

                if (pair == "eurisk")
                {
                    coin = "eur";
                }
                else if (pair == "usdisk")
                {
                    coin = "usd";
                }
                else if (pair == "gbpisk")
                {
                    coin = "gbp";
                }
                else
                {
                    return null;
                }

                var response = await _client.GetAsync($"?m=GetBankCurrency&coin={coin}&currencyType=65&dateFrom={dateFrom}&dateTo={dateTo}");

                response.EnsureSuccessStatusCode();

                using var responseStream = await response.Content.ReadAsStreamAsync();
                var arionCurrencyPairs = await JsonSerializer.DeserializeAsync<List<ArionCurrencyPair>>(responseStream);

                List<Asset> assets = new List<Asset>();

                foreach (var arionPair in arionCurrencyPairs)
                {
                    assets.Add(new Asset
                    {
                        Ask = arionPair.AskValue,
                        Bid = arionPair.BidValue,
                        Time = arionPair.Time
                    });
                }

                return assets;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

    }

    public class ArionCurrencyPair
    {
        public decimal AskValue { get; set; }
        public decimal BidValue { get; set; }
        public decimal CustomsRate { get; set; }
        public decimal LastValueChange { get; set; }
        public object MainTicker { get; set; }
        public decimal MidValue { get; set; }
        public string Ticker { get; set; }
        public DateTime Time { get; set; }
        public object Title { get; set; }
    }
}