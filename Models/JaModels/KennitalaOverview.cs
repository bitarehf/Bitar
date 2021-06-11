namespace Bitar.Models.JaModels
{
    using System;
    using System.Text.Json.Serialization;

    public partial class KennitalaOverview
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("kennitala")]
        public string Kennitala { get; set; }

        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("kennitala_type")]
        public string KennitalaType { get; set; }

        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

        [JsonPropertyName("birth_number")]
        public long BirthNumber { get; set; }

        [JsonPropertyName("check_digit")]
        public long CheckDigit { get; set; }

        [JsonPropertyName("century")]
        public long Century { get; set; }

        [JsonPropertyName("see_also")]
        public SeeAlso SeeAlso { get; set; }
    }

    public partial class SeeAlso
    {
        [JsonPropertyName("data")]
        public Uri Data { get; set; }

        [JsonPropertyName("search")]
        public Uri Search { get; set; }
    }
}