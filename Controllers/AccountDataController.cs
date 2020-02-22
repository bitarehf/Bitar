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
    public class AccountDataController : ControllerBase
    {
        private readonly ILogger<AccountDataController> _logger;
        private readonly BitcoinService _bitcoin;
        private readonly LandsbankinnService _landsbankinn;
        private readonly ApplicationDbContext _context;

        public AccountDataController(
            ILogger<AccountDataController> logger,
            ApplicationDbContext context,
            LandsbankinnService landsbankinn,
            BitcoinService bitcoin)
        {
            _logger = logger;
            _landsbankinn = landsbankinn;
            _bitcoin = bitcoin;
            _context = context;
        }

        // GET: api.bitar.is/AccountData/GetAccountData
        [HttpGet]
        public async Task<ActionResult<AccountData>> GetAccountData()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }

            return await _context.AccountData
                .Include(x => x.MarketTransactions)
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // POST: api.bitar.is/AccountData/UpdateWithdrawalAddress
        /// <summary>
        /// Updates Account WithdrawalAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<string>> UpdateWithdrawalAddress([FromBody] string bitcoinAddress)
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }

            var accountData = await _context.AccountData.FindAsync(id);
            accountData.WithdrawalAddress = bitcoinAddress;

            await _context.SaveChangesAsync();

            return Ok(bitcoinAddress);
        }

        // POST: api.bitar.is/AccountData/UpdateBankAccountNumber
        /// <summary>
        /// Updates Account WithdrawalAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<string>> UpdateBankAccountNumber([FromBody] string bankAccountNumber)
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }

            var accountData = await _context.AccountData.FindAsync(id);
            accountData.BankAccountNumber = bankAccountNumber;

            await _context.SaveChangesAsync();

            return Ok(bankAccountNumber);
        }

        // POST: api.bitar.is/AccountData/Withdraw
        /// <summary>
        /// Updates Account WithdrawalAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<decimal>> Withdraw([FromBody] decimal amount)
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

            if (accountData.BankAccountNumber == null)
            {
                return NotFound("User has not set a bank account number");
            }

            if (amount <= 0 || amount > 10000000)
            {
                return BadRequest("Invalid amount");
            }

            if (accountData.Balance - amount >= 0)
            {
                try
                {
                    _logger.LogCritical($"{accountData.Id} is withdrawing {amount} ISK to {accountData.BankAccountNumber}");
                    accountData.Balance -= amount;
                    await _context.SaveChangesAsync();

                    string hq = accountData.BankAccountNumber.Substring(0, 3);
                    string hb = accountData.BankAccountNumber.Substring(4, 5);
                    string nr = accountData.BankAccountNumber.Substring(6, 11);
                    bool result = _landsbankinn.Pay(hq, hb, nr, accountData.Id, amount);

                    MarketTransaction mtx = new MarketTransaction
                    {
                        PersonalId = id,
                        Time = DateTime.Now,
                        Amount = -amount,
                        Type = TransactionType.Withdrawal,
                        Status = TransactionStatus.Completed
                    };

                    if (result == false)
                    {
                        mtx.Status = TransactionStatus.Failed;
                        return Conflict("Failed to create/send transaction");
                    }

                    accountData.MarketTransactions.Add(mtx);
                    await _context.SaveChangesAsync();

                    _logger.LogCritical($"Withdrawal successful");

                    return Ok(accountData.Balance);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                    return NotFound();
                }
            }

            return NotFound("tf did you just try to do?");
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetDepositAddress()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }

            BitcoinWitPubKeyAddress address = await _bitcoin.GetDepositAddress(id);
            return address.ToString();
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