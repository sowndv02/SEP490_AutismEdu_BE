using AutoMapper;
using AutismEduConnectSystem;
using AutismEduConnectSystem.Authorize;
using AutismEduConnectSystem.Authorize.Requirements;
using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Mapper;
using AutismEduConnectSystem.Messaging;
using AutismEduConnectSystem.Middlewares;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutismEduConnectSystem.Swagger;
using AutismEduConnectSystem.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Security.Claims;

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
                   .SetIsOriginAllowedToAllowWildcardSubdomains()
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
builder.Services.AddScoped<ISyllabusRepository, SyllabusRepository>();
builder.Services.AddScoped<ISyllabusExerciseRepository, SyllabusExerciseRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IAssessmentScoreRangeRepository, AssessmentScoreRangeRepository>();
builder.Services.AddScoped<IPackagePaymentRepository, PackagePaymentRepository>();
builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>(); 
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportMediaRepository, ReportMediaRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();


// Add DI requirement authorization handler
builder.Services.AddScoped<IAuthorizationHandler, RequiredClaimHandler>();



// Add DI other Service
var mailsettings = builder.Configuration.GetSection("MailSettings");
builder.Services.Configure<MailSettings>(mailsettings);
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddSingleton<DateTimeEncryption>();
builder.Services.AddSingleton<TokenEcryption>();
builder.Services.AddSingleton<FormatString>();
builder.Services.AddHostedService<DailyService>();
builder.Services.AddHostedService<GenerateScheduleTimeSlot>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<CheckAssignedExerciseForSchedule>();

// Config Message Queue
var rabbitMQSettings = builder.Configuration.GetSection("RabbitMQSettings");
builder.Services.AddHostedService<RabbitMQConsumer>();
builder.Services.AddScoped<IRabbitMQMessageSender, RabbitMQMessageSender>();

//builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();


// Add AutoMapper
var mapperConfig = new MapperConfiguration(mc =>
{
    mc.AddProfile(new MappingConfig());
});

IMapper mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

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
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user?.LockoutEnd > DateTime.Now)
                {
                    context.Fail("Tài khoản của bạn đang bị khóa.");
                }
            }
        }
    };
}).AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.Scope.Add("https://www.googleapis.com/auth/drive");
    options.SaveTokens = true;
});


//builder.Host.UseSerilog((context, configuration) =>
//{
//    configuration
//    .Enrich.FromLogContext()
//    .Enrich.WithMachineName()
//    .WriteTo.Console()
//    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(context.Configuration["ElasticConfiguration:Uri"]))
//    {
//        AutoRegisterTemplate = true,
//        IndexFormat = $"applogs-{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-logs-{DateTime.Now:yyyy-MM}",
//        NumberOfShards = 2,
//        NumberOfReplicas = 1
//    }).Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
//    .ReadFrom.Configuration(context.Configuration);
//});

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
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Autism Edu Connect System");
    //options.RoutePrefix = string.Empty;
});
app.UseCors("AllowSpecificOrigin");

//app.UseMiddleware<UnauthorizedRequestLoggingMiddleware>();
//app.UseMiddleware<RequestTimeLoggingMidleware>();
//app.UseMiddleware<ExceptionMiddleware>();
//app.UseMiddleware<RequestLoggingMiddleware>();


app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseResponseCaching();
app.MapHub<NotificationHub>("/hub/notifications");
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
app.UseMiddleware<PaymentCheckerMiddleware>();


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
            Console.WriteLine("Build successfully.");
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