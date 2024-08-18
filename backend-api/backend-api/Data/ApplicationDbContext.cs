using backend_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Emit;

namespace backend_api.Data
{
    public class ApplicationDbContext :IdentityDbContext
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
            SeedData(builder);
        }

        //Seeding
        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationClaim>().HasData(
                new ApplicationClaim { Id = 1, ClaimType = "Create", ClaimValue = "True", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 2, ClaimType = "Delete", ClaimValue = "True", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 3, ClaimType = "Update", ClaimValue = "True", CreatedDate = DateTime.Now },

                new ApplicationClaim { Id = 4, ClaimType = "Create", ClaimValue = "Claim", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 5, ClaimType = "Delete", ClaimValue = "Claim", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 6, ClaimType = "Update", ClaimValue = "Claim", CreatedDate = DateTime.Now },

                new ApplicationClaim { Id = 7, ClaimType = "Create", ClaimValue = "Role", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 8, ClaimType = "Delete", ClaimValue = "Role", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 9, ClaimType = "Update", ClaimValue = "Role", CreatedDate = DateTime.Now },

                new ApplicationClaim { Id = 10, ClaimType = "Create", ClaimValue = "User", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 11, ClaimType = "Delete", ClaimValue = "User", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 12, ClaimType = "Update", ClaimValue = "User", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 13, ClaimType = "Assign", ClaimValue = "Role", CreatedDate = DateTime.Now },
                new ApplicationClaim { Id = 13, ClaimType = "Assign", ClaimValue = "Claim", CreatedDate = DateTime.Now }
            );
           
        }

    }
}
