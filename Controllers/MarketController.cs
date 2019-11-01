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
    [Authorize]
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
        [AllowAnonymous]
        public async Task<ActionResult<string>> Boing([FromBody] decimal amount)
        {
            _logger.LogDebug($"Order: {amount} isk.");
            var result = await _market.Order("0411002650", amount);
            if (result == null)
            {
                return Conflict("Order failed.");
            }

            return result.ToString();
        }

        // POST: api.bitar.is/Market/Order
        [HttpPost]
        public async Task<ActionResult<string>> Order([FromBody] decimal amount)
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return NotFound("User not found.");
            }

            var result = await _market.Order(id, amount);
            if (result == null)
            {
                return Conflict("Order failed.");
            }

            return result.ToString();
        }
    }
}