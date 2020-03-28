using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Helpers;
using Bitar.Hubs;
using Bitar.Models;
using KrakenCore.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bitar.Services
{
    public class OhlcService : IHostedService
    {
        private readonly ILogger<OhlcService> _logger;
        private readonly IHubContext<StockHub> _hubContext;
        private readonly AssetService _asset;
        private readonly KrakenService _kraken;
        private Timer _timer;
        public OhlcPair OhlcPair { get; set; }
        public ChartPair ChartPair { get; set; }

        public OhlcService(
            ILogger<OhlcService> logger,
            IHubContext<StockHub> hubContext,
            LandsbankinnService landsbankinn,
            AssetService asset,
            KrakenService kraken)
        {
            _logger = logger;
            _hubContext = hubContext;
            _asset = asset;
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

            OhlcPair = await _kraken.UpdateOhlc();

            if (OhlcPair == null)
            {
                _logger.LogWarning($"Ohlc update failed: {DateTime.Now}");
                return;
            }

            List<ChartData> chartData = new List<ChartData>();
            List<Asset> a = _asset.Assets["EURISK"];

            foreach (var ohlc in OhlcPair.Ohlc)
            {
                var t = Converters.UnixTimestampToDateTime(ohlc.Time);
                
                // Find the Asset with the closest DateTime to the ohlc object.
                var asset = a.OrderBy(m => Math.Abs((t - m.Time).TotalMilliseconds)).First();

                chartData.Add(new ChartData()
                {
                    Time = t,
                    Value = decimal.Ceiling(asset.Ask * ohlc.Open * 1.02m + 100m)
                    //y = new decimal[4] { u.Open, u.High, u.Low, u.Close }
                });
            }

            ChartPair = new ChartPair()
            {
                Pair = OhlcPair.Pair,
                Last = OhlcPair.Last,
                ChartData = chartData
            };

            _logger.LogInformation($"Ohlc updated: {DateTime.Now}");
        }

        /// <summary>
        /// Find closest date in ohlc.
        /// </summary>
        static Asset Closest(List<Asset> asset, DateTime time)
        {
            // Return closest.
            return asset.OrderBy(m => Math.Abs((time - m.Time).TotalMilliseconds)).First();
        }

    }


}