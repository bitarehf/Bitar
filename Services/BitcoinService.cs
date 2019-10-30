using Bitar.Models;
using Bitar.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
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
        private readonly ExtKey _masterKey;
        public readonly RPCClient _client;

        public BitcoinService(
            ILogger<BitcoinService> logger,
            IOptions<BitcoinSettings> options,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _masterKey = new BitcoinExtKey(_options.MasterKey, Network.Main);

            var credentialString = new RPCCredentialString
            {
                Server = _options.Server,
                UserPassword = new NetworkCredential
                {
                    UserName = _options.Username,
                    Password = _options.Password
                }
            };

            _client = new RPCClient(credentialString, Network.Main);
        }

        /// <summary>
        /// Sends specified amount <paramref name="amount"/> of bitcoin to the wallet of the user
        /// with Id value of the Id <paramref name="id"/> parameter.
        /// </summary>
        /// <param name="id">Id of the receiver.</param>
        /// <param name="money">Amount of bitcoin to send.</param>
        /// <remarks>
        /// Change address is the same as the sender address.
        /// </remarks>
        /// <returns>Transaction (uint256) or null.</returns>
        public async Task<uint256> MakePayment(string id, Money amount)
        {
            try
            {
                var accountData = await GetAccountData(id);

                ExtKey bitarKey = _masterKey.Derive(new KeyPath($"m/84'/0'/0'/0/0"));
                var sender = bitarKey.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                var receiver = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                var unspentCoins = await _client.ListUnspentAsync(6, int.MaxValue, sender);
                foreach (var coin in unspentCoins)
                {
                    _logger.LogDebug($"Address: {coin.Address}");
                    _logger.LogDebug($"Amount: {coin.Amount}");
                    _logger.LogDebug($"Confirmations: {coin.Confirmations}");
                    _logger.LogDebug($"OutPoint: {coin.OutPoint}");
                    _logger.LogDebug($"ScriptPubKey: {coin.ScriptPubKey}");
                }

                var coins = unspentCoins.Select(c => c.AsCoin()).ToArray();
                foreach (var coin in coins)
                {
                    _logger.LogDebug($"Amount: {coin.Amount}");
                    _logger.LogDebug($"CanGetScriptCode: {coin.CanGetScriptCode}");
                    _logger.LogDebug($"Outpoint: {coin.Outpoint}");
                    _logger.LogDebug($"ScriptPubKey: {coin.ScriptPubKey}");
                    _logger.LogDebug($"TxOut: {coin.TxOut}");
                }

                if (coins.Select(c => c.Amount).Sum() < amount)
                {
                    _logger.LogCritical("Not enough funds.");
                    return null;
                }

                var rate = await _client.EstimateSmartFeeAsync(8);
                _logger.LogDebug($"Estimated fee rate: {rate.FeeRate}");

                var coinSelector = new DefaultCoinSelector
                {
                    GroupByScriptPubKey = false
                };


                var tx = Network.Main.CreateTransactionBuilder()
                    .SetCoinSelector(coinSelector)
                    .AddCoins(coins)
                    .AddKeys(bitarKey.PrivateKey)
                    .Send(receiver, amount)
                    .SetChange(sender)
                    .SendEstimatedFees(rate.FeeRate)
                    .BuildTransaction(true);

                _logger.LogDebug(tx.ToString());

                //return await _client.SendRawTransactionAsync(tx);

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
        public async Task ImportAddress(BitcoinWitPubKeyAddress bitcoinAddress, string id)
        {
            await _client.ImportAddressAsync(bitcoinAddress, id, false);
        }

        public async Task<AccountData> GetAccountData(string id)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    return await context.AccountData.FindAsync(id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        public async Task<BitcoinWitPubKeyAddress> GetDepositAddress(string id)
        {
            var accountData = await GetAccountData(id);

            // BIP84 - Derivation scheme for P2WPKH based accounts.
            // m / 84' / coin_type' / account' / change / address
            ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
            return key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);
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
                var accountData = await GetAccountData(id);

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                var senderAddress = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);
                var unspentCoins = await _client.ListUnspentAsync(6, 99999999, senderAddress);
                var estimateFeeRate = await _client.EstimateSmartFeeAsync(8);

                var tx = Network.Main.CreateTransactionBuilder()
                    .AddCoins(unspentCoins.Select(c => c.AsCoin()))
                    .AddKeys(key)
                    .Send(receiverAddress, amount)
                    .SendEstimatedFees(estimateFeeRate.FeeRate)
                    .SetChange(senderAddress)
                    .BuildTransaction(true);

                //return await _client.SendRawTransactionAsync(tx);

            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        public async Task<Money> GetAddressBalance(BitcoinAddress address)
        {
            Money total = new Money(Decimal.Zero, MoneyUnit.BTC);

            var utxos = await _client.ListUnspentAsync(1, 99999999, address);
            if (utxos == null)
            {
                _logger.LogCritical("utxos null?");
            }

            foreach (var utxo in utxos)
            {
                total += utxo.Amount;
            }

            return total;
        }
    }
}