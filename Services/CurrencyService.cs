using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Hubs;
using KrakenCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bitar.Services
{
    public class CurrencyService : BackgroundService
    {
        private readonly ILogger<CurrencyService> _logger;
        private readonly IHubContext<CurrencyHub> _hubContext;
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private readonly TimeSpan _delay = TimeSpan.FromMinutes(1);
        public Dictionary<string, decimal> Currencies { get; set; }

        public CurrencyService(ILogger<CurrencyService> logger,
            IHubContext<CurrencyHub> hubContext,
            LandsbankinnService landsbankinn,
            KrakenService kraken)
        {
            _logger = logger;
            _hubContext = hubContext;
            _landsbankinn = landsbankinn;
            _kraken = kraken;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CurrencyService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fetching currency updates.");

                Currencies = await FetchCurrencyUpdates();

                foreach (var currency in Currencies)
                {
                    _logger.LogInformation($"Currency update: {currency.Key} {currency.Value}.");
                }

                await Broadcast(Currencies);

                await Task.Delay(_delay, stoppingToken);
            }
        }

        private async Task<Dictionary<string, decimal>> FetchCurrencyUpdates()
        {
            var data = new Dictionary<string, decimal>
            {
                {"ISK", await _landsbankinn.FetchISKEUR()},
                {"BTC", await _kraken.FetchBTCEUR()}
            };

            return await Task.FromResult(data);
        }

        private async Task Broadcast(Dictionary<string, decimal> data)
        {
            _logger.LogCritical("Broadcasting updates");
            await _hubContext.Clients.All.SendAsync("currenciesUpdated", new object[] { data });
        }


    }
}