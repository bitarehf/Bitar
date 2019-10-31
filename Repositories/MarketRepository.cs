using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Bitar.Repositories
{
    public class MarketRepository
    {
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly BitcoinService _bitcoin;
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
        public async Task<uint256> Buy(string id, decimal isk)
        {
            var accountData = await _context.AccountData.FindAsync(id);
            if (accountData == null)
            {
                _logger.LogCritical($"Order cancelled because no account with id: {id} was found");
                return null;
            }

            decimal btcisk = Decimal.Zero;

            using (var scope = _serviceProvider.CreateScope())
            {
                var _stockService = scope.ServiceProvider.GetRequiredService<StockService>();

                if (_stockService.MarketState == MarketState.Open)
                {
                    foreach (var stock in _stockService.Stocks)
                    {
                        if (stock.Symbol == Symbol.BTC)
                        {
                            btcisk = stock.Price;
                        }
                    }
                }
                else
                {
                    _logger.LogCritical("Order cancelled because market is closed.");
                    return null;
                }
            }

            if (btcisk == Decimal.Zero)
            {
                _logger.LogCritical("Order cancelled because btcisk value was zero, this should never happen.");
                return null;
            }

            Money amount = Money.Coins(Math.Round(isk / btcisk, 8, MidpointRounding.ToZero));
            if (accountData.Balance >= isk)
            {
                _logger.LogWarning($"{id} bought {amount} Bitcoin for {isk} ISK at the rate of {btcisk}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var _bitcoin = scope.ServiceProvider.GetRequiredService<BitcoinService>();
                    return await _bitcoin.MakePayment(id, amount);
                }
            }
            else
            {
                _logger.LogCritical(
                    "Order cancelled.\n" +
                    $"{id} does not have sufficient balance for the order.\n" +
                    $"Order => {amount} BTC for {isk} ISK.\n" +
                    $"Current balance: {accountData.Balance} ISK.");
                return null;
            }

            return null;
        }
    }
}