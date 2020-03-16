using System;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Hubs;
using KrakenCore;
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
        private readonly KrakenService _kraken;
        private Timer _timer;
        public TimestampedDictionary<string, Ohlc[]> Ohlc { get; set; }

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

            _timer = new Timer(UpdateOhlc, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

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
            Ohlc = await _kraken.GetOhlcData();

            _logger.LogInformation($"Ohlc Updated: {DateTime.Now}");
        }

    }


}