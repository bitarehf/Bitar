using Microsoft.EntityFrameworkCore;

namespace Bitar.Models
{
    public class BitarContext : DbContext
    {
        public BitarContext(DbContextOptions<BitarContext> options) : base(options) { }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
    }
}