using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bitar.Helpers;
using Bitar.Models;
using Bitar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Bitar.Controllers
{
    [Route("[controller]/[action]")]
    [Authorize]
    [ApiController]
    public class BlockchainController : ControllerBase
    {
        private readonly ILogger<BlockchainController> _logger;
        private readonly BitcoinService _bitcoin;
        private readonly BlockchainService _blockchain;
        private readonly ApplicationDbContext _context;
        private readonly TickerService _ticker;

        public BlockchainController(
            ILogger<BlockchainController> logger,
            ApplicationDbContext context,
            BitcoinService bitcoin,
            BlockchainService blockchain,
            TickerService ticker)
        {
            _logger = logger;
            _context = context;
            _bitcoin = bitcoin;
            _blockchain = blockchain;
            _ticker = ticker;
        }

        // Post: api/AccountData
        /// <summary>
        /// Estimate the optimal amount of fees for the specified amount of blocks.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<FeeRate>> EstimateFees([FromBody] int blocks)
        {
            return await _bitcoin.EstimateSmartFee(blocks);
        }

        [HttpPost]
        public async Task<ActionResult<string>> Withdraw(BitcoinWithdrawal withdrawal)
        {

            if (MathDecimals.GetDecimals(withdrawal.Amount) > 8)
            {
                return BadRequest("withdrawal amount cannot have more than 8 decimals");
            }

            if (MathDecimals.GetDecimals(withdrawal.Fees) > 8)
            {
                return BadRequest("withdrawal fees cannot have more than 8 decimals");
            }

            string accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (accountIdString == null)
            {
                return BadRequest("AccountId missing from request");
            }

            int accountId = int.Parse(accountIdString);

            var account = await _context.Account
                .Include(x => x.MarketTransactions)
                .FirstOrDefaultAsync(x => x.Id == accountId);

            if (account == null)
            {
                return NotFound("User not found in database");
            }

            if (account.WithdrawalAddress == null)
            {
                return NotFound("User has not set a withdrawal address");
            }

            var address = BitcoinAddress.Create(account.WithdrawalAddress, Network.Main);
            Money amount = Money.FromUnit(withdrawal.Amount, MoneyUnit.BTC);
            Money fees = Money.FromUnit(withdrawal.Fees, MoneyUnit.BTC);

            var result = await _bitcoin.SendBitcoin(accountId, address, amount, fees);
            if (result == null)
            {
                return Conflict("Failed to create/send transaction");
            }

            decimal rate = Decimal.Zero;

            if (_ticker.MarketState == MarketState.Open)
            {
                rate = _ticker.Tickers["btcisk"].Ask;
            }

            MarketTransaction mtx = new MarketTransaction
            {
                AccountId = accountId,
                Time = DateTime.Now,
                Coins = -(withdrawal.Amount + withdrawal.Fees),
                TxId = result.ToString(),
                Type = TransactionType.Withdrawal,
                Rate = rate,
                Status = TransactionStatus.Completed
            };

            account.MarketTransactions.Add(mtx);
            await _context.SaveChangesAsync();

            return result.ToString();
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetAddressBalance()
        {
            string accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (accountIdString == null)
            {
                return BadRequest("AccountId missing from request");
            }

            int accountId = int.Parse(accountIdString);

            BitcoinWitPubKeyAddress address = await _bitcoin.GetDepositAddress(accountId);
            Money result = await _blockchain.GetAddressBalance(address);
            return result.ToDecimal(MoneyUnit.BTC);
        }
    }
}