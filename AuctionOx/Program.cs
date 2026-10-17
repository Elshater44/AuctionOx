using AuctionOx.Data;
using AuctionOx.Hubs;
using AuctionOx.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add Global Exception Handling (Problem Details)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AuctionOx.Infrastructure.GlobalExceptionHandler>();

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("BidLimiter", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.RejectionStatusCode = 429;
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register Services
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AuctionOx.Mappings.MappingProfile>());

builder.Services.AddScoped(typeof(AuctionOx.Repositories.Interfaces.IRepository<>), typeof(AuctionOx.Repositories.Implementations.Repository<>));
builder.Services.AddScoped<AuctionOx.Repositories.Interfaces.IUnitOfWork, AuctionOx.Repositories.Implementations.UnitOfWork>();

builder.Services.AddScoped<AuctionOx.Services.Interfaces.IAuthService, AuctionOx.Services.Implementations.AuthService>();
builder.Services.AddScoped<AuctionOx.Services.Interfaces.ICategoryService, AuctionOx.Services.Implementations.CategoryService>();
builder.Services.AddScoped<AuctionOx.Services.Interfaces.IAuctionService, AuctionOx.Services.Implementations.AuctionService>();
builder.Services.AddScoped<AuctionOx.Services.Interfaces.IBidService, AuctionOx.Services.Implementations.BidService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("Secret");

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT Secret is not configured. Please set 'JwtSettings:Secret' in user-secrets or environment variables.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var currentJwtSettings = builder.Configuration.GetSection("JwtSettings");
    var currentSecretKey = currentJwtSettings.GetValue<string>("Secret") ?? secretKey;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = currentJwtSettings.GetValue<string>("Issuer"),
        ValidAudience = currentJwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(currentSecretKey)),
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity) return;

            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail("Missing user identifier claim.");
                return;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // Auto-healing fallback: If user was re-seeded with a different ID, recover using email
                if (!string.IsNullOrEmpty(email))
                {
                    user = await userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        var idClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                        if (idClaim != null)
                        {
                            claimsIdentity.RemoveClaim(idClaim);
                        }
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                        return;
                    }
                }

                // If user doesn't exist by ID or Email, fail auth with 401
                context.Fail("User account no longer exists in database.");
            }
        }
    };
});

builder.Services.AddControllers();

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AuctionOx.Validators.CreateAuctionRequestValidator>();

// Register Background Services
builder.Services.AddHostedService<AuctionOx.BackgroundServices.AuctionCloserService>();

builder.Services.AddSignalR();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Simply paste your token here without any prefix.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler(); // Maps to problem details
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedRolesAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}
app.MapHub<BiddingHub>("/hubs/bidding");
app.Run();

public partial class Program { }
