using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Models;
using Landsbankinn;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bitar.Services
{
    public class MarketService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;
        private readonly AssetService _asset;
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private readonly TickerService _ticker;
        private readonly OhlcService _ohlc;

        public MarketService(
            ILogger<MarketService> logger,
            IServiceScopeFactory scopeFactory,
            AssetService asset,
            LandsbankinnService landsbankinn,
            KrakenService kraken,
            TickerService ticker,
            OhlcService ohlc)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _asset = asset;
            _landsbankinn = landsbankinn;
            _kraken = kraken;
            _ticker = ticker;
            _ohlc = ohlc;

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MarketService is starting.");

            await _asset.StartAsync(cancellationToken);
            await _landsbankinn.StartAsync(cancellationToken);
            await _ticker.StartAsync(cancellationToken);
            await _ohlc.StartAsync(cancellationToken);

            _timer = new Timer(CheckPayments, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(2));

            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MarketService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void CheckPayments(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                _logger.LogInformation("Checking transactions.");

                var transactions = await FetchTransactions();

                if (transactions == null)
                {
                    _logger.LogWarning("No transactions received today.");
                    return;
                }

                foreach (var transaction in transactions)
                {
                    _logger.LogDebug(
                        $"Time: {transaction.Time}\n" +
                        $"AccountId: {transaction.AccountId}\n" +
                        $"skyring_tilvisunar: {transaction.Reference}\n" +
                        $"tekka_sedilnr: {transaction.ShortReference}\n" +
                        $"tilvisun: {transaction.PaymentDetail}\n" +
                        $"upphaed: {transaction.Amount}");

                    Account account = await context.Account
                        .Include(x => x.Transactions)
                        .FirstOrDefaultAsync(x => x.Id == transaction.AccountId);

                    if (account == null)
                    {
                        _logger.LogCritical($"Found a transaction from an unregistered user {transaction.AccountId}");
                        continue;
                    }

                    if (account.Transactions == null)
                    {
                        _logger.LogCritical($"Found {transaction.AccountId} first transaction");
                        account.Transactions.Add(transaction);
                        account.Balance += transaction.Amount;
                        await context.SaveChangesAsync();
                    }
                    else if (!account.Transactions.Any(x =>
                        x.AccountId == transaction.AccountId &&
                        x.Time == transaction.Time &&
                        x.Reference == transaction.Reference &&
                        x.ShortReference == transaction.ShortReference &&
                        x.PaymentDetail == transaction.PaymentDetail &&
                        x.Amount == transaction.Amount))
                    {
                        _logger.LogCritical($"Found a new transaction from {transaction.AccountId}");
                        account.Transactions.Add(transaction);
                        account.Balance += transaction.Amount;
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task<List<Bitar.Models.Transaction>> FetchTransactions()
        {
            List<LI_Fyrirspurn_reikningsyfirlit_svarFaersla> tx = await _landsbankinn.FetchTransactions();
            List<Bitar.Models.Transaction> transactions = new List<Bitar.Models.Transaction>();

            if (tx == null) return null;

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();



                // Converts to the transaction to the transaction model we are using.
                foreach (var transaction in tx)
                {
                    if (transaction.faerslulykill != "01") continue; // Do not remove this line.

                    Account account = await context.Account
                            .Include(x => x.Transactions)
                            .FirstOrDefaultAsync(x => x.Kennitala == transaction.kt_greidanda);

                    transactions.Add(new Bitar.Models.Transaction
                    {
                        AccountId = account.Id,
                        Time = transaction.bokunardags,
                        Kennitala = transaction.kt_greidanda,
                        Reference = transaction.tekka_sedilnr,
                        ShortReference = transaction.tilvisun,
                        PaymentDetail = transaction.skyring_tilvisunar,
                        Amount = transaction.upphaed
                    });
                }
            }
            return transactions;
        }
    }
}