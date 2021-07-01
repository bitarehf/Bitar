using System;
using System.ComponentModel.DataAnnotations;

namespace Bitar.Models
{
    public class KnowYourCustomer
    {
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        public string Occupation { get; set; }

        [Required]
        public string OriginOfFunds { get; set; }

        [Required]
        public bool OwnerOfFunds { get; set; }

        public DateTime Time { get; set; }
    }
}