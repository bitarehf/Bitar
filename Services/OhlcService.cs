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
        public OhlcData OhlcData { get; set; }
        public OhlcChartData OhlcChartData { get; set; }

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
            
            OhlcData = await _kraken.UpdateOhlc();

            if (OhlcData == null)
            {
                _logger.LogWarning($"Ohlc update failed: {DateTime.Now}");
                return;
            }

            List<OhlcChart> ohlcChart = new List<OhlcChart>();

            foreach (var u in OhlcData.Ohlc)
            {
                ohlcChart.Add(new OhlcChart()
                {
                    Time = u.Time * 1000, // Convert from Unix timestamp to javascript date
                    Ohlc = u.Open
                    //y = new decimal[4] { u.Open, u.High, u.Low, u.Close }
                });
            }

            OhlcChartData = new OhlcChartData()
            {
                Pair = OhlcData.Pair,
                Last = OhlcData.Last,
                OhlcChart = ohlcChart
            };

            _logger.LogInformation($"Ohlc updated: {DateTime.Now}");
        }

    }


}