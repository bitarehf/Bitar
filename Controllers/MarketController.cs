using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Repositories;
using Bitar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

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
        private readonly AssetService _asset;

        public MarketController(ILogger<MarketController> logger, MarketRepository market, OhlcService ohlc, AssetService asset)
        {
            _logger = logger;
            _market = market;
            _ohlc = ohlc;
            _asset = asset;
        }


        // POST: api.bitar.is/Market/Order
        [HttpPost]
        public async Task<ActionResult<string>> Order([FromBody] decimal amount)
        {            
            string accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (accountIdString == null)
            {
                return BadRequest("AccountId missing from request");
            }

            int accountId = int.Parse(accountIdString);

            var result = await _market.Order(accountId, amount);
            if (result == null)
            {
                return Conflict("Order failed.");
            }

            return result.ToString();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<OhlcPair> Ohlc()
        {
            return _ohlc.OhlcPair1440;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<ChartPair> ChartPair()
        {
            return _ohlc.ChartPair1440;
        }
    }
}