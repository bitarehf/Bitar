using Bitar.Models;
using KrakenCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

            // Wait 15 seconds to allow StockService to get updates.
            _timer = new Timer(CheckPayments, null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1));

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
            List<Stock> stocks = _stock.Stocks;
            decimal BTCISK = decimal.Zero;

            if (_stock.MarketState == MarketState.Open)
            {
                foreach (var stock in stocks)
                {
                    if (stock.Symbol == Symbol.BTC)
                    {
                        BTCISK = stock.Price;
                    }
                }
            }
            else
            {
                _logger.LogCritical("Not checking transactions because market is closed.");
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                _logger.LogCritical("Checking transactions");
                foreach (var transactionA in _landsbankinn.transactions)
                {
                    if (transactionA.Amount < 500)
                    {
                        _logger.LogCritical("Transaction less than 500 ISK");
                        continue;
                    }

                    _logger.LogCritical("Searching for person with SSN: " + transactionA.SSN);
                    Person person = await context.Persons.FindAsync(transactionA.SSN);
                    if (person == null)
                    {
                        _logger.LogCritical($"{transactionA.SSN} not found");
                        continue;
                    }

                    if (await context.Transactions.FindAsync(transactionA.Id) != null)
                    {
                        _logger.LogCritical($"Transaction: {transactionA.Id} has already been paid");
                        continue;
                    }

                    _logger.LogCritical($"Transaction {transactionA.Id} has not been paid");
                    string address = person.BitcoinAddress;

                    // Convert ISK transaction amount  to Money.
                    Money amount = new Money((1 / BTCISK * transactionA.Amount) * 0.990m + 500m, MoneyUnit.BTC);

                    if (amount == null)
                    {
                        _logger.LogWarning("Failed to convert ISK to BTC");
                        break;
                    }

                    // Try to send bitcoin.
                    _logger.LogCritical($"Attempting to pay {amount.Satoshi} satoshis to {address}");
                    uint256 txId = await _bitcoin.MakePayment(address, amount);
                    if (txId != null)
                    {
                        transactionA.TxId = txId.ToString();
                        _logger.LogCritical($"({transactionA.SSN}) {address} paid {amount.Satoshi} satoshis. TxId: {transactionA.TxId}");
                    }
                    else
                    {
                        _logger.LogCritical($"Failed to pay ({transactionA.SSN}) {transactionA.Id} {address} {amount.Satoshi} satoshis.");
                    }

                    context.Transactions.Add(transactionA);
                    await context.SaveChangesAsync();
                }
            }
        }

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