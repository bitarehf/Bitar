using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NBitcoin;

namespace Bitar.Models
{
    public class AccountData
    {
        [Key]
        [StringLength(10, MinimumLength = 10)]
        [Required]
        public string Id { get; set; }
        public string WithdrawalAddress { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Derivation { get; set; }
    }
}