using System.Text.Json.Serialization;

namespace Bitar.Models.Dilisense
{


    public partial class DilisenseEntity
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("total_hits")]
        public long TotalHits { get; set; }

        [JsonPropertyName("found_records")]
        public FoundRecord[] FoundRecords { get; set; }
    }

    public partial class FoundRecord
    {
        [JsonPropertyName("source_type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SourceType SourceType { get; set; }

        [JsonPropertyName("pep_type")]
        public string PepType { get; set; }

        [JsonPropertyName("source_id")]
        public string SourceId { get; set; }

        [JsonPropertyName("entity_type")]
        public string EntityType { get; set; }

        [JsonPropertyName("list_date")]
        public string ListDate { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("tl_name")]
        public string TlName { get; set; }

        [JsonPropertyName("last_names")]
        public string[] LastNames { get; set; }

        [JsonPropertyName("alias_names")]
        public string[] AliasNames { get; set; }

        [JsonPropertyName("given_names")]
        public string[] GivenNames { get; set; }

        [JsonPropertyName("alias_given_names")]
        public string[] AliasGivenNames { get; set; }

        [JsonPropertyName("name_remarks")]
        public string[] NameRemarks { get; set; }

        [JsonPropertyName("spouse")]
        public string[] Spouse { get; set; }

        [JsonPropertyName("parents")]
        public string[] Parents { get; set; }

        [JsonPropertyName("siblings")]
        public string[] Siblings { get; set; }

        [JsonPropertyName("children")]
        public string[] Children { get; set; }

        [JsonPropertyName("date_of_birth")]
        public string[] DateOfBirth { get; set; }

        [JsonPropertyName("date_of_birth_remarks")]
        public string[] DateOfBirthRemarks { get; set; }

        [JsonPropertyName("place_of_birth")]
        public string[] PlaceOfBirth { get; set; }

        [JsonPropertyName("place_of_birth_remarks")]
        public string[] PlaceOfBirthRemarks { get; set; }

        [JsonPropertyName("address")]
        public string[] Address { get; set; }

        [JsonPropertyName("address_remarks")]
        public string[] AddressRemarks { get; set; }

        [JsonPropertyName("citizenship")]
        public string[] Citizenship { get; set; }

        [JsonPropertyName("citizenship_remarks")]
        public string[] CitizenshipRemarks { get; set; }

        [JsonPropertyName("sanction_details")]
        public string[] SanctionDetails { get; set; }

        [JsonPropertyName("description")]
        public string[] Description { get; set; }

        [JsonPropertyName("occupations")]
        public string[] Occupations { get; set; }

        [JsonPropertyName("positions")]
        public string[] Positions { get; set; }

        [JsonPropertyName("political_parties")]
        public string[] PoliticalParties { get; set; }

        [JsonPropertyName("links")]
        public string[] Links { get; set; }

        [JsonPropertyName("titles")]
        public string[] Titles { get; set; }

        [JsonPropertyName("functions")]
        public string[] Functions { get; set; }

        [JsonPropertyName("other_information")]
        public string[] OtherInformation { get; set; }
        
        [JsonPropertyName("company_number")]
        public string[] CompanyNumber { get; set; }

        [JsonPropertyName("jurisdiction")]
        public string[] Jurisdiction { get; set; }
    }

    public enum SourceType
    {
        CRIMINAL,
        PEP,
        SANCTION
    }
}
