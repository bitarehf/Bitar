using Bitar.Models;
using Bitar.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Bitar.Services
{
    public class BitcoinService
    {
        private readonly ILogger<BitcoinService> _logger;
        private readonly BitcoinSettings _options;
        private readonly RPCClient _client;

        public BitcoinService(ILogger<BitcoinService> logger, IOptions<BitcoinSettings> options)
        {
            _logger = logger;
            _options = options.Value;

            var credentials = new NetworkCredential()
            {
                UserName = _options.Username,
                Password = _options.Password
            };

            var credentialString = new RPCCredentialString()
            {
                Server = _options.Server,
                UserPassword = credentials
            };

            _client = new RPCClient(credentialString, Network.Main);
        }

        public async Task<uint256> MakePayment(string address, Money amount)
        {
            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, Network.Main);
                return await _client.SendToAddressAsync(bitcoinAddress, amount);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return null;
        }
    }
}