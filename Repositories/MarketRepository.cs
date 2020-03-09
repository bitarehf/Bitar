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
        public async Task<uint256> Order(string id, decimal isk)
        {
            MarketTransaction mtx = new MarketTransaction
            {
                PersonalId = id,
                Time = DateTime.Now,
                Amount = -isk,
                Type = TransactionType.Buy
            };

            try
            {
                var accountData = await _context.AccountData
                    .Include(x => x.MarketTransactions)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (accountData == null)
                {
                    _logger.LogCritical($"Order cancelled because no account with id: {id} was found");
                    return null;
                }

                mtx.Fee = -(accountData.Fee / 100 * isk);

                decimal rate = Decimal.Zero;

                using(var scope = _serviceProvider.CreateScope())
                {
                    var _stockService = scope.ServiceProvider.GetRequiredService<StockService>();

                    if (_stockService.MarketState == MarketState.Open)
                    {
                        foreach (var stock in _stockService.Stocks)
                        {
                            if (stock.Symbol == Symbol.BTC)
                            {
                                rate = stock.Price;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogCritical("Order cancelled because market is closed.");
                        mtx.Status = TransactionStatus.Rejected;
                        accountData.MarketTransactions.Add(mtx);
                        await _context.SaveChangesAsync();
                        return null;
                    }
                }

                mtx.Rate = rate;

                if (rate == Decimal.Zero)
                {
                    _logger.LogCritical("Order cancelled because rate value was zero, this should never happen.");
                    mtx.Status = TransactionStatus.Rejected;
                    accountData.MarketTransactions.Add(mtx);
                    await _context.SaveChangesAsync();
                    return null;
                }

                Money coins = Money.Coins(Math.Round((isk + mtx.Fee) / rate, 8, MidpointRounding.ToZero));
                mtx.Coins = coins.ToDecimal(MoneyUnit.BTC);
                _logger.LogDebug($"Id: {id} Coins: {coins} ISK: {isk} Rate: {rate} Account Balance: {accountData.Balance}");

                if (accountData.Balance >= isk)
                {
                    mtx.Balance = accountData.Balance - isk;
                    _logger.LogDebug($"{id} has sufficient balance for the order");

                    using(var scope = _serviceProvider.CreateScope())
                    {
                        var _bitcoin = scope.ServiceProvider.GetRequiredService<BitcoinService>();
                        if (await _bitcoin.BitarCanMakePayment(id, coins))
                        {
                            accountData.Balance -= isk;
                            await _context.SaveChangesAsync();

                            ExtKey key = _bitcoin._masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                            var receiver = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                            var result = await _bitcoin.SendBitcoin("4708180420", receiver, coins, 36);

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

                            accountData.MarketTransactions.Add(mtx);
                            await _context.SaveChangesAsync();
                            return result;
                        }

                    }
                }
                else
                {
                    _logger.LogCritical(
                        "Order cancelled.\n" +
                        $"{id} does not have sufficient balance for the order.\n" +
                        $"Order => {coins} BTC for {isk} ISK.\n" +
                        $"Current balance: {accountData.Balance} ISK.");

                    mtx.Status = TransactionStatus.Rejected;
                    accountData.MarketTransactions.Add(mtx);
                    await _context.SaveChangesAsync();
                    return null;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogError($"DbUpdateConcurrencyException: {id} {isk} ISK order cancelled.");
            }

            return null;
        }
    }
}