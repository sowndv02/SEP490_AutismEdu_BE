using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace AutismEduConnectSystem.Data
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
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Attachment> Attachments { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Conversation>()
                .HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

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

                if (!AssessmentQuestions.Any())
                {
                    AssessmentQuestions.AddRange(

                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "VỀ QUAN HỆ VỚI MỌI NGƯỜI", CreatedDate = new DateTime(2024, 10, 26, 9, 14, 25, 370), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "BẮT CHƯỚC", CreatedDate = new DateTime(2024, 10, 26, 9, 14, 25, 370), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "THỂ HIỆN TÌNH CẢM", CreatedDate = new DateTime(2024, 10, 26, 9, 14, 25, 370), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "CÁC ĐỘNG TÁC CƠ THỂ", CreatedDate = new DateTime(2024, 10, 26, 9, 14, 25, 370), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "SỬ DỤNG ĐỒ VẬT", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "SỰ THÍCH ỨNG VỚI THAY ĐỔI", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "PHẢN ỨNG THỊ GIÁC", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "PHẢN ỨNG THÍNH GIÁC", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "VỊ GIÁC, XÚC GIÁC, KHỨU GIÁC", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "SỰ SỢ HÃI HOẶC HỒI HỘP", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "GIAO TIẾP BẰNG LỜI", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "GIAO TIẾP KHÔNG LỜI", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "MỨC ĐỘ HOẠT ĐỘNG", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "MỨC ĐỘ VÀ SỰ NHẤT QUÁN CỦA PHẢN XẠ THÔNG MINH", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false },
                        new AssessmentQuestion { SubmitterId = adminUser.Id, Question = "ẤN TƯỢNG CHUNG", CreatedDate = new DateTime(2024, 10, 29, 11, 3, 2, 196), IsHidden = false }
                    );
                }
                await SaveChangesAsync();
                if (!AssessmentOptions.Any())
                {
                    AssessmentOptions.AddRange(

                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Không có biểu hiện khó khăn hoặc bất thường trong quan hệ với mọi người: Hành vi của trẻ tương ứng với tuổi. Có thể thấy được một số hiện tượng bẽn lẽn, nhắng nhít hoặc khó chịu khi bị yêu cầu làm việc gì, nhưng không ở mức độ không điển hình.",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Quan hệ có chút bất thường ở mức độ nhẹ: Trẻ đôi khi có vẻ lưỡng lự hoặc ngại ngùng trong việc tương tác với người lớn, nhưng vẫn có thể duy trì sự tiếp xúc với một chút khuyến khích từ môi trường xung quanh. Các biểu hiện bẽn lẽn hoặc né tránh không rõ ràng và chỉ thỉnh thoảng xuất hiện.",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Quan hệ không bình thường ở mức độ nhẹ: Trẻ tránh tiếp xúc với người lớn bằng ánh mắt, tránh người lớn hoặc trở nên nhắng nhít nếu như có sự tác động, trở nên quá bẽn lẽn, không phản ứng với người lớn như bình thường, hoặc bám chặt vào bố mẹ nhiều hơn hầu hết trẻ cùng lứa tuổi.",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Quan hệ bất thường ở mức độ trung bình nhẹ: Trẻ thường thể hiện sự lưỡng lự rõ ràng hơn khi tương tác với người lớn. Trẻ có thể né tránh ánh mắt hoặc trở nên quá bẽn lẽn khi được yêu cầu tham gia các hoạt động, nhưng không phải lúc nào cũng hoàn toàn tách biệt. Việc khởi đầu mối quan hệ với người lớn đòi hỏi một ít nỗ lực, nhưng vẫn có thể duy trì sự tương tác khi có động lực từ môi trường.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Quan hệ không bình thường ở mức độ trung bình: Thỉnh thoảng trẻ thể hiện sự tách biệt (dường như không nhận thức được người lớn). Để thu hút sự chú ý của trẻ, đôi khi cần có những nỗ lực liên tục và mạnh mẽ. Quan hệ tối thiểu được khởi đầu bởi trẻ.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Quan hệ bất thường ở mức độ trung bình nặng: Trẻ thường xuyên có biểu hiện tách biệt với người lớn và cần nhiều nỗ lực hơn để thu hút sự chú ý của trẻ. Trẻ thỉnh thoảng có thể phản ứng khi được yêu cầu tương tác, nhưng các phản ứng này thường chậm và không bền vững. Việc duy trì mối quan hệ với người lớn cần có sự tác động liên tục và trẻ gần như không chủ động trong việc giao tiếp.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 1,
                            OptionText = "Quan hệ không bình thường ở mức độ nặng: Trẻ luôn tách biệt hoặc không nhận thức được những việc người lớn đang làm. Trẻ hầu như không bao giờ đáp ứng hoặc bắt đầu mối quan hệ với người lớn. Chỉ có thể những nỗ lực liên tục nhất mới nhận được sự chú ý của trẻ.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 2,
                            OptionText = "Bắt chước đúng: Trẻ có thể bắt chước âm thanh, từ và các hành động phù hợp với khả năng của chúng.",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption 
                        { 
                            QuestionId = 2, 
                            OptionText = "Bắt chước có chút bất thường nhẹ: Trẻ có thể bắt chước các hành động hoặc âm thanh, nhưng cần có sự khích lệ rõ rệt hơn và đôi khi có sự chần chừ trong việc bắt chước.", 
                            Point = 3.5, 
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 2, 
                            OptionText = "Bắt chước không bình thường ở mức độ nhẹ: Trẻ thường bắt chước các hành vị đơn giản như là vỗ tay hoặc các từ đơn , đôi khi trẻ chỉ bắt chước sau khi có sự khích lệ hoặc sau đôi chút trì hoãn.", 
                            Point = 3, 
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 2, 
                            OptionText = "Bắt chước bất thường ở mức độ trung bình nhẹ: Trẻ có xu hướng bắt chước các hành vi đơn giản sau khi có sự trợ giúp hoặc khích lệ mạnh mẽ, với thời gian phản hồi kéo dài hơn so với mức bình thường.", 
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 2, 
                            OptionText = "Bắt chước không bình thường ở mức độ trung bình: Trẻ chỉ bắt chước một lúc nào đó và đòi hỏi cần có sự kiên trì và giúp đỡ của người lớn; thường xuyên chỉ bắt chước sau đôi chút trì hoãn.", 
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption {
                            QuestionId = 2, 
                            OptionText = "Bắt chước bất thường ở mức độ trung bình nặng: Trẻ chỉ bắt chước trong một số tình huống nhất định và cần nhiều sự hỗ trợ. Trẻ thể hiện sự chần chừ rõ rệt và không phản ứng ngay cả khi được khích lệ.", 
                            Point = 1.5, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 2, 
                            OptionText = "Bắt chước không bình thường ở mức độ nặng: Trẻ rất ít khi hoặc không bao giờ bắt chước âm thanh, từ hoặc các hành động ngay cả khi có sự khích lệ và giúp đỡ của người lớn.", 
                            Point = 1, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now  
                        },
                        new AssessmentOption { 
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm phù hợp với tuổi và tình huống: Trẻ thể hiện đúng với thể loại và mực độ tình cảm thông qua nét mặt, điệu bộ và thái độ.", 
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption {
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm có chút bất thường: Trẻ thường thể hiện tình cảm phù hợp, nhưng đôi khi có những phản ứng nhẹ chưa phù hợp với tình huống hoặc đối tượng.", 
                            Point = 3.5, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm không bình thường ở mức độ nhẹ: Trẻ đôi khi thể hiện tình cảm không bình thường với thể loại và mức độ tình cảm. Phản ứng đôi khi không liên quan đến đôi tượng hoặc sự việc xung quanh.", 
                            Point = 3, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm bất thường ở mức độ trung bình nhẹ: Trẻ thể hiện tình cảm không đồng nhất, có những phản ứng chưa phù hợp với tình huống và mức độ tình cảm, thường cần sự tác động để thay đổi phản ứng.", 
                            Point = 2.5, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm không bình thường ở mức độ trung bình: Phản ứng của trẻ có thể khá hạn chế hoặc quá mức hoặc không liên quan đến tình huống; có thể là nhăn nhó, cười lớn, hoặc trở nên máy móc cho dù không có sự xuất hiện đối tượng hoặc sự việc gây xúc động.", 
                            Point = 2, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm bất thường ở mức độ trung bình nặng: Trẻ thường thể hiện tình cảm không liên quan hoặc quá mức với tình huống. Biểu cảm cảm xúc của trẻ thường cứng nhắc và thiếu tính linh hoạt.", 
                            Point = 1.5, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 3, 
                            OptionText = "Thể hiện tình cảm không bình thường ở mức độ nặng: Phản ứng của trẻ rất ít khi phù hợp với tình huống; khi trẻ đang ở một tâm trạng nào đó thì rất khó có thể thay đổi sang tâm trạng khác. Ngược lại, trẻ có thể thể hiện rất nhiều tâm trạng khác nhau khi không có sự thay đổi nào cả.",
                            Point = 1, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption
                        {
                            QuestionId = 4,
                            OptionText = "Thể hiện các động tác phù hợp với tuổi: Trẻ chuyển động thoải mái, nhanh nhẹn, và phối hợp các động tác như những trẻ khác cùng lứa tuổi.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác phù hợp với tuổi: Trẻ chuyển động thoải mái, nhanh nhẹn, và phối hợp các động tác như những trẻ khác cùng lứa tuổi.", 
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác có chút bất thường: Trẻ có thể thể hiện một số cử động không bình thường như vụng về hoặc động tác lặp lại, nhưng các biểu hiện này không thường xuyên và khá nhẹ.", 
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption {
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác không bình thường ở mức độ nhẹ: Trẻ đôi khi thể hiện một số biểu hiện khác thường nhỏ, ví dụ như vụng về, động tác diễn đi diễn lại, phối hợp giữa các động tác kém, hoặc ít xuất hiện những cử động khác thường.", 
                            Point = 3, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác bất thường ở mức độ trung bình nhẹ: Trẻ có các hành vi khác thường rõ ràng hơn, ví dụ như lặp lại các cử động ngón tay, hoặc có những chuyển động bất thường ở các bộ phận cơ thể trong một khoảng thời gian.", 
                            Point = 2.5, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác không bình thường ở mức độ trung bình: Những hành vi rõ ràng khác lạ hoặc không bình thường của trẻ ở tuổi này có thể bao gồm những cử động ngón tay, ngón tay hoặc dáng điệu cơ thể khác thường, nhìn chằm chằm hoặc hoặc một chỗ nào đó trên cơ thể, tự mình bị kích động, đu đưa, ngón tay lắc lư hoặc đi bằng ngón chân.", 
                            Point = 2, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác bất thường ở mức độ trung bình nặng: Trẻ thường thể hiện các động tác không bình thường liên tục và rõ ràng, chẳng hạn như việc nhìn chằm chằm vào một chỗ hoặc có những cử động lặp đi lặp lại khó kiểm soát.", 
                            Point = 1.5, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption {
                            QuestionId = 4, 
                            OptionText = "Thể hiện các động tác không bình thường ở mức độ nặng: Sự xuất hiện các biểu hiện nói trên một cách liên tục và mãnh liệt là biểu hiện của việc thể hiện các động tác không phù hợp ở mức độ nặng. Các biểu hiện này có thể liên tục cho dù có những cố gắng để hạn chế hoặc hướng trẻ vào các hoạt động khác.", 
                            Point = 1, 
                            CreatedDate = DateTime.Now, 
                            UpdatedDate = DateTime.Now 
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Sử dụng phù hợp, và ham thích chơi với đồ chơi và các đồ vật khác: Trẻ thể hiện sự ham thích đồ chơi và các đồ vật khác phù hợp với khả năng và sử dụng những đồ chơi này đúng cách.", 
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Sử dụng phù hợp, và ham thích chơi với đồ chơi và các đồ vật khác: Trẻ thể hiện sự ham thích đồ chơi và các đồ vật khác phù hợp với khả năng và sử dụng những đồ chơi này đúng cách.", 
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Không bình thường ở mức độ nhẹ trong những ham mê hoặc trong việc sử dung đồ chơi và các đồ vật khác: Trẻ có thể thể hiện sự ham muốn không bình thường vào đồ chơi hoặc việc sử dụng những đồ chơi này không phù hợp với tính cách trẻ em (ví dụ như mút đồ chơi).", 
                            Point = 3.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Không bình thường ở mức độ nhẹ trong những ham mê hoặc trong việc sử dung đồ chơi và các đồ vật khác: Trẻ có thể thể hiện sự ham muốn không bình thường vào đồ chơi hoặc việc sử dụng những đồ chơi này không phù hợp với tính cách trẻ em (ví dụ như mút đồ chơi).", Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Không bình thường ở mức độ trung bình trong những ham mê hoặc trong việc sử dung đồ chơi và các đồ vật khác: Trẻ có thể ít ham thích đến đồ chơi hoặc các đồ vật khác hoặc có thể chiếm giữ những đồ chơi và các đồ vật khác một cách khác thường. Trẻ có thể tập trung vào một bộ phận không nổi bật của đồ chơi, bị thu hút vào phần không phản xạ ánh sáng, liên tục di chuyển một vài bộ phận của đồ vật hoặc chỉ chơi riêng với một đồ vật.", 
                            Point = 2.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Không bình thường ở mức độ trung bình trong những ham mê hoặc trong việc sử dung đồ chơi và các đồ vật khác: Trẻ có thể ít ham thích đến đồ chơi hoặc các đồ vật khác hoặc có thể chiếm giữ những đồ chơi và các đồ vật khác một cách khác thường. Trẻ có thể tập trung vào một bộ phận không nổi bật của đồ chơi, bị thu hút vào phần không phản xạ ánh sáng, liên tục di chuyển một vài bộ phận của đồ vật hoặc chỉ chơi riêng với một đồ vật.", 
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 5, 
                            OptionText = "Không bình thường ở mức độ nặng trong những ham mê hoặc trong việc sử dung đồ chơi và các đồ vật khác: Trẻ có thể có những hành động như trên với mức độ thường xuyên và cường độ lớn hơn. Rất khó có thể bị đánh lạc hướng/lãng quên khi đã có những hành động như trên.", 
                            Point = 1.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 6, 
                            OptionText = "Thể thiện sự phản ứng bằng thính giác phù hợp với tuổi: Các biểu hiện thính giác của trẻ bình thường và phù hợp với tuổi. Thính giác được dùng cùng với các giác quan khác.", 
                            Point = 4.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 6, 
                            OptionText = "Thể thiện sự phản ứng bằng thính giác phù hợp với tuổi: Các biểu hiện thính giác của trẻ bình thường và phù hợp với tuổi. Thính giác được dùng cùng với các giác quan khác.", 
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 6, 
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ nhẹ: Trẻ đôi khi không phản ứng, hoặc hơn phản ứng với một số loại tiếng động. Phản ứng với âm thanh có thể chậm, tiếng động cần được lặp lại để gây được sự chú ý của trẻ. Trẻ có thể bị phân tán bởi âm thanh bên ngoài.", 
                            Point = 3.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 6, 
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ nhẹ: Trẻ đôi khi không phản ứng, hoặc hơn phản ứng với một số loại tiếng động. Phản ứng với âm thanh có thể chậm, tiếng động cần được lặp lại để gây được sự chú ý của trẻ. Trẻ có thể bị phân tán bởi âm thanh bên ngoài.", 
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption { 
                            QuestionId = 6, 
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ trung bình: Phản ứng của trẻ với âm thanh có nhiều dạng; luôn bỏ qua tiếng động sau những lần nghe đầu tiên; có thể giật mình hoặc che tai khi nghe thấy những âm thanh thường ngày.", 
                            Point = 2.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 6,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ trung bình: Phản ứng của trẻ với âm thanh có nhiều dạng; luôn bỏ qua tiếng động sau những lần nghe đầu tiên; có thể giật mình hoặc che tai khi nghe thấy những âm thanh thường ngày.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 6,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ nặng: Trẻ quá phản ứng hoặc phản ứng dưới mức bình thường với âm thanh ở một mức độ khác thường cho dù đó là lại âm thanh nào.",
                            Point = 1.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng bằng thị giác phù hợp với tuổi: Trẻ thể hiện sự phản ứng bằng thị giác bình thường và phù hợp với lứa tuổi. Thị giác được phối hợp với các giác quan khác khi khám phá ra đồ vật mới.",
                            Point = 4.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng bằng thị giác phù hợp với tuổi: Trẻ thể hiện sự phản ứng bằng thị giác bình thường và phù hợp với lứa tuổi. Thị giác được phối hợp với các giác quan khác khi khám phá ra đồ vật mới.",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng bằng thị giác không bình thường ở mức độ nhẹ: Đôi khi trẻ phải được nhắc lại bằng việc nhìn lại đồ vật. Trẻ có thể thích nhìn vào gương hoặc ánh đèn hơn chúng bạn, có thể nhìn chằm chằm vảo khoảng trống, hoặc tránh nhìn vào mắt người khác.",
                            Point = 3.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng bằng thị giác không bình thường ở mức độ nhẹ: Đôi khi trẻ phải được nhắc lại bằng việc nhìn lại đồ vật. Trẻ có thể thích nhìn vào gương hoặc ánh đèn hơn chúng bạn, có thể nhìn chằm chằm vảo khoảng trống, hoặc tránh nhìn vào mắt người khác.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng thị giác không bình thường ở mức độ trung bình: Trẻ thường xuyên phải được nhắc nhìn vào những gì trẻ đang làm. Trẻ có thể nhìn chằm chằm vào khoảng trống, tránh không nhìn vào mắt người khác, nhìn vào đồ vật từ một góc độ bất thường, hoặc giữ đồ vật rất gần với mắt.",
                            Point = 2.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng thị giác không bình thường ở mức độ trung bình: Trẻ thường xuyên phải được nhắc nhìn vào những gì trẻ đang làm. Trẻ có thể nhìn chằm chằm vào khoảng trống, tránh không nhìn vào mắt người khác, nhìn vào đồ vật từ một góc độ bất thường, hoặc giữ đồ vật rất gần với mắt.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 7,
                            OptionText = "Thể hiện sự phản ứng thị giác không bình thường ở góc độ nặng: Trẻ luôn tránh không nhìn vào mắt người khác, hoặc các đồ vật cụ thể nào đó, và có thể thể hiện các hình thức rất đặc biệt của các cách nhìn nói trên.",
                            Point = 1.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác phù hợp với tuổi: Các biểu hiện thính giác của trẻ bình thường và phù hợp với tuổi. Thính giác được dùng cùng với các giác quan khác.",
                            Point = 4.0,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác phù hợp với tuổi: Các biểu hiện thính giác của trẻ bình thường và phù hợp với tuổi. Thính giác được dùng cùng với các giác quan khác.",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ nhẹ: Trẻ đôi khi không đáp ứng, hoặc quá phản ứng đối với một số loại âm thanh nhất định. Phản ứng đối với âm thanh có thể chậm, và tiếng động cần được lặp lại để gây được sự chú ý của trẻ. Trẻ có thể bị phân tán bởi âm thanh bên ngoài.",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ nhẹ: Trẻ đôi khi không đáp ứng, hoặc quá phản ứng đối với một số loại âm thanh nhất định. Phản ứng đối với âm thanh có thể chậm, và tiếng động cần được lặp lại để gây được sự chú ý của trẻ. Trẻ có thể bị phân tán bởi âm thanh bên ngoài.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ trung bình: Phản ứng của trẻ với âm thanh hay biến đổi; bỏ qua âm thanh sau những lần nghe đầu tiên; có thể giật mình hoặc che tai khi nghe thấy những âm thanh thường ngày.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ trung bình: Phản ứng của trẻ với âm thanh hay biến đổi; bỏ qua âm thanh sau những lần nghe đầu tiên; có thể giật mình hoặc che tai khi nghe thấy những âm thanh thường ngày.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 8,
                            OptionText = "Thể hiện sự phản ứng bằng thính giác không bình thường ở mức độ nặng: Trẻ quá phản ứng hoặc phản ứng dưới mức bình thường với âm thanh ở một mức độ khác thường cho dù đó là âm thanh nào.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác bình thường: Trẻ khám phá đồ vật mới với một thái độ phù hợp với lứa tuổi, thông thường bằng xúc giác và thị giác.\n\nVị giác hoặc khứu giác có thể được sủ dụng khi cân thiết. Khi phản ứng với những đau đớn nhỏ, thường ngày thì trẻ thể hiện sự khó chịu nhưng không không quá phản ứng.",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác bình thường: Trẻ khám phá đồ vật mới với một thái độ phù hợp với lứa tuổi, thông thường bằng xúc giác và thị giác.\n\nVị giác hoặc khứu giác có thể được sủ dụng khi cân thiết. Khi phản ứng với những đau đớn nhỏ, thường ngày thì trẻ thể hiện sự khó chịu nhưng không không quá phản ứng.",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác không bình thường ở mức độ nhẹ: Trẻ  có thể khăng khăng đút đò vật vào miệng; có thể ngửi hoặc nếm các đồ vật không được; có thể không để ý hoặc quá phản ứng với những đau đớn nhẹ mà những trẻ bình thường có thể thấy khó chịu.",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác không bình thường ở mức độ nhẹ: Trẻ  có thể khăng khăng đút đò vật vào miệng; có thể ngửi hoặc nếm các đồ vật không được; có thể không để ý hoặc quá phản ứng với những đau đớn nhẹ mà những trẻ bình thường có thể thấy khó chịu.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác không bình thường ở mức độ trung bình: Trẻ có thể bị khó chịu ở mức độ trung bình khi sờ, ngửi hoặc nếm đồ vật hoặc người. Trẻ có thể phản ứng quá mức hoặc dưới mức.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác không bình thường ở mức độ trung bình: Trẻ có thể bị khó chịu ở mức độ trung bình khi sờ, ngửi hoặc nếm đồ vật hoặc người. Trẻ có thể phản ứng quá mức hoặc dưới mức.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 9,
                            OptionText = "Việc sử dụng, và sự phản ứng bằng các giác quan vị, khứu và xúc giác không bình thường ở mức độ nặng: Trẻ bị khó chịu với việc ngửi, nếm, hoặc sờ vào đồ vật về cảm giác hơn là về khám phá thông thường hoặc sử dụng đồ vật. Trẻ có thể hoàn toàn bỏ qua cảm giác đau đớn hoặc phản ứng dữ dội với khó chịu nhỏ.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi và hồi hộp bình thường: Hành vi của trẻ phù hợp với tuổi và tình huống",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi và hồi hộp bình thường: Hành vi của trẻ phù hợp với tuổi và tình huống",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi và hồi hộp không bình thường ở mức độ nhẹ: Trẻ đôi khi thể hiện sự quá nhiều hoặc quá ít sự sợ hãi hoặc hồi hộp khi so sánh với những trẻ bình thường trong tình huống tương tự.",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi và hồi hộp không bình thường ở mức độ nhẹ: Trẻ đôi khi thể hiện sự quá nhiều hoặc quá ít sự sợ hãi hoặc hồi hộp khi so sánh với những trẻ bình thường trong tình huống tương tự.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi và hồi hộp không bình thường ở mức độ trung bình: Trẻ đặc biệt thể hiện sự sợ hãi hoặc hơi nhiều hoặc hơi ít ngay cả so với trẻ ít tháng hơn trong tình huống tương tự.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi và hồi hộp không bình thường ở mức độ trung bình: Trẻ đặc biệt thể hiện sự sợ hãi hoặc hơi nhiều hoặc hơi ít ngay cả so với trẻ ít tháng hơn trong tình huống tương tự.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 10,
                            OptionText = "Thể hiện sự sợ hãi hoặc hồi hộp không bình thường ở mức độ nặng: Luôn sợ hãi ngay cả đã gặp lại những tình huống hoặc đồ vật vô hại. Rất khó làm cho trẻ bình tĩnh hoặc thoải mái. Ngược lại trẻ không thể hiện có được sự để ý cần thiết đối với nguy hại mà trẻ cùng tuổi có thể tránh được.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời bình thường phù hợp với tuổi và tình huống",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời bình thường phù hợp với tuổi và tình huống",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời không bình thường ở mức độ nhẹ: Nhìn chung, nói chậm. Hầu hết lời nói có nghĩa; tuy nhiên có thể xuất hiện sự lặp lại máy móc hoặc phát âm bị đảo lộn.",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời không bình thường ở mức độ nhẹ: Nhìn chung, nói chậm. Hầu hết lời nói có nghĩa; tuy nhiên có thể xuất hiện sự lặp lại máy móc hoặc phát âm bị đảo lộn.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời không bình thường ở mức độ trung bình: Có thể không nói. Khi nói, giao tiếp bằng lời có thể lẫn lộn giữa những lời nói có nghĩa và những lời nói khác biệt như là không rõ nghĩa, lặp lại máy móc, hoặc phát âm đảo lộn.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời không bình thường ở mức độ trung bình: Có thể không nói. Khi nói, giao tiếp bằng lời có thể lẫn lộn giữa những lời nói có nghĩa và những lời nói khác biệt như là không rõ nghĩa, lặp lại máy móc, hoặc phát âm đảo lộn.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 11,
                            OptionText = "Giao tiếp bằng lời không bình thường ở mức độ nặng: Không có những lời nói có nghĩa. Trẻ có thể kêu thét như trẻ mới sinh, kêu những tiếng kêu kỳ lạ hoặc như tiếng kêu của động vật.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 12,
                            OptionText = "Giao tiếp không lời phù hợp với tuổi và tinh huống",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 12,
                            OptionText = "Giao tiếp không lời phù hợp với tuổi và tinh huống",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 12,
                            OptionText = "Giao tiếp không lời không bình thường ở mức độ nhẹ: Non nớt trong việc dùng các đối thoại không bằng lời; có thể chỉ ở mức độ không rõ ràng, hoặc với tay tới cái mà trẻ muốn.",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                         new AssessmentOption
                         {
                             QuestionId = 12,
                             OptionText = "Giao tiếp không lời không bình thường ở mức độ nhẹ: Non nớt trong việc dùng các đối thoại không bằng lời; có thể chỉ ở mức độ không rõ ràng, hoặc với tay tới cái mà trẻ muốn, trong những tình huống mà trẻ cung lứa tuổi có thể chỉ hoặc ra hiệu chính xác hơn nhằm chỉ ra cái mà trẻ muốn.",
                             Point = 2.5,
                             CreatedDate = DateTime.Now,
                             UpdatedDate = DateTime.Now
                         },
                        new AssessmentOption
                        {
                            QuestionId = 12,
                            OptionText = "Giao tiếp không lời không bình thường ở mức độ trung bình: Thông thường trẻ không thể diễn đạt không bằng lời cái trẻ cần hoặc mong muốn, và không thể hiểu được giao tiếp không lời của những người khác.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 12,
                            OptionText = "Giao tiếp không lời không bình thường ở mức độ trung bình: Thông thường trẻ không thể diễn đạt không bằng lời cái trẻ cần hoặc mong muốn, và không thể hiểu được giao tiếp không lời của những người khác.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 12,
                            OptionText = "Giao tiếp không lời không bình thường ở mức độ nặng: Trẻ chỉ có thể thể hiện những cử chỉ kỳ quái hoặc khác thường mà không rõ nghĩa và thể hiện sự không nhận thức được các ý nghĩa liên quan tới cử chỉ hoặc biển hiện nét mặt của người khác.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động bình thường so với tuổi và tình huống: Trẻ không biểu hiện nhanh hơn hay chậm hơn trẻ cùng lứa tuổi trong tình huống tương tự.",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        }, 
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động bình thường so với tuổi và tình huống: Trẻ không biểu hiện nhanh hơn hay chậm hơn trẻ cùng lứa tuổi trong tình huống tương tự.",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động không bình thường ở mức độ nhẹ: Trẻ đôi khi có thể luôn hiếu động hoặc có dấu hiệu lười và chậm chuyển động. Mức độ hoạt động của trẻ ảnh hưởng rất nhỏ đến kết quả hoạt động của trẻ",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động không bình thường ở mức độ nhẹ: Trẻ đôi khi có thể luôn hiếu động hoặc có dấu hiệu lười và chậm chuyển động. Mức độ hoạt động của trẻ ảnh hưởng rất nhỏ đến kết quả hoạt động của trẻ",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động không bình thường ở mức độ trung bình: Trẻ có thể rất hiếu động và khó có thể kèm chế trẻ. Trẻ có thể hoạt động không biết mệt mỏi và có thể muốn không ngủ về đêm. Ngược lại, trẻ có thể khá mê mệt và cần phải thúc giục rất nhiều mới làm cho trẻ vận động.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động không bình thường ở mức độ trung bình: Trẻ có thể rất hiếu động và khó có thể kèm chế trẻ. Trẻ có thể hoạt động không biết mệt mỏi và có thể muốn không ngủ về đêm. Ngược lại, trẻ có thể khá mê mệt và cần phải thúc giục rất nhiều mới làm cho trẻ vận động.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 13,
                            OptionText = "Mức độ hoạt động không bình thường ở mức độ nặng: Trẻ thể hiện hoặc quá hiếu động hoặc quá thụ động và có thể chuyển từ trạng thái quá này sang trạng thái quá kia.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Mức độ hiểu biết bình thường và có sự nhất quán phù hợp trên các lĩnh vực: Trẻ có mức độ hiểu biết như những đứa trẻ bình thường và không có kỹ năng hiểu biết khác thường hoặc có vấn đề nào.",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Mức độ hiểu biết bình thường và có sự nhất quán phù hợp trên các lĩnh vực: Trẻ có mức độ hiểu biết như những đứa trẻ bình thường và không có kỹ năng hiểu biết khác thường hoặc có vấn đề nào.",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Trí thông minh không bình thường ở mức độ nhẹ: Trẻ không thông minh như những trẻ bình thường cùng lứa tuổi; kỹ năng hơi chậm trên các lĩnh vực.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Trí thông minh không bình thường ở mức độ nhẹ: Trẻ không thông minh như những trẻ bình thường cùng lứa tuổi; kỹ năng hơi chậm trên các lĩnh vực.",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Trí thông minh không bình thường ở mức độ trung bình: Nói chung, trẻ không thông minh như những trẻ bình thường cùng tuổi; tuy nhiên, trẻ có thể có chức năng gần như bình thường đối với một số lĩnh vực có liên quan đến vận động trí não.",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Trí thông minh không bình thường ở mức độ trung bình: Nói chung, trẻ không thông minh như những trẻ bình thường cùng tuổi; tuy nhiên, trẻ có thể có chức năng gần như bình thường đối với một số lĩnh vực có liên quan đến vận động trí não.",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 14,
                            OptionText = "Trí thông minh không bình thường ở mức độ nặng: Trong khi trẻ thường không thông minh như những trẻ khác cung lứa tuổi, trẻ có thể làm tốt hơn trẻ bình thường cùng tuổi trong một hoặc nhiều lĩnh vực.",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Không tự kỉ: Đứa trẻ không biểu lộ triệu chứng tự kỉ nào",
                            Point = 4,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        }, new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Không tự kỉ: Đứa trẻ không biểu lộ triệu chứng tự kỉ nào",
                            Point = 3.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Tự kỉ nhẹ: Đứa trẻ biểu lộ một vài triệu chứng hoặc chỉ tự kỉ mức độ nhẹ",
                            Point = 3,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Tự kỉ nhẹ: Đứa trẻ biểu lộ một vài triệu chứng hoặc chỉ tự kỉ mức độ nhẹ",
                            Point = 2.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Tự kỉ mức độ vừa: Trẻ biểu lộ một số triệu chứng hay tự kỉ ở mức độ tương đối",
                            Point = 2,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Tự kỉ mức độ vừa: Trẻ biểu lộ một số triệu chứng hay tự kỉ ở mức độ tương đối",
                            Point = 1.5,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new AssessmentOption
                        {
                            QuestionId = 15,
                            OptionText = "Tự kỉ nặng: Trẻ bộc lộ nhiều triệu chứng hay tự kỉ ở mức độ nặng",
                            Point = 1,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        }


                    );
                }


                await SaveChangesAsync();
            }
        }

    }
}
