
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bitar.Services
{

    public class AssetService : IHostedService
    {
        private readonly ILogger<AssetService> _logger;
        private readonly ArionService _arion;
        private Timer _timer;
        public Dictionary<string, List<Asset>> Assets = new Dictionary<string, List<Asset>>();

        public AssetService(ILogger<AssetService> logger, ArionService arion)
        {
            _logger = logger;
            _arion = arion;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AssetService is starting.");

            _timer = new Timer(UpdateRates, null, TimeSpan.Zero, TimeSpan.FromHours(1));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AssetService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            await Task.CompletedTask;
        }

        public async void UpdateRates(object state)
        {
            Assets["eurisk"] = await _arion.GetArionRates("eurisk", new DateTime(2015, 1, 1).ToString("yyyy'-'MM'-'dd"), DateTime.Now.ToString("yyyy'-'MM'-'dd"));
            // Assets["USDISK"] = await _arion.GetArionRates("USDISK", new DateTime(2015, 1, 1).ToString("yyyy'-'MM'-'dd"), DateTime.Now.ToString("yyyy'-'MM'-'dd"));
        }
    }
}