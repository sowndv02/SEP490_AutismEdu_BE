using backend_api.Models;
using backend_api.Utils;
using Microsoft.AspNetCore.Identity;
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
            //builder.Entity<ApplicationClaim>()
            //    .HasOne(a => a.User)
            //    .WithMany(u => u.ApplicationClaims)
            //    .HasForeignKey(a => a.UserId)
            //    .OnDelete(DeleteBehavior.Cascade);

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
            var roleAdmin = Roles.FirstOrDefault(x => x.Name.Equals(SD.Admin));
            var adminUser = ApplicationUsers.FirstOrDefault(x => x.Email.Equals("admin@admin.com"));
            if (!Roles.Any() || roleAdmin == null)
            {
                roleAdmin = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.Admin };
                Roles.Add(roleAdmin);
            }
            if (!ApplicationUsers.Any() || adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@admin.com",
                    FullName = "admin",
                    PasswordHash = PasswordGenerator.GeneratePassword(),
                    UserName = "admin@admin.com",
                    CreatedDate = DateTime.Now,
                };

                ApplicationUsers.Add(adminUser);

                UserRoles.Add(new IdentityUserRole<string>
                {
                    RoleId = roleAdmin.Id,
                    UserId = adminUser.Id
                });
            }

            if (!ApplicationClaims.Any())
            {
                ApplicationClaims.AddRange(
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "True", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "True", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "True", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "Claim", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "Claim", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "Claim", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "Role", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "Role", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "Role", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Create", ClaimValue = "User", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Delete", ClaimValue = "User", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Update", ClaimValue = "User", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Assign", ClaimValue = "Role", CreatedDate = DateTime.Now, UserId = adminUser.Id },
                    new ApplicationClaim { ClaimType = "Assign", ClaimValue = "Claim", CreatedDate = DateTime.Now, UserId = adminUser.Id }
                );

                await SaveChangesAsync();
            }
        }

    }
}
