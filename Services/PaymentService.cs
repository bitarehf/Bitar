using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bitar.Models;
using KrakenCore;
using Landsbankinn;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Bitar.Services
{
    public class PaymentService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;
        private readonly BitcoinService _bitcoin;
        private readonly LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;
        private readonly StockService _stock;

        public PaymentService(
            ILogger<PaymentService> logger,
            IServiceScopeFactory scopeFactory,
            BitcoinService bitcoin,
            LandsbankinnService landsbankinn,
            KrakenService kraken,
            StockService stock)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _bitcoin = bitcoin;
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
            _logger.LogInformation("PaymentService is starting.");

            await _stock.StartAsync(cancellationToken);

            _timer = new Timer(CheckPayments, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));

            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PaymentService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void CheckPayments(object state)
        {
            using(var scope = _scopeFactory.CreateScope())
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
                    _logger.LogDebug($"Date: {transaction.Date}");
                    _logger.LogDebug($"PersonalId: {transaction.PersonalId}");
                    _logger.LogDebug($"skyring_tilvisunar: {transaction.Reference}");
                    _logger.LogDebug($"tekka_sedilnr: {transaction.ShortReference}");
                    _logger.LogDebug($"tilvisun: {transaction.PaymentDetail}");
                    _logger.LogDebug($"upphaed: {transaction.Amount}");
                    _logger.LogDebug("===========================");

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
                    else if (!accountData.Transactions.Contains(transaction))
                    {
                        _logger.LogCritical($"Found a new transaction from {transaction.PersonalId}");
                        accountData.Transactions.Add(transaction);
                        accountData.Balance += transaction.Amount;
                        await context.SaveChangesAsync();
                    }
                }

                // foreach (var transactionA in _landsbankinn.transactions)
                // {

                //     if (transactionA.Amount > 20000)
                //     {
                //         _logger.LogCritical("Transaction is more than 20000 ISK");
                //         continue;
                //     }

                //     _logger.LogCritical("Searching for person with SSN: " + transactionA.SSN);
                //     AccountData accountData = await context.AccountData.FindAsync(transactionA.SSN);
                //     if (accountData == null)
                //     {
                //         _logger.LogCritical($"{transactionA.SSN} not found");
                //         continue;
                //     }

                //     context.Transactions.Add(transactionA);
                //     await context.SaveChangesAsync();
                // }
            }
        }

        private async Task<List<Bitar.Models.Transaction>> FetchTransactions()
        {
            List<LI_Fyrirspurn_reikningsyfirlit_svarFaersla> tx = await _landsbankinn.FetchTransactions();
            List<Bitar.Models.Transaction> transactions = new List<Bitar.Models.Transaction>();

            if (tx == null)return null;

            // Converts to the transaction to the transaction model we are using.
            foreach (var transaction in tx)
            {
                if (transaction.faerslulykill != "01")continue; // Do not remove this line.

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

        // private async void CheckPaymentsOld(object state)
        // {
        //     List<Stock> stocks = _stock.Stocks;
        //     decimal BTCISK = decimal.Zero;

        //     if (_stock.MarketState == MarketState.Open)
        //     {
        //         foreach (var stock in stocks)
        //         {
        //             if (stock.Symbol == Symbol.BTC)
        //             {
        //                 BTCISK = stock.Price;
        //             }
        //         }
        //     }
        //     else
        //     {
        //         _logger.LogCritical("Not checking transactions because market is closed.");
        //         return;
        //     }

        //     using(var scope = _scopeFactory.CreateScope())
        //     {
        //         var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //         _logger.LogInformation("Checking transactions");
        //         foreach (var transactionA in _landsbankinn.transactions)
        //         {
        //             if (transactionA.Amount < 1000)
        //             {
        //                 _logger.LogCritical("Transaction less than 1000 ISK");
        //                 continue;
        //             }

        //             if (transactionA.Amount > 20000)
        //             {
        //                 _logger.LogCritical("Transaction is more than 20000 ISK");
        //                 continue;
        //             }

        //             _logger.LogCritical("Searching for person with SSN: " + transactionA.SSN);
        //             AccountData accountData = await context.AccountData.FindAsync(transactionA.SSN);
        //             if (accountData == null)
        //             {
        //                 _logger.LogCritical($"{transactionA.SSN} not found");
        //                 continue;
        //             }

        //             if (await context.Transactions.FindAsync(transactionA.Id) != null)
        //             {
        //                 _logger.LogCritical($"Transaction: {transactionA.Id} has already been paid");
        //                 continue;
        //             }

        //             _logger.LogCritical($"Transaction {transactionA.Id} has not been paid");
        //             var address = _bitcoin.GetDepositAddress(transactionA.SSN);

        //             // Convert ISK transaction amount  to Money.
        //             Money amount = new Money((1 / BTCISK * transactionA.Amount) * 0.995m, MoneyUnit.BTC);

        //             if (amount == null)
        //             {
        //                 _logger.LogWarning("Failed to convert ISK to BTC");
        //                 break;
        //             }

        //             // Try to send bitcoin.
        //             _logger.LogCritical($"Attempting to pay {amount.Satoshi} satoshis to {address}");
        //             uint256 txId = await _bitcoin.MakePayment(address.ToString(), amount);
        //             if (txId != null)
        //             {
        //                 transactionA.TxId = txId.ToString();
        //                 _logger.LogCritical($"({transactionA.SSN}) {address} paid {amount.Satoshi} satoshis. TxId: {transactionA.TxId}");
        //             }
        //             else
        //             {
        //                 _logger.LogCritical($"Failed to pay ({transactionA.SSN}) {transactionA.Id} {address} {amount.Satoshi} satoshis.");
        //             }

        //             context.Transactions.Add(transactionA);
        //             await context.SaveChangesAsync();
        //         }
        //     }
        // }

        // private void CheckPayments(object state)
        // {
        //     _logger.LogWarning("Checking Payments.");
        //     var account = _client.AccountStatement("0133", "26", "014528");
        //     if (account != null)
        //     {
        //         _logger.LogWarning("Balance: " + account.stada_reiknings + " " + account.mynt);
        //         List<Transaction> transactions = new List<Transaction>();
        //         foreach (var item in account.faerslur)
        //         {
        //             _logger.LogWarning("======== Transaction ========");
        //             _logger.LogWarning("SSN: " + item.kt_greidanda);
        //             _logger.LogWarning("Amount:" + item.upphaed);
        //             _logger.LogWarning("Date: " + item.bokunardags);

        //             var transaction = new Transaction()
        //             {
        //                 SSN = item.kt_greidanda,
        //                 Amount = item.upphaed,
        //                 Date = item.bokunardags
        //             };
        //             transactions.Add(transaction);
        //         }

        //         // Remove paid transactions from the list.
        //         transactions = transactions.Except(_paidTransactions).ToList();

        //         // Check if any of the transactions are linked to a bitcoin address
        //         // and pay them if they are.
        //         foreach (var transaction in transactions)
        //         {
        //             var link = _context.AddressLinks.FirstOrDefault(c => c.SSN == transaction.SSN);
        //             if(link != null)
        //             {
        //                 _logger.LogCritical($"Sending bitcoin to {link.SSN}, {link.BitcoinAddress}");
        //                 //_bitcoin.MakePayment(s.BitcoinAddress, "1337");
        //                 _paidTransactions.Add(transaction);
        //                 _context.Transactions.Add(transaction);
        //                 _context.SaveChanges();
        //             }
        //         }
        //     }
        // }
    }
}