using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Bitar.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegistrationDate { get; set; }
        public bool IdConfirmed { get; set; } = false;
        
        [Required]
        public bool Institution { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public bool PoliticallyExposed { get; set; }
        public bool CriminalWatchlist { get; set; }
        public bool SanctionList { get; set; }
    }
}