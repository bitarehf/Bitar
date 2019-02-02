using Bitar.Models;
using Bitar.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Bitar.Services
{
    public class BitcoinService
    {
        private readonly ILogger<BitcoinService> _logger;
        private readonly BitcoinSettings _options;
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly RPCClient _client;

        public BitcoinService(
            ILogger<BitcoinService> logger,
            IOptions<BitcoinSettings> options,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _options = options.Value;
            _scopeFactory = scopeFactory;

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

        /// <summary>
        /// Imports address to the bitcoin node without a rescan.
        /// </summary>
        public async Task ImportAddress(BitcoinAddress bitcoinAddress)
        {
            await _client.ImportAddressAsync(bitcoinAddress, "", false);
        }


        /// <summary>
        /// Sends specified amount of bitcoin to an address.
        /// Change address is the same as the sender address.
        /// </summary>
        /// <param name="id">Id of the sender.</param>
        /// <param name="receiverAddress">Bitcoin address to send the money to.</param>
        /// <param name="money">Amount of bitcoin to send.</param>
        /// <returns>Transaction or null.</returns>
        public async Task<uint256> SendBitcoin(string id, BitcoinAddress receiverAddress, Money amount)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var accountData = await context.AccountData.FindAsync(id);
                    var bitcoinSecret = new BitcoinSecret(accountData.BitcoinSecret, Network.Main);
                    var senderAddress = BitcoinAddress.Create(accountData.DepositAddress, Network.Main);
                    var unspentCoins = await _client.ListUnspentAsync(6, 99999999, senderAddress);
                    var estimateFeeRate = await _client.EstimateSmartFeeAsync(8);

                    var tx = Network.Main.CreateTransactionBuilder()
                        .AddCoins(unspentCoins.Select(c => c.AsCoin()))
                        .AddKeys(bitcoinSecret)
                        .Send(receiverAddress, amount)
                        .SendEstimatedFees(estimateFeeRate.FeeRate)
                        .SetChange(senderAddress)
                        .BuildTransaction(true);

                    return await _client.SendRawTransactionAsync(tx);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }
    }
}