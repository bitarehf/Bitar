using Bitar.Models;
using Bitar.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bitar.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly BitarContext _context;
        private LandsbankinnService _landsbankinn;

        public TransactionsController(ILogger<TransactionsController> logger, 
            LandsbankinnService landsbankinn,
            BitarContext context)
        {
            _landsbankinn = landsbankinn;
            _context = context;
        }

        // GET: api/Transaction/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(string id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        [HttpPost]
        public ActionResult<Transaction> Post(Transaction transaction)
        {
            _landsbankinn.transactions.Add(transaction);

            return transaction;
        }
    }
}
