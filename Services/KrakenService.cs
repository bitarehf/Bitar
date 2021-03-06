using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Models.Settings;
using KrakenCore;
using KrakenCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bitar.Services
{

    public class KrakenService
    {
        private readonly ILogger<KrakenService> _logger;
        private readonly KrakenSettings _options;
        private readonly KrakenClient _client;

        public KrakenService(ILogger<KrakenService> logger, IOptions<KrakenSettings> options)
        {
            _logger = logger;
            _options = options.Value;
            _client = new KrakenClient(_options.ApiKey, _options.PrivateKey);
        }

        public async Task<OhlcPair> UpdateOhlc(int interval)
        {
            try
            {
                var response = await _client.GetOhlcData("XBTEUR", interval);

                foreach (var item in response.Result)
                {
                    if (item.Key == "XXBTZEUR")
                    {
                        return new OhlcPair
                        {
                            Pair = "BTCEUR",
                            Last = response.Result.Last,
                            Ohlc = item.Value
                        };
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        public async Task<TickerInfo> GetTickerInformation(string ticker)
        {
            try
            {
                var response = await _client.GetTickerInformation(ticker);
                return response.Result.Select(c => c.Value).First();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        public async Task<decimal> GetBTCEUR()
        {
            try
            {
                var response = await _client.GetTickerInformation("XBTEUR");
                foreach (var item in response.Result)
                {
                    return item.Value.Ask[0];
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return Decimal.Zero;
        }
    }
}