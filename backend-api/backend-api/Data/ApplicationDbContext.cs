using backend_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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


        public ApplicationDbContext(DbContextOptions options)
          : base(options)
        {
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
        public DbSet<AvailableTimeSlot> AvailableTimeSlots { get; set; }
        public DbSet<TutorRequest> TutorRequests { get; set; }
        public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
        public DbSet<AssessmentOption> AssessmentOptions { get; set; }
        public DbSet<AssessmentResult> AssessmentResults { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<TutorProfileUpdateRequest> TutorProfileUpdateRequests { get; set; }
        public DbSet<InitialAssessmentResult> InitialAssessmentResults { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ScheduleTimeSlot> ScheduleTimeSlots { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<EmailLogger> EmailLoggers { get; set; }
        public DbSet<ExerciseType> ExerciseTypes { get; set; }
        public DbSet<Exercise> Exercisese { get; set; }
        public DbSet<Syllabus> Syllabuses { get; set; }
        public DbSet<SyllabusExercise> SyllabusExercises { get; set; }
        public DbSet<AssessmentScoreRange> AssessmentScoreRanges { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<TestResultDetail> TestResultDetails { get; set; }
        public DbSet<PackagePayment> PackagePayments { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PaymentHistory>()
                .HasOne(a => a.Submitter)
                .WithMany()
                .HasForeignKey(a => a.SubmitterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AssessmentQuestion>()
                .HasOne(a => a.Submitter)
                .WithMany()
                .HasForeignKey(a => a.SubmitterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedule>()
                .HasOne(se => se.Syllabus)
                .WithMany()
                .HasForeignKey(se => se.SyllabusId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<Schedule>()
                .HasOne(se => se.ExerciseType)
                .WithMany()
                .HasForeignKey(se => se.ExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<Schedule>()
                .HasOne(se => se.Exercise)
                .WithMany()
                .HasForeignKey(se => se.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<TutorRequest>()
                .HasOne(tr => tr.Tutor)
                .WithMany(t => t.Requests)
                .HasForeignKey(tr => tr.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SyllabusExercise>()
         .HasKey(se => new { se.SyllabusId, se.ExerciseTypeId, se.ExerciseId });

            builder.Entity<SyllabusExercise>()
                .HasOne(se => se.Syllabus)
                .WithMany(s => s.SyllabusExercises)
                .HasForeignKey(se => se.SyllabusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SyllabusExercise>()
                .HasOne(se => se.ExerciseType)
                .WithMany(et => et.SyllabusExercises)
                .HasForeignKey(se => se.ExerciseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SyllabusExercise>()
                .HasOne(se => se.Exercise)
                .WithMany(e => e.SyllabusExercises)
                .HasForeignKey(se => se.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TutorRequest>()
                .HasOne(tr => tr.Parent) // Assuming TutorRequest has a Parent navigation property
                .WithMany(p => p.TutorRequests) // Assuming Parent has a collection of TutorRequests
                .HasForeignKey(tr => tr.ParentId) // Foreign key property
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tutor>()
                .HasMany(t => t.Reviews)
                .WithOne(r => r.Tutor)
                .HasForeignKey(r => r.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tutor>()
                .HasMany(t => t.Exercises)
                .WithOne(r => r.Tutor)
                .HasForeignKey(r => r.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProgressReport>()
                .HasOne(pr => pr.Tutor)
                .WithMany()
                .HasForeignKey(pr => pr.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentProfile>()
                .HasOne(pr => pr.Tutor)
                .WithMany()
                .HasForeignKey(pr => pr.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ScheduleTimeSlot>()
                .HasOne(pr => pr.StudentProfile)
                .WithMany()
                .HasForeignKey(pr => pr.StudentProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedule>()
                .HasOne(pr => pr.Tutor)
                .WithMany()
                .HasForeignKey(pr => pr.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentProfile>()
                .HasMany(sp => sp.ScheduleTimeSlots)
                .WithOne(st => st.StudentProfile)
                .HasForeignKey(st => st.StudentProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InitialAssessmentResult>()
                .HasOne(pr => pr.Question)
                .WithMany()
                .HasForeignKey(pr => pr.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AssessmentResult>()
                .HasOne(pr => pr.Question)
                .WithMany()
                .HasForeignKey(pr => pr.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TestResultDetail>()
                .HasOne(pr => pr.Question)
                .WithMany()
                .HasForeignKey(pr => pr.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TestResult>()
                .HasOne(tr => tr.Parent)
                .WithMany()
                .HasForeignKey(tr => tr.ParentId)
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

            var roleParent = Roles.FirstOrDefault(x => x.Name.Equals(SD.PARENT_ROLE));
            if (roleParent == null)
            {
                roleParent = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.PARENT_ROLE, NormalizedName = SD.PARENT_ROLE.ToUpper() };
                Roles.Add(roleParent);
            }
            var roleManager = Roles.FirstOrDefault(x => x.Name.Equals(SD.MANAGER_ROLE));
            if (roleManager == null)
            {
                roleManager = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = SD.MANAGER_ROLE, NormalizedName = SD.MANAGER_ROLE.ToUpper() };
                Roles.Add(roleManager);
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
                    ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB,
                    EmailConfirmed = true,
                    IsLockedOut = false,
                    Address = SD.ADMIN_ADDRESS_DEFAULT,
                    LockoutEnabled = false,
                    NormalizedEmail = SD.ADMIN_EMAIL_DEFAULT.ToUpper(),
                    NormalizedUserName = SD.ADMIN_ADDRESS_DEFAULT.ToUpper(),
                    AccessFailedCount = 0,
                    PhoneNumber = SD.ADMIN_PHONENUMBER_DEFAULT,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    UserType = SD.APPLICATION_USER
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

            if (!ExerciseTypes.Any())
            {
                ExerciseTypes.AddRange(

                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_1,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_2,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_3,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_4,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_5,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_6,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_7,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_8,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_9,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_10,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_11,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_12,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_13,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_14,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_15,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_16,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_17,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_18,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_19,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_20,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_21,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_22,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_23,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_24,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_25,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_26,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_27,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_28,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_29,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new ExerciseType
                    {
                        ExerciseTypeName = SD.DEFAULT_EXERCISE_TYPE_30,
                        SubmitterId = adminUser.Id,
                        IsActive = true,
                        IsDeleted = false
                    }
                );

                await SaveChangesAsync();
            }
        }

    }
}
