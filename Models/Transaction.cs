using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitar.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PersonalId { get; set; }

        [Required]
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string ShortReference { get; set; }
        public string PaymentDetail { get; set; }

        [Required]
        public decimal Amount { get; set; }
        public AccountData AccountData { get; set; }
    }
}