using backend_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace backend_api.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor)
          : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationClaim> ApplicationClaims { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<CertificateMedia> CertificateMedias { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportMedia> ReportMedias { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<ChildInformation> ChildInformations { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
		public DbSet<TutorRegistrationRequest> TutorRegistrationRequests { get; set; }
        public DbSet<Curriculum> Curriculums { get; set; }        
        public DbSet<AvailableTime> AvailableTimes { get; set; }
        public DbSet<AvailableTimeSlot> AvailableTimeSlots { get; set; }
        public DbSet<TutorRequest> TutorRequests { get; set; }
        public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
        public DbSet<AssessmentOption> AssessmentOptions { get; set; }
        public DbSet<AssessmentResult> AssessmentResults { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<TutorRequest>()
                .HasOne(t => t.Tutor)
                .WithMany()
                .HasForeignKey(t => t.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TutorRequest>()
                .HasOne(tr => tr.Parent)
                .WithMany()  // or define the correct navigation property
                .HasForeignKey(tr => tr.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Change Cascade to Restrict

            builder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Reviewee)
                .WithMany()
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProgressReport>()
                .HasOne(pr => pr.Tutor)
                .WithMany()
                .HasForeignKey(pr => pr.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

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
            var roleStaff = Roles.FirstOrDefault(x => x.Name.Equals(SD.STAFF_ROLE));
            if (roleStaff == null)
            {
                roleStaff = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.STAFF_ROLE, NormalizedName = SD.STAFF_ROLE.ToUpper() };
                Roles.Add(roleStaff);
            }

            var roleTutor = Roles.FirstOrDefault(x => x.Name.Equals(SD.TUTOR_ROLE));
            if (roleTutor == null)
            {
                roleTutor = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.TUTOR_ROLE, NormalizedName = SD.TUTOR_ROLE.ToUpper() };
                Roles.Add(roleTutor);
            }

            var roleUser = Roles.FirstOrDefault(x => x.Name.Equals(SD.USER_ROLE));
            if (roleUser == null)
            {
                roleUser = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.USER_ROLE, NormalizedName = SD.USER_ROLE.ToUpper() };
                Roles.Add(roleUser);
            }

            var roleParent = Roles.FirstOrDefault(x => x.Name.Equals(SD.PARENT_ROLE));
            if (roleParent == null)
            {
                roleParent = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.PARENT_ROLE, NormalizedName = SD.PARENT_ROLE.ToUpper() };
                Roles.Add(roleParent);
            }

            var roleAdmin = Roles.FirstOrDefault(x => x.Name.Equals(SD.ADMIN_ROLE));
            var adminUser = ApplicationUsers.FirstOrDefault(x => x.Email.Equals("admin@admin.com"));
            if (roleAdmin == null)
            {
                roleAdmin = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.ADMIN_ROLE, NormalizedName = SD.ADMIN_ROLE.ToUpper() };
                Roles.Add(roleAdmin);
            }
            if (!ApplicationUsers.Any() || adminUser == null)
            {

                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@admin.com",
                    FullName = "admin",
                    PasswordHash = SD.ADMIN_PASSWORD_DEFAULT,
                    UserName = "admin@admin.com",
                    CreatedDate = DateTime.Now,
                    ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB
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
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_CREATE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_DELETE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_UPDATE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_CREATE_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_DELETE_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_UPDATE_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_CREATE_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_DELETE_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_UPDATE_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_CREATE_USER_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_CREATE_USER_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_CREATE_USER_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_CREATE_USER_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_DELETE_USER_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_DELETE_USER_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_DELETE_USER_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_DELETE_USER_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_UPDATE_USER_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_UPDATE_USER_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_UPDATE_USER_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_UPDATE_USER_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_ASSIGN_ROLE_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_ASSIGN_ROLE_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_ASSIGN_ROLE_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_ASSIGN_ROLE_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_ASSIGN_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_VIEW_CLAIM_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_VIEW_CLAIM_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_VIEW_CLAIM_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_VIEW_CLAIM_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    },
                    new ApplicationClaim
                    {
                        ClaimType = SD.DEFAULT_VIEW_TUTOR_CLAIM_TYPE,
                        ClaimValue = SD.DEFAULT_VIEW_TUTOR_CLAIM_VALUE,
                        DefaultClaimType = SD.DEFAULT_VIEW_TUTOR_CLAIM_TYPE,
                        DefaultClaimValue = SD.DEFAULT_VIEW_TUTOR_CLAIM_VALUE,
                        CreatedDate = DateTime.Now,
                        UserId = adminUser.Id
                    }
                );

                await SaveChangesAsync();
            }
        }

    }
}
