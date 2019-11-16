using System.ComponentModel.DataAnnotations;

namespace Bitar.Models
{
    public class BitcoinWithdrawal
    {
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public decimal Fees { get; set; }
    }
}