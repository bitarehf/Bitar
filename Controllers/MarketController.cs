using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Bitar.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class MarketController : ControllerBase
    {
        private readonly ILogger<MarketController> _logger;
        private readonly MarketRepository _market;

        public MarketController(ILogger<MarketController> logger, MarketRepository market)
        {
            _logger = logger;
            _market = market;
        }

        // POST: api.bitar.is/Market/Boing
        [HttpPost]
        public async Task<ActionResult<uint256>> Boing([FromBody] decimal amount)
        {
            _logger.LogDebug($"Order: {amount} isk.");
            var result = await _market.Order("0411002650", amount);
            if (result == null)
            {
                return Conflict("Order failed.");
            }

            return result;
        }

        // POST: api.bitar.is/Market/Order
        [HttpPost]
        public async Task<ActionResult<uint256>> Order([FromBody] decimal amount)
        {
            string id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var result = await _market.Order(id, amount);
            if (result == null)
            {
                return Conflict("Order failed.");
            }

            return result;
        }

        // GET: api.bitar.is/Market/xxx??
        // [HttpGet]
        // public async Task<AccountData> GetAccountData()
        // {
        //     string id = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        //     return await _context.AccountData.FindAsync(id);
        // }
    }
}