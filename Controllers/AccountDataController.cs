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
        public async Task<ActionResult<Account>> GetAccountData()
        {
            string accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (accountIdString == null)
            {
                return BadRequest("AccountId missing from request");
            }

            int accountId = int.Parse(accountIdString);

            return await _context.Account
                .Include(x => x.MarketTransactions)
                .Include(x => x.Transactions)
                .Include(x => x.DilisenseRecords)
                .Include(x => x.KnowYourCustomerRecords)
                .FirstOrDefaultAsync(x => x.Id == accountId);
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

            var accountData = await _context.Account.FindAsync(id);
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

            var accountData = await _context.Account.FindAsync(id);
            accountData.BankAccountNumber = bankAccountNumber;

            await _context.SaveChangesAsync();

            return Ok(bankAccountNumber);
        }

        // POST: api.bitar.is/AccountData/Withdraw
        /// <summary>
        /// Withdraw money from account
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<decimal>> Withdraw([FromBody] decimal amount)
        {
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

            if (account.BankAccountNumber == null)
            {
                return NotFound("User has not set a bank account number");
            }

            if (amount <= 0 || amount > 10000000)
            {
                return BadRequest("Invalid amount");
            }

            if (account.Balance - amount >= 0)
            {
                try
                {
                    _logger.LogCritical($"{account.Id} is withdrawing {amount} ISK to {account.BankAccountNumber}");
                    account.Balance -= amount;
                    await _context.SaveChangesAsync();

                    string hq = account.BankAccountNumber.Substring(0, 4);
                    string hb = account.BankAccountNumber.Substring(4, 2);
                    string num = account.BankAccountNumber.Substring(6, 6);
                    bool result = _landsbankinn.Pay(hq, hb, num, account.Kennitala, amount);

                    MarketTransaction mtx = new MarketTransaction
                    {
                        AccountId = accountId,
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

                    account.MarketTransactions.Add(mtx);
                    await _context.SaveChangesAsync();

                    _logger.LogCritical($"Withdrawal successful");

                    return Ok(account.Balance);
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
            string accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (accountIdString == null)
            {
                return BadRequest("AccountId missing from request");
            }

            int accountId = int.Parse(accountIdString);

            BitcoinWitPubKeyAddress address = await _bitcoin.GetDepositAddress(accountId);
            return address.ToString();
        }

        // POST: api.bitar.is/AccountData/UpdateKnowYourCustomer
        /// <summary>
        /// Updates KYC.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> UpdateKnowYourCustomer(KnowYourCustomer knowYourCustomer)
        {
            _logger.LogInformation($"KYC updated called. {knowYourCustomer.Occupation} {knowYourCustomer.OriginOfFunds} {knowYourCustomer.OwnerOfFunds}");
            string accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (accountIdString == null)
            {
                return BadRequest("AccountId missing from request");
            }

            int accountId = int.Parse(accountIdString);

            var account = await _context.Account
                .Include(x => x.KnowYourCustomerRecords)
                .FirstOrDefaultAsync(x => x.Id == accountId);

            if (account == null)
            {
                return NotFound("User not found in database");
            }


            knowYourCustomer.AccountId = accountId;
            knowYourCustomer.Time = DateTime.Now;

            account.KnowYourCustomerRecords.Add(knowYourCustomer);

            await _context.SaveChangesAsync();
            _logger.LogInformation($"KYC updated for user: {accountId}");

            return Ok();
        }
    }
}