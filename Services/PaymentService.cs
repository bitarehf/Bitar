using Bitar.Models;
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
        private readonly BitcoinService _bitcoin;
        private Timer _timer;
        private LandsbankinnService _landsbankinn;

        public PaymentService(ILogger<PaymentService> logger,
            IServiceScopeFactory scopeFactory,
            BitcoinService bitcoin,
            LandsbankinnService landsbankinn)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _bitcoin = bitcoin;
            _landsbankinn = landsbankinn;

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
            _logger.LogWarning("Payment Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void CheckPayments(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BitarContext>();
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
                    var person = await context.Persons.FindAsync(transactionA.SSN);
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

                    _logger.LogCritical($"Transaction: {transactionA.Id} not found");
                    var address = person.BitcoinAddress;
                    // Todo: Validate bitcoin address.
                    _logger.LogCritical($"Address: {address}");

                    // Try to send bitcoin.
                    Money amount = ConvertISKToMoney(transactionA.Amount);
                    _logger.LogCritical($"Attempting to pay {amount.Satoshi} satoshis to {address}");
                    var payment = _bitcoin.MakePayment(address, amount.Satoshi);

                    
                    if (payment.Result != null)
                    {
                        transactionA.TxId = payment.Result.ToString();
                        _logger.LogCritical($"({transactionA.SSN}) {address} paid {amount.Satoshi} satoshis. TxId: {transactionA.TxId}");
                    }

                    // Add transaction to database.
                    context.Transactions.Add(transactionA);
                    await context.SaveChangesAsync();

                }
            }
        }

        /// <summary>
        /// Converts <paramref name="amount"/> to <see cref="Money"/>
        /// </summary>
        /// <param name="amount">Amount in ISK</param>
        private Money ConvertISKToMoney(decimal amount)
        {
            // Bitcoins = ISK / ISKEUR-ExchangeRate / BTCEUR-ExchangeRate.
            amount /= 135m; // TODO: Get actual ISKEUR-ExchangeRate.
            amount /= 5400m; // TODO: Get actual BTCEUR-ExchangeRate.
            return new Money(amount, MoneyUnit.BTC);
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