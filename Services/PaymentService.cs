using Bitar.Models;
using KrakenCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using System;
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
        private LandsbankinnService _landsbankinn;
        private readonly KrakenService _kraken;

        public PaymentService(ILogger<PaymentService> logger,
            IServiceScopeFactory scopeFactory,
            BitcoinService bitcoin,
            LandsbankinnService landsbankinn,
            KrakenService kraken)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _bitcoin = bitcoin;
            _landsbankinn = landsbankinn;
            _kraken = kraken;

            if (!string.IsNullOrWhiteSpace(_landsbankinn.sessionId))
            {
                _logger.LogInformation("Successfully logged in. SessionId: " + _landsbankinn.sessionId);
            }
            else
            {
                _logger.LogCritical("LandsbankinnClient failed to login in.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Payment Service is starting.");
            _timer = new Timer(CheckPayments, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Payment Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void CheckPayments(object state)
        {
            decimal ISKEUR = 135m;
            decimal BTCEUR = 5000m;
            // if (ISKEUR == decimal.Zero || BTCEUR == decimal.Zero)
            // {
            //     _logger.LogCritical("Failed to get exchange rate");
            //     return;
            // }
            // _logger.LogCritical($"Rates: ISKEUR {ISKEUR} BTCEUR {BTCEUR}");
            //
            // Money m = new Money(5000 / ISKEUR / BTCEUR, MoneyUnit.BTC);
            // _logger.LogCritical("Satoshi amount: " + m.Satoshi.ToString());

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                _logger.LogCritical("Checking transactions");
                foreach (var transactionA in _landsbankinn.transactions)
                {
                    if (transactionA.Amount < 500)
                    {
                        // Todo: Refund.
                        _logger.LogCritical("Transaction less than 500 ISK");
                        continue;
                    }

                    _logger.LogCritical("Searching for person with SSN: " + transactionA.SSN);
                    Person person = await context.Persons.FindAsync(transactionA.SSN);
                    if (person == null)
                    {
                        // Todo: Refund.
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

                    // Convert transaction amount (ISK) to Money.
                    // Bitcoins = ISK / ISKEUR / BTCEUR.
                    Money amount = new Money(transactionA.Amount / ISKEUR / BTCEUR, MoneyUnit.BTC);

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