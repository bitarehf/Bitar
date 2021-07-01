using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Bitar.Repositories
{
    public class MarketRepository
    {
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        public MarketRepository(
            ILogger<MarketRepository> logger,
            ApplicationDbContext context,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _context = context;
            _serviceProvider = serviceProvider;
        }
        public async Task<uint256> Order(int accountId, decimal isk)
        {
            MarketTransaction mtx = new MarketTransaction
            {
                AccountId = accountId,
                Time = DateTime.Now,
                Amount = -isk,
                Type = TransactionType.Buy
            };

            try
            {
                var account = await _context.Account
                    .Include(x => x.MarketTransactions)
                    .FirstOrDefaultAsync(x => x.Id == accountId);

                if (account == null)
                {
                    _logger.LogCritical($"Order cancelled because no account with id: {accountId} was found");
                    return null;
                }

                mtx.Fee = -Math.Round(isk * (account.Fee / 100));

                decimal rate = Decimal.Zero;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var _tickerService = scope.ServiceProvider.GetRequiredService<TickerService>();

                    if (_tickerService.MarketState == MarketState.Open)
                    {
                        rate = _tickerService.Tickers["btcisk"].Ask;
                    }
                    else
                    {
                        _logger.LogCritical("Order cancelled because market is closed.");
                        mtx.Status = TransactionStatus.Rejected;
                        account.MarketTransactions.Add(mtx);
                        await _context.SaveChangesAsync();
                        return null;
                    }
                }

                mtx.Rate = rate;

                if (rate == Decimal.Zero)
                {
                    _logger.LogCritical("Order cancelled because rate value was zero, this should never happen.");
                    mtx.Status = TransactionStatus.Rejected;
                    account.MarketTransactions.Add(mtx);
                    await _context.SaveChangesAsync();
                    return null;
                }

                Money coins = Money.Coins(
                    Math.Round(isk / rate * (1 - account.Fee / 100), 8, MidpointRounding.ToZero));
                mtx.Coins = coins.ToDecimal(MoneyUnit.BTC);
                _logger.LogDebug($"Id: {accountId} Coins: {coins} ISK: {isk} Rate: {rate} Account Balance: {account.Balance}");

                if (account.Balance >= isk)
                {
                    mtx.Balance = account.Balance - isk;
                    _logger.LogDebug($"{accountId} has sufficient balance for the order");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var _bitcoin = scope.ServiceProvider.GetRequiredService<BitcoinService>();
                        if (await _bitcoin.BitarCanMakePayment(accountId, coins))
                        {
                            account.Balance -= isk;
                            await _context.SaveChangesAsync();

                            ExtKey key = _bitcoin._masterKey.Derive(new KeyPath($"m/84'/0'/{account.Derivation}'/0/0"));
                            var receiver = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                            int bitarAccountId = 0;
                            var result = await _bitcoin.SendBitcoin(0, receiver, coins, 36);

                            if (result != null)
                            {
                                _logger.LogDebug($"Bitcoin transaction result: {result.ToString()}");
                                mtx.TxId = result.ToString();
                                mtx.Status = TransactionStatus.Completed;
                            }
                            else
                            {
                                _logger.LogCritical($"Bitcoin transaction failed");
                                // The bitcoin transaction failed.
                                // Should we refund the user right away?
                                // accountData.Balance += isk;
                                mtx.Status = TransactionStatus.Failed;
                            }

                            _logger.LogWarning(
                                "Market Transaction.\n" +
                                $"Id: {mtx.Id}\n" +
                                $"Date: {mtx.Time}\n" +
                                $"Rate: {mtx.Rate}\n" +
                                $"Coins: {mtx.Coins}\n" +
                                $"Fee: {mtx.Fee}\n" +
                                $"Amount: {mtx.Amount}\n" +
                                $"Type: {mtx.Type}\n" +
                                $"Status: {mtx.Status}");

                            account.MarketTransactions.Add(mtx);
                            await _context.SaveChangesAsync();
                            return result;
                        }

                    }
                }
                else
                {
                    _logger.LogCritical(
                        "Order cancelled.\n" +
                        $"{accountId} does not have sufficient balance for the order.\n" +
                        $"Order => {coins} BTC for {isk} ISK.\n" +
                        $"Current balance: {account.Balance} ISK.");

                    mtx.Status = TransactionStatus.Rejected;
                    account.MarketTransactions.Add(mtx);
                    await _context.SaveChangesAsync();
                    return null;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogError($"DbUpdateConcurrencyException: {accountId} {isk} ISK order cancelled.");
            }

            return null;
        }
    }
}