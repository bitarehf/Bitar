using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bitar.Models
{
    public class Person
    {
        [Key]
        [StringLength(10, MinimumLength = 10)]
        public string SSN { get; set; }
        [Required]
        public string BitcoinAddress { get; set; }
    }
}