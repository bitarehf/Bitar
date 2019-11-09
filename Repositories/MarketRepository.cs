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

                decimal rate = Decimal.Zero;

                using (var scope = _serviceProvider.CreateScope())
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
                        return null;
                    }
                }

                if (rate == Decimal.Zero)
                {
                    _logger.LogCritical("Order cancelled because rate value was zero, this should never happen.");
                    return null;
                }

                Money coins = Money.Coins(Math.Round(isk / rate, 8, MidpointRounding.ToZero));
                _logger.LogDebug($"Id: {id} Coins: {coins} ISK: {isk} Rate: {rate} Account Balance: {accountData.Balance}");
                if (accountData.Balance >= isk)
                {
                    _logger.LogDebug($"{id} has sufficient balance for the order");

                    var transaction = new MarketTransaction
                    {
                        PersonalId = id,
                        Date = DateTime.Now,
                        Rate = rate,
                        Coins = coins.ToDecimal(MoneyUnit.BTC),
                        Amount = isk
                    };

                    accountData.Balance -= isk;
                    accountData.MarketTransactions.Add(transaction);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning($"{id} bought {coins} BTC for {isk} ISK with a rate of: {rate}");
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var _bitcoin = scope.ServiceProvider.GetRequiredService<BitcoinService>();
                        return await _bitcoin.MakePayment(id, coins);
                    }
                }
                else
                {
                    _logger.LogCritical(
                        "Order cancelled.\n" +
                        $"{id} does not have sufficient balance for the order.\n" +
                        $"Order => {coins} BTC for {isk} ISK.\n" +
                        $"Current balance: {accountData.Balance} ISK.");
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