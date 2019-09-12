using Bitar.Models;
using Bitar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bitar.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private LandsbankinnService _landsbankinn;

        public TransactionsController(LandsbankinnService landsbankinn,
            ApplicationDbContext context)
        {
            _landsbankinn = landsbankinn;
            _context = context;
        }

        // GET: api/Transaction/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(string id)
        {
            return null;
            //return await _context.Transactions.FindAsync(id);
        }

        [HttpPost]
        public ActionResult<Transaction> Post(Transaction transaction)
        {
            // Only use this in the test environment.
            //_landsbankinn.transactions.Add(transaction);

            return transaction;
        }
    }
}
