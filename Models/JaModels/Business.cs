namespace Bitar.Models.JaModels
{
    using System;
    using System.Text.Json.Serialization;

    public partial class Business
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("kennitala")]
        public string Kennitala { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }

        [JsonPropertyName("alt_foreign_name")]
        public object AltForeignName { get; set; }

        [JsonPropertyName("is_company")]
        public bool IsCompany { get; set; }

        [JsonPropertyName("business_type")]
        public BusinessType BusinessType { get; set; }

        [JsonPropertyName("business_activity")]
        public object BusinessActivity { get; set; }

        [JsonPropertyName("parent_company_kennitala")]
        public object ParentCompanyKennitala { get; set; }

        [JsonPropertyName("director")]
        public string Director { get; set; }

        [JsonPropertyName("legal_address")]
        public AlAddress LegalAddress { get; set; }

        [JsonPropertyName("postal_address")]
        public AlAddress PostalAddress { get; set; }

        [JsonPropertyName("international_address")]
        public object InternationalAddress { get; set; }

        [JsonPropertyName("receiver")]
        public object Receiver { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("share_capital")]
        public long ShareCapital { get; set; }

        [JsonPropertyName("remarks")]
        public object Remarks { get; set; }

        [JsonPropertyName("banned")]
        public bool Banned { get; set; }

        [JsonPropertyName("isat")]
        public BusinessType Isat { get; set; }

        [JsonPropertyName("vsk")]
        public Vsk[] Vsk { get; set; }

        [JsonPropertyName("date_bankrupt")]
        public object DateBankrupt { get; set; }

        [JsonPropertyName("date_established")]
        public DateTimeOffset DateEstablished { get; set; }

        [JsonPropertyName("registered_at")]
        public DateTimeOffset RegisteredAt { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTimeOffset ModifiedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("see_also")]
        public SeeAlso SeeAlso { get; set; }
    }

    public partial class BusinessType
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public Name Name { get; set; }
    }

    public partial class AlAddress
    {
        [JsonPropertyName("street")]
        public Street Street { get; set; }

        [JsonPropertyName("postal_code")]
        public long PostalCode { get; set; }

        [JsonPropertyName("town")]
        public Street Town { get; set; }

        [JsonPropertyName("country")]
        public BusinessType Country { get; set; }

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("coordinates")]
        public Coordinates Coordinates { get; set; }
    }

    public partial class Vsk
    {
        [JsonPropertyName("vsk_number")]
        public String VskNumber { get; set; }

        [JsonPropertyName("isat")]
        public BusinessType Isat { get; set; }

        [JsonPropertyName("opened")]
        public DateTimeOffset Opened { get; set; }

        [JsonPropertyName("closed")]
        public object Closed { get; set; }
    }
}
