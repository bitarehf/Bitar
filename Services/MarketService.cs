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
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private readonly StockService _stock;

        public MarketService(
            ILogger<MarketService> logger,
            IServiceScopeFactory scopeFactory,
            BitcoinService bitcoin,
            LandsbankinnService landsbankinn,
            KrakenService kraken,
            StockService stock)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _landsbankinn = landsbankinn;
            _kraken = kraken;
            _stock = stock;

            if (!string.IsNullOrWhiteSpace(_landsbankinn.sessionId))
            {
                _logger.LogInformation("Successfully logged in. SessionId: " + _landsbankinn.sessionId);
            }
            else
            {
                _logger.LogCritical("LandsbankinnClient failed to login in.");
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MarketService is starting.");

            await _stock.StartAsync(cancellationToken);

            _timer = new Timer(CheckPayments, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));

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
                    _logger.LogCritical("No transactions received today.");
                    return;
                }

                foreach (var transaction in transactions)
                {
                    _logger.LogDebug($@"
                        Date: {transaction.Date}
                        PersonalId: {transaction.PersonalId}
                        skyring_tilvisunar: {transaction.Reference}
                        tekka_sedilnr: {transaction.ShortReference}
                        tilvisun: {transaction.PaymentDetail}
                        upphaed: {transaction.Amount}");

                    AccountData accountData = await context.AccountData
                        .Include(x => x.Transactions)
                        .FirstOrDefaultAsync(x => x.Id == transaction.PersonalId);
                    if (accountData == null)
                    {
                        _logger.LogCritical($"Found a transaction from an unregistered user {transaction.PersonalId}");
                        continue;
                    }

                    if (accountData.Transactions == null)
                    {
                        _logger.LogCritical($"Found {transaction.PersonalId} first transaction");
                        accountData.Transactions.Add(transaction);
                        accountData.Balance += transaction.Amount;
                        await context.SaveChangesAsync();
                    }
                    else if (!accountData.Transactions.Any(x =>
                            x.PersonalId == transaction.PersonalId &&
                            x.Date == transaction.Date &&
                            x.Reference == transaction.Reference &&
                            x.ShortReference == transaction.ShortReference &&
                            x.PaymentDetail == transaction.PaymentDetail &&
                            x.Amount == transaction.Amount))
                    {
                        _logger.LogCritical($"Found a new transaction from {transaction.PersonalId}");
                        accountData.Transactions.Add(transaction);
                        accountData.Balance += transaction.Amount;
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

            // Converts to the transaction to the transaction model we are using.
            foreach (var transaction in tx)
            {
                if (transaction.faerslulykill != "01") continue; // Do not remove this line.

                transactions.Add(new Bitar.Models.Transaction
                {
                    Date = transaction.bokunardags,
                    PersonalId = transaction.kt_greidanda,
                    Reference = transaction.tekka_sedilnr,
                    ShortReference = transaction.tilvisun,
                    PaymentDetail = transaction.skyring_tilvisunar,
                    Amount = transaction.upphaed
                });
            }

            return transactions;
        }
    }
}