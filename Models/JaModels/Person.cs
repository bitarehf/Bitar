namespace Bitar.Models.JaModels
{
    using System;
    using System.Text.Json.Serialization;

    public partial class Person
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("kennitala")]
        public string Kennitala { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("citizenship")]
        public Citizenship Citizenship { get; set; }

        [JsonPropertyName("date_of_birth")]
        public DateTimeOffset DateOfBirth { get; set; }

        [JsonPropertyName("date_of_birth_inferred")]
        public DateTimeOffset DateOfBirthInferred { get; set; }

        [JsonPropertyName("age")]
        public long Age { get; set; }

        [JsonPropertyName("age_year_end")]
        public long AgeYearEnd { get; set; }

        [JsonPropertyName("birth_place")]
        public BirthPlace BirthPlace { get; set; }

        [JsonPropertyName("permanent_address")]
        public PermanentAddress PermanentAddress { get; set; }

        [JsonPropertyName("legal_residence")]
        public Residence LegalResidence { get; set; }

        [JsonPropertyName("december_legal_residence")]
        public Residence DecemberLegalResidence { get; set; }

        [JsonPropertyName("last_legal_residence")]
        public Residence LastLegalResidence { get; set; }

        [JsonPropertyName("temporary_residence")]
        public Residence TemporaryResidence { get; set; }

        [JsonPropertyName("tax_country")]
        public object TaxCountry { get; set; }

        [JsonPropertyName("family_kennitala")]
        public string FamilyKennitala { get; set; }

        [JsonPropertyName("partner_kennitala")]
        public string PartnerKennitala { get; set; }

        [JsonPropertyName("marital_status")]
        public MaritalStatus MaritalStatus { get; set; }

        [JsonPropertyName("proxy_kennitala")]
        public object ProxyKennitala { get; set; }

        [JsonPropertyName("banned")]
        public bool Banned { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("date_of_death")]
        public DateTimeOffset DateOfDeath { get; set; }

        [JsonPropertyName("kennitala_requested_by")]
        public object KennitalaRequestedBy { get; set; }

        [JsonPropertyName("alt_kennitala")]
        public object AltKennitala { get; set; }

        [JsonPropertyName("date_registered")]
        public object DateRegistered { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("see_also")]
        public SeeAlso SeeAlso { get; set; }
    }

    public partial class BirthPlace
    {
        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("country")]
        public Citizenship Country { get; set; }
    }

    public partial class Citizenship
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public Name Name { get; set; }
    }

    public partial class Name
    {
        [JsonPropertyName("is")]
        public string Is { get; set; }

        [JsonPropertyName("en")]
        public string En { get; set; }
    }

    public partial class Residence
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("country")]
        public Citizenship Country { get; set; }
    }

    public partial class MaritalStatus
    {
        [JsonPropertyName("code")]
        public long Code { get; set; }

        [JsonPropertyName("description")]
        public Name Description { get; set; }
    }

    public partial class PermanentAddress
    {
        [JsonPropertyName("street")]
        public Street Street { get; set; }

        [JsonPropertyName("postal_code")]
        public long PostalCode { get; set; }

        [JsonPropertyName("town")]
        public Street Town { get; set; }

        [JsonPropertyName("country")]
        public Citizenship Country { get; set; }

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("coordinates")]
        public Coordinates Coordinates { get; set; }
    }

    public partial class Coordinates
    {
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("x_isn93")]
        public long XIsn93 { get; set; }

        [JsonPropertyName("y_isn93")]
        public long YIsn93 { get; set; }
    }

    public partial class Street
    {
        [JsonPropertyName("nominative")]
        public string Nominative { get; set; }

        [JsonPropertyName("dative")]
        public string Dative { get; set; }
    }

    public partial class SeeAlso
    {
        [JsonPropertyName("map")]
        public Uri Map { get; set; }
    }
}
