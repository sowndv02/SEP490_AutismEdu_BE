using backend_api;
using backend_api.Authorize;
using backend_api.Authorize.Requirements;
using backend_api.Data;
using backend_api.Mapper;
using backend_api.Models;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services;
using backend_api.Services.IServices;
using backend_api.Swagger;
using backend_api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins(SD.URL_FE)
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.AddResponseCaching();

// Add DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IBlobStorageRepository, BlobStorageRepository>();


// Add DI requirement authorization handler
builder.Services.AddScoped<INumberOfDaysForAccount, NumberOfDaysForAccount>();
builder.Services.AddScoped<IAuthorizationHandler, AdminWithOver1000DaysHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FirstNameAuthHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AssignRoleHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AssignClaimHandler>();



// Add DI SendMail Service
var mailsettings = builder.Configuration.GetSection("MailSettings");
builder.Services.Configure<MailSettings>(mailsettings);
builder.Services.AddTransient<IEmailSender, SendMailService>();
builder.Services.AddSingleton<DateTimeEncryption>();

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
});


// Config Authorization with policy

builder.Services.AddAuthorization(option =>
{

    // Authorization with policy using role requirement
    option.AddPolicy("Admin", policy => policy.RequireRole(SD.Admin));
    // Authorization with policy using role requirement using condition and
    option.AddPolicy("AdminAndUser", policy => policy.RequireRole(SD.Admin).RequireRole(SD.User));
    // Authorization with policy using single claim requirement
    option.AddPolicy("AdminRole_CreateClaim", policy => policy.RequireRole(SD.Admin).RequireClaim("Create", "True"));
    // Authorization with policy using multiple claim requirement
    option.AddPolicy("AdminRole_CreateEditDeleteClaim", policy => policy.RequireRole(SD.Admin)
                                            .RequireClaim("Create", "True")
                                            .RequireClaim("Delete", "True")
                                            .RequireClaim("Edit", "True")
                                            );


    option.AddPolicy("AdminRole_CreateEditDeleteClaim_ORSuperAdminRole", policy => policy.RequireAssertion(context => AdminRole_CreateEditDeleteClaim_ORSuperAdminRole(context)));

    option.AddPolicy("OnlySuperAdminChecker", p => p.Requirements.Add(new OnlySuperAdminChecker()));
    // requirement is calculate date
    option.AddPolicy("AdminWithMoreThan1000Days", p => p.Requirements.Add(new AdminWithMoreThan1000DaysRequirement(1000)));

    // requirement is claim 
    option.AddPolicy("FirstNameAuth", p => p.Requirements.Add(new FirstNameAuthRequirement("test")));

    // Define policy for checking assign-role
    option.AddPolicy("AssignRolePolicy", policy =>
        policy.Requirements.Add(new AssignRoleRequirement("Assign", "Role")));

    // Define policy for checking assign-claim
    option.AddPolicy("AssignClaimPolicy", policy =>
        policy.Requirements.Add(new AssignClaimRequirement("Assign", "Claim")));

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


app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowSpecificOrigin");

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


bool AdminRole_CreateEditDeleteClaim_ORSuperAdminRole(AuthorizationHandlerContext context)
{
    return (context.User.IsInRole(SD.Admin) && context.User.HasClaim(c => c.Type == "Create" && c.Value == "True")
        && context.User.HasClaim(c => c.Type == "Edit" && c.Value == "True")
        && context.User.HasClaim(c => c.Type == "Delete" && c.Value == "True")
    ) || context.User.IsInRole(SD.SuperAdmin);
}

public partial class Program { }