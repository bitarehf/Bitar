using Bitar.Models;
using Bitar.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bitar.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AccountDataController : ControllerBase
    {
        private readonly BitcoinService _bitcoin;
        private readonly ApplicationDbContext _context;

        public AccountDataController(ApplicationDbContext context, BitcoinService bitcoin)
        {
            _bitcoin = bitcoin;
            _context = context;
        }

        // GET: api/AccountData
        [HttpGet]
        public List<AccountData> AllAccountData()
        {
            return _context.AccountData.ToList();
        }

        // GET: api/AccountData/4708180420
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountData>> AccountData(string id)
        {
            return await _context.AccountData.FindAsync(id);
        }

        // POST: api/AccountData
        /// <summary>
        /// Updates Account WithdrawalAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BitcoinAddress>> UpdateWithdrawalAddress(string id, BitcoinAddress bitcoinAddress)
        {
            var accountData = await _context.AccountData.FindAsync(id);
            accountData.WithdrawalAddress = bitcoinAddress.ToString();

            await _context.SaveChangesAsync();

            return BitcoinAddress.Create(accountData.WithdrawalAddress, Network.Main);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetDepositAddress(string id)
        {
            var result = await _bitcoin.GetDepositAddress(id);
            return result.ToString();
        }

        [HttpGet]
        public async Task<ActionResult<Money>> GetAddressBalance(string address)
        {
            return await _bitcoin.GetAddressBalance(BitcoinAddress.Create(address, Network.Main));
        }
    }
}