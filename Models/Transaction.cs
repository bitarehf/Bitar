using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitar.Models
{
    public class Transaction
    {
        [Key]
        public string Id { get; set; }
        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string SSN { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string TxId { get; set; }
    }
}