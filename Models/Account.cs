using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bitar.Models.Dilisense;

namespace Bitar.Models
{
    public class Account : IBaseEntity
    {
        public int Id { get; set; }
        public List<ApplicationUser> Users { get; set; }
        public string Name { get; set; }

        [StringLength(10, MinimumLength = 10)]
        public string Kennitala { get; set; }
        public string WithdrawalAddress { get; set; }
        public string BankAccountNumber { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Derivation { get; set; }

        [Required]
        public decimal Fee { get; set; } = 0.5m;

        [DefaultValue(0)]
        [Range(0, int.MaxValue)]
        [ConcurrencyCheck]
        public decimal Balance { get; set; }
        public bool IdConfirmed { get; set; } = false;

        [Required]
        public bool Institution { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }

        [ConcurrencyCheck]
        [ForeignKey("AccountId")]
        public virtual List<Transaction> Transactions { get; set; }

        [ConcurrencyCheck]
        [ForeignKey("AccountId")]
        public virtual List<MarketTransaction> MarketTransactions { get; set; }
        
        [ForeignKey("AccountId")]
        public virtual List<DilisenseRecord> DilisenseRecords { get; set; }

        [ForeignKey("AccountId")]
        public virtual List<KnowYourCustomer> KnowYourCustomerRecords { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual DateTime DateUpdated { get; set; }
        public virtual int CreatedBy { get; set; }
        public virtual int UpdatedBy { get; set; }
    }
}