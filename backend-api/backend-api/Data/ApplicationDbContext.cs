using backend_api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions options)
          : base(options) { }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationClaim> ApplicationClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (!string.IsNullOrEmpty(tableName) && tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }
        }

        //Seeding
        public async Task SeedDataIfEmptyAsync()
        {
            if (!ApplicationClaims.Any())
            {
                ApplicationClaims.AddRange(
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "True", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "True", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "True", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "Claim", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "Claim", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "Claim", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "Role", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "Role", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "Role", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "User", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "User", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "User", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Assign", ClaimValue = "Role", CreatedDate = DateTime.Now },
                    new ApplicationClaim { ClaimType = "Assign", ClaimValue = "Claim", CreatedDate = DateTime.Now }
                );

                await SaveChangesAsync();
            }
        }

    }
}
