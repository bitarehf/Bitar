using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Repositories;
using Bitar.Services;
using KrakenCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bitar.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly AssetService _asset;

        public AssetController(ILogger<AssetController> logger, AssetService asset)
        {
            _logger = logger;
            _asset = asset;
        }

        // [Route("{asset}")]
        // [HttpGet]
        // public ActionResult<TickerInfo> Asset(string asset)
        // {
        //     return null;
        // }

        [Route("{asset}/{start:datetime}/{end:datetime}")]
        [HttpGet]
        public ActionResult<IEnumerable<Asset>> Asset(string asset, DateTime start, DateTime end)
        {
            return _asset.Assets[asset]
                .Where(a => a.Time >= start && a.Time <= end).ToList();
        }
    }
}