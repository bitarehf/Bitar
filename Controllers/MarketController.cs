using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Repositories;
using Bitar.Services;
using KrakenCore;
using KrakenCore.Models;
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
        private readonly OhlcService _ohlc;

        public MarketController(ILogger<MarketController> logger, MarketRepository market, OhlcService ohlc)
        {
            _logger = logger;
            _market = market;
            _ohlc = ohlc;
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

        [HttpGet]
        public ActionResult<OhlcData> Ohlc()
        {
            return _ohlc.OhlcData;
        }

        [HttpGet]
        public ActionResult<OhlcChartData> OhlcChart()
        {
            return _ohlc.OhlcChartData;
        }
    }
}