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
        private readonly ApplicationDbContext _context;

        public AccountDataController(ILogger<AccountDataController> logger, ApplicationDbContext context, BitcoinService bitcoin)
        {
            _logger = logger;
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


        // POST: api/AccountData
        /// <summary>
        /// Updates Account WithdrawalAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BitcoinAddress>> UpdateWithdrawalAddress(BitcoinAddress bitcoinAddress)
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found");
            }

            var accountData = await _context.AccountData.FindAsync(id);
            accountData.WithdrawalAddress = bitcoinAddress.ToString();

            await _context.SaveChangesAsync();

            return BitcoinAddress.Create(accountData.WithdrawalAddress, Network.Main);
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