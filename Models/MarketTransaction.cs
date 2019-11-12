using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bitar.Services;
using NBitcoin;

namespace Bitar.Models
{
    public class MarketTransaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PersonalId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public decimal Rate { get; set; }

        [Required]
        public decimal Coins { get; set; }

        [Required]
        public decimal Fee { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string TxId { get; set; }

        [Required]
        public TransactionStatus Status { get; set; }
    }

    public enum TransactionStatus
    {
        Completed,
        Pending,
        Rejected,
        Failed
    }
}