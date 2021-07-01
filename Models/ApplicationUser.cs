using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Bitar.Models
{
    public class ApplicationUser : IdentityUser<int>, IBaseEntity
    {
        public List<Account> Accounts { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public String Kennitala { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IdConfirmed { get; set; } = false;

        [Required]
        public bool Institution { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual DateTime DateUpdated { get; set; }
        public virtual int CreatedBy { get; set; }
        public virtual int UpdatedBy { get; set; }
    }
}