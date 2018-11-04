using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Hubs;
using Bitar.Models;
using KrakenCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bitar.Services
{
    public class StockService : IHostedService
    {
        private readonly ILogger<StockService> _logger;
        private readonly IHubContext<StockHub> _hubContext;
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private Timer _timer;
        private decimal BTC { get; set; }
        private decimal ISK { get; set; }
        public List<Stock> Stocks { get; set; }
        public MarketState MarketState { get; set; }

        public StockService(
            ILogger<StockService> logger,
            IHubContext<StockHub> hubContext,
            LandsbankinnService landsbankinn,
            KrakenService kraken)
        {
            _logger = logger;
            _hubContext = hubContext;
            _landsbankinn = landsbankinn;
            _kraken = kraken;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StockService is starting.");

            _timer = new Timer(UpdateStockPrices, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StockService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            await Task.CompletedTask;
        }

        public async void UpdateStockPrices(object state)
        {
            List<Stock> stocks = await _landsbankinn.FetchCurrencyUpdates();

            decimal eurisk = decimal.Zero;
            foreach (var stock in stocks)
            {
                if (stock.Price == decimal.Zero)
                {
                    _logger.LogCritical($"Failed to update {stock.Symbol}");
                    CloseMarket();
                }

                if (stock.Symbol == Symbol.EUR)
                {
                    eurisk = stock.Price;
                    break;
                }
            }

            decimal btceur = await _kraken.FetchBTCEUR();
            if (btceur == decimal.Zero)
            {
                _logger.LogCritical("Failed to update btceur.");
                CloseMarket();
            }

            decimal btcisk = eurisk * btceur;

            stocks.Add(new Stock() { Symbol = Symbol.BTC, Price = btcisk });
            Stocks = stocks;
            OpenMarket();

            // Send update to all SignalR Clients.
            await _hubContext.Clients.All.SendAsync("StocksUpdated", Stocks);
        }

        public void OpenMarket()
        {
            _logger.LogCritical("Market opened.");
            MarketState = MarketState.Open;
        }
        public void CloseMarket()
        {
            _logger.LogCritical("Market closed.");
            MarketState = MarketState.Closed;
        }

        // public async Task<List<Stock>> UpdateCurrencies(object state)
        // {
        //     List<Stock> currencies = await _landsbankinn.FetchCurrencyUpdates();
        //     return currencies;
        // }
    }

    public class Stock
    {
        public decimal Price { get; set; }
        public Symbol Symbol { get; set; }
    }

    public enum Symbol
    {
        BTC,
        ISK,
        EUR,
        USD
    }

    public enum MarketState
    {
        Open,
        Closed
    }


}