using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitar.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }
        public string Kennitala { get; set; }

        [Required]
        public DateTime Time { get; set; }
        public string Reference { get; set; }
        public string ShortReference { get; set; }
        public string PaymentDetail { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}