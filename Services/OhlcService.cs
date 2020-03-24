using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Helpers;
using Bitar.Hubs;
using Bitar.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bitar.Services
{
    public class OhlcService : IHostedService
    {
        private readonly ILogger<OhlcService> _logger;
        private readonly IHubContext<StockHub> _hubContext;
        private readonly KrakenService _kraken;
        private Timer _timer;
        public OhlcPair Ohlc { get; set; }
        public ChartPair ChartPair { get; set; }

        public OhlcService(
            ILogger<OhlcService> logger,
            IHubContext<StockHub> hubContext,
            LandsbankinnService landsbankinn,
            KrakenService kraken)
        {
            _logger = logger;
            _hubContext = hubContext;
            _kraken = kraken;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OhlcService is starting.");

            _timer = new Timer(UpdateOhlc, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OhlcService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            await Task.CompletedTask;
        }

        public async void UpdateOhlc(object state)
        {
            
            Ohlc = await _kraken.UpdateOhlc();

            if (Ohlc == null)
            {
                _logger.LogWarning($"Ohlc update failed: {DateTime.Now}");
                return;
            }

            List<ChartData> chartData = new List<ChartData>();

            foreach (var u in Ohlc.Ohlc)
            {
                chartData.Add(new ChartData()
                {
                    Time = Converters.ConvertFromUnixTimestamp(u.Time), // Convert from Unix timestamp to javascript date
                    Value = u.Open
                    //y = new decimal[4] { u.Open, u.High, u.Low, u.Close }
                });
            }

            ChartPair = new ChartPair()
            {
                Pair = Ohlc.Pair,
                Last = Ohlc.Last,
                ChartData = chartData
            };

            _logger.LogInformation($"Ohlc updated: {DateTime.Now}");
        }

    }


}