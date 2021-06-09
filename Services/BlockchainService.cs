using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bitar.Models;
using Bitar.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NBitcoin;
using NBitcoin.RPC;

namespace Bitar.Services
{
    public class BlockchainService
    {
        private readonly ILogger<BitcoinService> _logger;
        private readonly BitcoinSettings _options;
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly ExtKey _masterKey;
        private readonly HttpClient _client;

        public BlockchainService(
            ILogger<BitcoinService> logger,
            IOptions<BitcoinSettings> options,
            HttpClient client)
        {
            _logger = logger;
            _options = options.Value;
            _masterKey = new BitcoinExtKey(_options.MasterKey, Network.Main);
            client.BaseAddress = new Uri("https://blockchain.bitar.is/");
            _client = client;
        }

        public async Task<Money> GetAddressBalance(BitcoinAddress address)
        {
            Money total = new Money(Decimal.Zero, MoneyUnit.Satoshi);

            string response = await _client.GetStringAsync($"/address/{address}/utxo");

            List<Utxo> utxos = JsonSerializer.Deserialize<List<Utxo>>(response);

            if (utxos == null)
            {
                _logger.LogCritical("utxos null?");
            }

            foreach (var utxo in utxos)
            {
                if (utxo.Status.Confirmed)
                {
                    total += utxo.Value;
                }
            }

            return total;
        }

    }
}