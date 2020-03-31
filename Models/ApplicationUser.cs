using System;
using Microsoft.AspNetCore.Identity;

namespace Bitar.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegistrationDate { get; set; }
    }
}