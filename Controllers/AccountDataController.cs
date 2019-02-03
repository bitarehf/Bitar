using Bitar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bitar.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountDataDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountDataDetailsController(ApplicationDbContext context)
        {
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
    }
}