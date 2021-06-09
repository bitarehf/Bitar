using System.Text.Json.Serialization;

namespace Bitar.Models
{
    public partial class Utxo
    {
        [JsonPropertyName("txid")]
        public string Txid { get; set; }

        [JsonPropertyName("vout")]
        public long Vout { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("value")]
        public long Value { get; set; }
    }

    public partial class Status
    {
        [JsonPropertyName("confirmed")]
        public bool Confirmed { get; set; }

        [JsonPropertyName("block_height")]
        public long BlockHeight { get; set; }

        [JsonPropertyName("block_hash")]
        public string BlockHash { get; set; }

        [JsonPropertyName("block_time")]
        public long BlockTime { get; set; }
    }
}