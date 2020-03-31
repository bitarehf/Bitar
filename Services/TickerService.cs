using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Helpers;
using Bitar.Hubs;
using Bitar.Models;
using KrakenCore;
using KrakenCore.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bitar.Services
{
    public class TickerService : IHostedService
    {
        private readonly ILogger<TickerService> _logger;
        private readonly IHubContext<TickerHub> _hubContext;
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private Timer _timer;
        public MarketState MarketState { get; private set; }
        public Dictionary<string, Ticker> Tickers = new Dictionary<string, Ticker>();
        private TickerInfo _btceur;

        public TickerService(
            ILogger<TickerService> logger,
            IHubContext<TickerHub> hubContext,
            LandsbankinnService landsbankinn,
            KrakenService kraken,
            OhlcService ohlc)
        {
            _logger = logger;
            _hubContext = hubContext;
            _landsbankinn = landsbankinn;
            _kraken = kraken;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TickerService is starting.");

            _timer = new Timer(UpdateTickers, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TickerService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            await Task.CompletedTask;
        }

        public async void UpdateTickers(object state)
        {
            //decimal eurisk = decimal.Zero;
            //decimal usdisk = decimal.Zero;

            var bankTickers = await _landsbankinn.FetchCurrencyUpdates();

            if (bankTickers == null)
            {
                _logger.LogCritical($"Failed to update landsbankinnTickers.");
                CloseMarket();
                return;
            }

            foreach (var ticker in bankTickers)
            {
                if (ticker.Value.Ask == decimal.Zero || ticker.Value.Bid == decimal.Zero)
                {
                    _logger.LogCritical($"Failed to update tickers. Key: {ticker.Key} Ticker: {ticker.Value.Ask} : {ticker.Value.Bid} {ticker.Value.LastUpdated}.");
                    CloseMarket();
                    return;
                }
            }

            Tickers["eurisk"] = new Ticker
            {
                Ask = bankTickers["eurisk"].Ask,
                Bid = bankTickers["eurisk"].Bid,
                LastUpdated = DateTime.Now
            };

            Tickers["usdisk"] = new Ticker
            {
                Ask = bankTickers["usdisk"].Ask,
                Bid = bankTickers["usdisk"].Bid,
                LastUpdated = DateTime.Now
            };


            var btceur = await _kraken.GetTickerInformation("XBTEUR");
            if (btceur == null)
            {
                _logger.LogCritical($"Failed to update btceur {btceur}.");
                CloseMarket();
                return;
            }

            _btceur = btceur;

            Tickers["btcisk"] = new Ticker
            {
                Ask = decimal.Ceiling(Tickers["eurisk"].Ask * btceur.Ask[0] * 1.03m + 100m),
                Bid = decimal.Floor(Tickers["eurisk"].Bid * btceur.Bid[0] * 0.99m - 100m),
                DailyChange = await getDailyChange(),
                LastUpdated = DateTime.Now
            };

            OpenMarket();

            // Send update to all SignalR Clients.
            await _hubContext.Clients.All.SendAsync("TickersUpdated", Tickers);

            _logger.LogInformation($"Tickers Updated: {DateTime.Now}");
        }

        public async Task<decimal> getDailyChange()
        {
            var t = await _kraken.UpdateOhlc(60);
            var g = t.Ohlc.ToList().OrderBy(m =>
                Math.Abs((
                    DateTime.Now.AddDays(-1) -
                    Converters.UnixTimestampToDateTime(m.Time)
                    ).TotalMilliseconds)).First();

            return (_btceur.Ask[0] - g.Open) * Tickers["eurisk"].Ask;
        }

        public void OpenMarket()
        {
            if (MarketState != MarketState.Open)
            {
                _logger.LogCritical("Market opened.");
                MarketState = MarketState.Open;
            }
        }

        public void CloseMarket()
        {
            if (MarketState != MarketState.Closed)
            {
                _logger.LogCritical("Market closed.");
                MarketState = MarketState.Closed;
            }
        }
    }

    public class Ticker
    {
        public decimal Ask { get; set; }
        public decimal Bid { get; set; }
        public decimal DailyChange { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public enum MarketState
    {
        Open,
        Closed
    }


}