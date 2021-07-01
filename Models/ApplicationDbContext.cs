using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bitar.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public DbSet<Account> Account { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return (await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                // For entities that inherit from BaseEntity,
                // set Updated and Created appropriately.
                if (entry.Entity is IBaseEntity baseEntity)
                {
                    var now = DateTime.Now;
                    var user = GetCurrentUser();
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            baseEntity.DateUpdated = now;
                            baseEntity.UpdatedBy = user;
                            break;

                        case EntityState.Added:
                            baseEntity.DateCreated = now;
                            baseEntity.DateUpdated = now;
                            baseEntity.CreatedBy = user;
                            baseEntity.UpdatedBy = user;
                            break;
                    }
                }
            }
        }

        private int GetCurrentUser()
        {
            var id = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                // Created by system account.
                // Should never be used besides on registration.
                return 0;
            }
            
            return 0;
            //return int.Parse(id);
        }
    }
}