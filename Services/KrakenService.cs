using System;
using System.Threading.Tasks;
using Bitar.Models.Settings;
using KrakenCore;
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

        public async Task<decimal> FetchBTCEUR()
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