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
        private readonly IHubContext<CurrencyHub> _hubContext;
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private Timer _timer;
        private decimal BTC { get; set; }
        private decimal ISK { get; set; }
        public List<Stock> Stocks { get; set; }
        public MarketState MarketState { get; set; }

        public StockService(
            ILogger<StockService> logger,
            IHubContext<CurrencyHub> hubContext,
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
            MarketState = MarketState.Open;
            _timer = new Timer(UpdateStockPrices, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CurrencyService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            await Task.CompletedTask;
        }

        public async void UpdateStockPrices(object state)
        {
            List<Stock> currencies = await _landsbankinn.FetchCurrencyUpdates();
            decimal btceur = await _kraken.FetchBTCEUR();
            decimal eurisk = decimal.Zero;

            foreach (var currency in currencies)
            {
                if (currency.Symbol == Symbol.EUR)
                {
                    eurisk = currency.Price;
                    break;
                }
            }
            
            decimal btcisk = eurisk * btceur;

            Stocks = new List<Stock>()
            {
                { new Stock() { Symbol = Symbol.BTC, Price = btcisk }}
            };
            
            Stocks.AddRange(currencies);

            // Send update to all SignalR Clients.
            await _hubContext.Clients.All.SendAsync("StocksUpdated", Stocks);
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