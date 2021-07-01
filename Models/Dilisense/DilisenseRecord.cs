using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Bitar.Models.Dilisense
{

    public class DilisenseRecord
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public bool PoliticallyExposed { get; set; }
        public bool CriminalList { get; set; }
        public bool SanctionList { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime DateCreated { get; set; }
    }
}
