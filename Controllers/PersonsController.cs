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
    public class PersonsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PersonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Persons
        [HttpGet]
        public List<AccountData> GetPersons()
        {
            return _context.AccountData.ToList();
        }

        // GET: api/Persons/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountData>> GetPerson(string id)
        {
            return await _context.AccountData.FindAsync(id);
        }

        // POST: api/Persons
        /// <summary>
        /// Updates Persons WithdrawalAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BitcoinAddress>> UpdateWithdrawalAddress(string id, BitcoinAddress bitcoinAddress)
        {
            var person = await _context.AccountData.FindAsync(id);
            person.WithdrawalAddress = bitcoinAddress.ToString();

            await _context.SaveChangesAsync();

            return BitcoinAddress.Create(person.WithdrawalAddress, Network.Main);
        }
    }
}