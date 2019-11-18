using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class BitcoinController : ControllerBase
    {
        private readonly ILogger<BitcoinController> _logger;
        private readonly BitcoinService _bitcoin;
        private readonly ApplicationDbContext _context;

        public BitcoinController(ILogger<BitcoinController> logger, ApplicationDbContext context, BitcoinService bitcoin)
        {
            _logger = logger;
            _context = context;
            _bitcoin = bitcoin;
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
        public async Task<ActionResult<string>> Withdrawal(BitcoinWithdrawal withdrawal)
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }

            var accountData = await _context.AccountData
                    .Include(x => x.MarketTransactions)
                    .FirstOrDefaultAsync(x => x.Id == id);
            if (accountData == null)
            {
                return NotFound("User not found in database");
            }

            if (accountData.WithdrawalAddress == null)
            {
                return NotFound("User has not set a withdrawal address");
            }

            var address = BitcoinAddress.Create(accountData.WithdrawalAddress, Network.Main);
            Money amount = Money.FromUnit(withdrawal.Amount, MoneyUnit.BTC);
            Money fees = Money.FromUnit(withdrawal.Fees, MoneyUnit.BTC);


            var result = await _bitcoin.SendBitcoin(id, address, amount, fees);
            if (result == null)
            {
                return Conflict("Failed to create/send transaction");
            }

            MarketTransaction mtx = new MarketTransaction
            {
                PersonalId = id,
                Date = DateTime.Now,
                Coins = -(withdrawal.Amount + withdrawal.Fees),
                TxId = result.ToString(),
                Status = TransactionStatus.Completed
            };

            accountData.MarketTransactions.Add(mtx);
            await _context.SaveChangesAsync();

            return result.ToString();
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetAddressBalance()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }
            
            BitcoinWitPubKeyAddress address = await _bitcoin.GetDepositAddress(id);
            Money result = await _bitcoin.GetAddressBalance(address);
            return result.ToDecimal(MoneyUnit.BTC);
        }
    }
}