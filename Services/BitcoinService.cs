using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class BitcoinService
    {
        private readonly ILogger<BitcoinService> _logger;
        private readonly BitcoinSettings _options;
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly ExtKey _masterKey;
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
        /// Checks whether or we can send the specified amount <paramref name="amount"/> of bitcoin to 
        /// the wallet of the user with the Id <paramref name="id"/> parameter.
        /// </summary>
        /// <param name="id">Id of the receiver.</param>
        /// <param name="money">Amount of bitcoin to send.</param>
        public async Task<bool> BitarCanMakePayment(string id, Money amount)
        {
            try
            {
                var accountData = await GetAccountData(id);

                ExtKey bitarKey = _masterKey.Derive(new KeyPath($"m/84'/0'/0'/0/0"));
                var sender = bitarKey.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/0'/0/0"));
                var receiver = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                var unspentCoins = await _client.ListUnspentAsync(0, int.MaxValue, sender);
                if (unspentCoins == null)
                {
                    return false;
                }

                foreach (var coin in unspentCoins)
                {
                    _logger.LogCritical(
                        $"Address: {coin.Address}\n" +
                        $"Amount: {coin.Amount}\n" +
                        $"Confirmations: {coin.Confirmations}\n" +
                        $"OutPoint: {coin.OutPoint}");
                }

                var coins = unspentCoins.Select(c => c.AsCoin()).ToArray();
                if (coins.Select(c => c.Amount).Sum() < amount)
                {
                    _logger.LogCritical("Not enough funds.");
                    _logger.LogCritical($"Sum: {coins.Select(c => c.Amount).Sum()}");
                    _logger.LogCritical($"Amount: {amount}");
                    _logger.LogCritical("==Coins==");
                    _logger.LogCritical($"{coins.Select(c => c.Amount)}");
                    _logger.LogCritical($"======");
                    _logger.LogCritical("==Coins2==");
                    _logger.LogCritical($"{coins.Count()}");
                    _logger.LogCritical($"======");

                    return false;
                }

                var rate = await _client.EstimateSmartFeeAsync(36, EstimateSmartFeeMode.Economical);
                _logger.LogCritical($"Estimated fee rate: {rate.FeeRate}");

                var coinSelector = new DefaultCoinSelector
                {
                    GroupByScriptPubKey = false
                };

                var builder = Network.Main.CreateTransactionBuilder();
                builder.DustPrevention = false;
                var tx = builder
                    .SetCoinSelector(new DefaultCoinSelector { GroupByScriptPubKey = false })
                    .AddCoins(coins)
                    .AddKeys(bitarKey.PrivateKey)
                    .Send(receiver, amount)
                    .SetChange(sender)
                    .SendEstimatedFees(rate.FeeRate)
                    .BuildTransaction(true);

                _logger.LogCritical(
                    $"vsize: {tx.GetVirtualSize()}\n" +
                    $"{sender} sending {amount} btc to {receiver}" +
                    $"with {builder.EstimateFees(tx, rate.FeeRate)} fees\n" +
                    $"{tx.ToString()}");

                _logger.LogCritical($"TransactionCheckResult: {tx.Check()}");

                if (tx.Check() == TransactionCheckResult.Success)
                {
                    var txId = tx.GetHash();
                    _logger.LogCritical($"TxId: {txId}");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return false;
        }

        /// <summary>
        /// Checks whether or we can send the specified amount <paramref name="amount"/> of bitcoin to 
        /// the wallet of the user with the Id <paramref name="id"/> parameter.
        /// </summary>
        /// <param name="id">Id of the receiver.</param>
        /// <param name="money">Amount of bitcoin to send.</param>
        public async Task<bool> CanMakePayment(string id, Money amount)
        {
            try
            {
                var accountData = await GetAccountData(id);

                ExtKey bitarKey = _masterKey.Derive(new KeyPath($"m/84'/0'/0'/0/0"));
                var sender = bitarKey.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                var receiver = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                UnspentCoin[] unspentCoins;
                if (id == "4708180420")
                {
                    _logger.LogCritical("Bitar ehf. transaction");
                    unspentCoins = await _client.ListUnspentAsync(0, int.MaxValue, sender);
                }
                else
                {
                    _logger.LogCritical("Normal transaction");
                    unspentCoins = await _client.ListUnspentAsync(6, int.MaxValue, sender);
                }

                if (unspentCoins == null)
                {
                    return false;
                }

                foreach (var coin in unspentCoins)
                {
                    _logger.LogCritical(
                        $"Address: {coin.Address}\n" +
                        $"Amount: {coin.Amount}\n" +
                        $"Confirmations: {coin.Confirmations}\n" +
                        $"OutPoint: {coin.OutPoint}");
                }

                var coins = unspentCoins.Select(c => c.AsCoin()).ToArray();
                if (coins.Select(c => c.Amount).Sum() < amount)
                {
                    _logger.LogCritical("Not enough funds.");
                    _logger.LogCritical($"Sum: {coins.Select(c => c.Amount).Sum()}");
                    _logger.LogCritical($"Amount: {amount}");
                    _logger.LogCritical("==Coins==");
                    _logger.LogCritical($"{coins.Select(c => c.Amount)}");
                    _logger.LogCritical($"======");
                    _logger.LogCritical("==Coins2==");
                    _logger.LogCritical($"{coins.Count()}");
                    _logger.LogCritical($"======");

                    return false;
                }

                var rate = await _client.EstimateSmartFeeAsync(36, EstimateSmartFeeMode.Economical);
                _logger.LogCritical($"Estimated fee rate: {rate.FeeRate}");

                var coinSelector = new DefaultCoinSelector
                {
                    GroupByScriptPubKey = false
                };

                var builder = Network.Main.CreateTransactionBuilder();
                builder.DustPrevention = false;
                var tx = builder
                    .SetCoinSelector(new DefaultCoinSelector { GroupByScriptPubKey = false })
                    .AddCoins(coins)
                    .AddKeys(bitarKey.PrivateKey)
                    .Send(receiver, amount)
                    .SetChange(sender)
                    .SendEstimatedFees(rate.FeeRate)
                    .BuildTransaction(true);

                _logger.LogCritical(
                    $"vsize: {tx.GetVirtualSize()}\n" +
                    $"{sender} sending {amount} btc to {receiver}" +
                    $"with {builder.EstimateFees(tx, rate.FeeRate)} fees\n" +
                    $"{tx.ToString()}");

                _logger.LogCritical($"TransactionCheckResult: {tx.Check()}");

                if (tx.Check() == TransactionCheckResult.Success)
                {
                    var txId = tx.GetHash();
                    _logger.LogCritical($"TxId: {txId}");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return false;
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
                using(var scope = _scopeFactory.CreateScope())
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
        /// <param name="receiver">Bitcoin address to send the money to.</param>
        /// <param name="money">Amount of bitcoin to send.</param>
        /// <param name="fees">Amount of fees to be included in the transaction.</param>
        /// <remarks>
        /// Change address is the same as the sender address.
        /// </remarks>
        public async Task<uint256> SendBitcoin(string id, BitcoinAddress receiver, Money amount, Money fees)
        {
            try
            {
                var accountData = await GetAccountData(id);

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                var sender = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                UnspentCoin[] unspentCoins;

                if (id == "4708180420")
                {
                    unspentCoins = await _client.ListUnspentAsync(0, int.MaxValue, sender);
                }
                else
                {
                    unspentCoins = await _client.ListUnspentAsync(6, int.MaxValue, sender);
                }

                if (unspentCoins == null)
                {
                    return null;
                }

                foreach (var coin in unspentCoins)
                {
                    _logger.LogCritical(
                        $"Address: {coin.Address}\n" +
                        $"Amount: {coin.Amount}\n" +
                        $"Confirmations: {coin.Confirmations}\n" +
                        $"OutPoint: {coin.OutPoint}");
                }

                var coins = unspentCoins.Select(c => c.AsCoin()).ToArray();
                if (coins.Select(c => c.Amount).Sum() < amount)
                {
                    _logger.LogCritical("User does not have enough funds.");
                    return null;
                }

                var coinSelector = new DefaultCoinSelector
                {
                    GroupByScriptPubKey = false
                };

                var builder = Network.Main.CreateTransactionBuilder();
                builder.DustPrevention = false;
                var tx = builder
                    .SetCoinSelector(new DefaultCoinSelector { GroupByScriptPubKey = false })
                    .AddCoins(coins)
                    .AddKeys(key.PrivateKey)
                    .Send(receiver, amount)
                    .SetChange(sender)
                    .SendFees(fees)
                    .BuildTransaction(true);

                _logger.LogCritical(
                    $"vsize: {tx.GetVirtualSize()}\n" +
                    $"{sender} sending {amount} btc to {receiver} " +
                    $"with {fees} fees\n" +
                    $"{tx.ToString()}");

                _logger.LogCritical($"TransactionCheckResult: {tx.Check()}");

                if (tx.Check() == TransactionCheckResult.Success)
                {
                    var txId = tx.GetHash();
                    _logger.LogCritical($"TxId: {txId}");
                    // return txId; // Temporary -- Only for testing. 
                    return await _client.SendRawTransactionAsync(tx);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        /// <summary>
        /// Sends specified amount of bitcoin to an address.
        /// Change address is the same as the sender address.
        /// </summary>
        /// <param name="id">Id of the sender.</param>
        /// <param name="receiver">Bitcoin address to send the money to.</param>
        /// <param name="money">Amount of bitcoin to send.</param>
        /// <param name="fees">Amount of fees to be included in the transaction.</param>
        /// <remarks>
        /// Change address is the same as the sender address.
        /// </remarks>
        public async Task<uint256> SendBitcoin(string id, BitcoinAddress receiver, Money amount, int blocks)
        {
            try
            {
                var accountData = await GetAccountData(id);

                var rate = await _client.EstimateSmartFeeAsync(blocks, EstimateSmartFeeMode.Economical);
                _logger.LogCritical($"Estimated fee rate: {rate.FeeRate}");

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                var sender = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);

                UnspentCoin[] unspentCoins;
                if (id == "4708180420")
                {
                    unspentCoins = await _client.ListUnspentAsync(0, int.MaxValue, sender);
                }
                else
                {
                    unspentCoins = await _client.ListUnspentAsync(6, int.MaxValue, sender);
                }

                if (unspentCoins == null)
                {
                    return null;
                }

                foreach (var coin in unspentCoins)
                {
                    _logger.LogCritical(
                        $"Address: {coin.Address}\n" +
                        $"Amount: {coin.Amount}\n" +
                        $"Confirmations: {coin.Confirmations}\n" +
                        $"OutPoint: {coin.OutPoint}");
                }

                var coins = unspentCoins.Select(c => c.AsCoin()).ToArray();
                if (coins.Select(c => c.Amount).Sum() < amount)
                {
                    _logger.LogCritical("User does not have enough funds.");
                    return null;
                }

                var coinSelector = new DefaultCoinSelector
                {
                    GroupByScriptPubKey = false
                };

                var builder = Network.Main.CreateTransactionBuilder();
                builder.DustPrevention = false;
                var tx = builder
                    .SetCoinSelector(new DefaultCoinSelector { GroupByScriptPubKey = false })
                    .AddCoins(coins)
                    .AddKeys(key.PrivateKey)
                    .Send(receiver, amount)
                    .SetChange(sender)
                    .SendEstimatedFees(rate.FeeRate)
                    .BuildTransaction(true);

                var fees = rate.FeeRate.GetFee(tx);

                _logger.LogCritical(
                    $"vsize: {tx.GetVirtualSize()}\n" +
                    $"{sender} sending {amount} btc to {receiver}" +
                    $"with {fees} fees\n" +
                    $"{tx.ToString()}");

                _logger.LogCritical($"TransactionCheckResult: {tx.Check()}");

                if (tx.Check() == TransactionCheckResult.Success)
                {
                    var txId = tx.GetHash();

                    _logger.LogCritical($"TxId: {txId}");
                    // return txId; // Temporary -- Only for testing. 
                    return await _client.SendRawTransactionAsync(tx);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        public async Task<Money> CalculateFees(string id, BitcoinAddress receiver, Money amount, int blocks)
        {
            try
            {
                var accountData = await GetAccountData(id);

                var rate = await EstimateSmartFee(blocks);
                _logger.LogCritical($"Estimated fee rate: {rate}");

                ExtKey key = _masterKey.Derive(new KeyPath($"m/84'/0'/{accountData.Derivation}'/0/0"));
                var sender = key.PrivateKey.PubKey.GetSegwitAddress(Network.Main);
                var unspentCoins = await _client.ListUnspentAsync(6, int.MaxValue, sender);

                var coins = unspentCoins.Select(c => c.AsCoin()).ToArray();
                if (coins.Select(c => c.Amount).Sum() < amount)
                {
                    _logger.LogCritical("User does not have enough funds.");
                    return null;
                }

                var coinSelector = new DefaultCoinSelector
                {
                    GroupByScriptPubKey = false
                };

                var builder = Network.Main.CreateTransactionBuilder();
                builder.DustPrevention = false;
                var tx = builder
                    .SetCoinSelector(new DefaultCoinSelector { GroupByScriptPubKey = false })
                    .AddCoins(coins)
                    .AddKeys(key.PrivateKey)
                    .Send(receiver, amount)
                    .SetChange(sender)
                    .SendEstimatedFees(rate)
                    .BuildTransaction(true);

                var fees = rate.GetFee(tx);

                _logger.LogCritical($"Transaction Fees: {fees}");
                _logger.LogCritical($"TransactionCheckResult: {tx.Check()}");

                if (tx.Check() == TransactionCheckResult.Success)
                {
                    return fees;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null;
        }

        public async Task<Money> GetTransactionFees(int virtualSize, int blocks)
        {
            var rate = await EstimateSmartFee(blocks);
            return rate.GetFee(virtualSize);
        }

        public async Task<Money> GetAddressBalance(BitcoinAddress address)
        {
            Money total = new Money(Decimal.Zero, MoneyUnit.BTC);

            var utxos = await _client.ListUnspentAsync(1, int.MaxValue, address);
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

        /// <summary>
        /// Estimate the optimal amount of fees for the specified amount of blocks.
        /// </summary>
        /// <param name="blocks">Amount of blocks to estimate from.</param>
        public async Task<FeeRate> EstimateSmartFee(int blocks)
        {
            var result = await _client.EstimateSmartFeeAsync(blocks, EstimateSmartFeeMode.Economical);
            return result.FeeRate;
        }
    }
}