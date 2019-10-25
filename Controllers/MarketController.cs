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
    public class MarketController : ControllerBase
    {
        private readonly ILogger<MarketController> _logger;
        private readonly MarketService _market;
        private readonly ApplicationDbContext _context;

        public MarketController(ILogger<MarketController> logger, ApplicationDbContext context, MarketService market)
        {
            _logger = logger;
            _market = market;
            _context = context;
        }

        // GET: api.bitar.is/Market/xxx??
        [HttpGet]
        public async Task<AccountData> GetAccountData()
        {
            string id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.AccountData.FindAsync(id);
        }



        // POST: api.bitar.is/Market/Buy
        /// <summary>
        /// Buy bitcoin
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<string>> Buy(decimal isk)
        {
            string id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var result = await _market.Buy(id, isk);

            return result.ToString();
        }
    }
}