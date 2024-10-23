using backend_api;
using backend_api.Authorize;
using backend_api.Authorize.Requirements;
using backend_api.Data;
using backend_api.Mapper;
using backend_api.Middlewares;
using backend_api.Models;
using backend_api.RabbitMQSender;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services;
using backend_api.Swagger;
using backend_api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add Loging
Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
    .WriteTo.File("log/seplogs.txt", rollingInterval: RollingInterval.Day).CreateLogger();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins(SD.URL_FE)
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024;
    options.UseCaseSensitivePaths = true;
});

// Add DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IBlobStorageRepository, BlobStorageRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<ICertificateMediaRepository, CertificateMediaRepository>();
builder.Services.AddScoped<IWorkExperienceRepository, WorkExperienceRepository>();
builder.Services.AddScoped<IChildInformationRepository, ChildInformationRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IAvailableTimeSlotRepository, AvailableTimeSlotRepository>();
builder.Services.AddScoped<ITutorRegistrationRequestRepository, TutorRegistrationRequestRepository>();
builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
builder.Services.AddScoped<ITutorRequestRepository, TutorRequestRepository>();
builder.Services.AddScoped<IAssessmentQuestionRepository, AssessmentQuestionRepository>();
builder.Services.AddScoped<IAssessmentOptionRepository, AssessmentOptionRepository>();
builder.Services.AddScoped<IAssessmentResultRepository, AssessmentResultRepository>();
builder.Services.AddScoped<IProgressReportRepository, ProgressReportRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<ITutorProfileUpdateRequestRepository, TutorProfileUpdateRequestRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IScheduleTimeSlotRepository, ScheduleTimeSlotRepository>();
builder.Services.AddScoped<IInitialAssessmentResultRepository, InitialAssessmentResultRepository>();
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IExerciseTypeRepository, ExerciseTypeRepository>();



// Add DI requirement authorization handler
builder.Services.AddScoped<IAuthorizationHandler, RequiredClaimHandler>();



// Add DI other Service
var mailsettings = builder.Configuration.GetSection("MailSettings");
builder.Services.Configure<MailSettings>(mailsettings);
builder.Services.AddTransient<IEmailSender, SendMailService>();
builder.Services.AddSingleton<DateTimeEncryption>();
builder.Services.AddSingleton<TokenEcryption>();
builder.Services.AddSingleton<FormatString>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHostedService<GenerateScheduleTimeSlot>();
builder.Services.AddHostedService<AutoRejectStudentProfile>();


// Config RabbitMQ
var rabbitMQSettings = builder.Configuration.GetSection("RabbitMQSettings");
builder.Services.AddHostedService<EmailConsumerService>();
builder.Services.AddScoped<IRabbitMQMessageSender, RabbitMQMessageSender>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingConfig));
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

// Config password, Lockout, AccessAttempts
builder.Services.Configure<IdentityOptions>(opt =>
{
    opt.Password.RequireDigit = false;
    opt.Password.RequireLowercase = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    opt.SignIn.RequireConfirmedEmail = true;
});



builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["ApiSettings:JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["ApiSettings:JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["ApiSettings:JWT:Secret"]))
    };
}).AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.Scope.Add("https://www.googleapis.com/auth/drive");
    options.SaveTokens = true;
});


builder.Host.UseSerilog();
// Config Authorization with policy

builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("RoleAdminPolicy", policy => policy.RequireRole(SD.ADMIN_ROLE));

    option.AddPolicy("AssignClaimPolicy", policy =>
        policy.Requirements.Add(new RequiredClaimRequirement("Assign", "Claim")));

    option.AddPolicy("AssignRolePolicy", policy =>
        policy.Requirements.Add(new RequiredClaimRequirement("Assign", "Role")));

    option.AddPolicy("AssignRolePolicy", policy =>
        policy.Requirements.Add(new RequiredClaimRequirement("Assign", "Role")));

    option.AddPolicy("ViewTutorPolicy", policy =>
        policy.Requirements.Add(new RequiredClaimRequirement("View", "Tutor")));

    option.AddPolicy("UpdateTutorPolicy", policy =>
        policy.Requirements.Add(new RequiredClaimRequirement("Update", "Tutor")));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);


builder.Services.AddControllers().AddNewtonsoftJson().AddXmlDataContractSerializerFormatters();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SEP490");
    //options.RoutePrefix = string.Empty;
});
app.UseCors("AllowSpecificOrigin");

app.UseMiddleware<UnauthorizedRequestLoggingMiddleware>();
app.UseMiddleware<RequestTimeLoggingMidleware>();
app.UseMiddleware<ExceptionMiddleware>();


app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseResponseCaching();

// Add cache for response
//app.Use(async (context, next) =>
//{
//    context.Response.GetTypedHeaders().CacheControl =
//    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
//    {
//        Public = true,
//        MaxAge = TimeSpan.FromSeconds(10)
//    };
//    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] = new string[] { "Accept-Encoding" };
//    await next();
//});

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
ApplyMigration();
app.Run();


void ApplyMigration()
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check for pending migrations and apply them if any
            var pendingMigrations = _db.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Count > 0)
            {
                Console.WriteLine($"Applying {pendingMigrations.Count} pending migrations...");
                _db.Database.Migrate();
                Console.WriteLine("Migrations applied successfully.");
            }

            // Seed the database if it’s empty
            _db.SeedDataIfEmptyAsync().GetAwaiter().GetResult();
        }
    }
    catch (Exception ex)
    {
        // Log the error and handle it appropriately
        Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
        throw; // Re-throw the exception or handle it as needed
    }
}

public partial class Program { }