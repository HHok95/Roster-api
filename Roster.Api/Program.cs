using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Roster.Api.Data;
using Roster.Api.Models;
using Roster.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Db
builder.Services.AddDbContext<AppDbContext>(opt =>
    // opt.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")));
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Bind JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Identity (now using JWT, not cookies - but keep Identity for UserManager/RoleManager)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
    throw new InvalidOperationException("JWT settings not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { message = "Forbidden" });
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<RosterService>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Stricter limit for auth endpoints: 5 requests per minute per IP
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// CORS - environment-configurable
const string CorsPolicyName = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string>()
    ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "X-Requested-With")
            .AllowCredentials();
    });
});

// EF Core Health Check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");



var app = builder.Build();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

    // Only add CSP for non-Swagger paths
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");
    }

    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // HSTS for production
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cors
app.UseCors(CorsPolicyName);

// Rate limiting
app.UseRateLimiter();

// Auth middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health");

await SeedAuthAsync(app);

app.Run();

static async Task SeedAuthAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await db.Database.MigrateAsync();

    // Seed store
    var store = await db.Stores.FirstOrDefaultAsync(s => s.Code == "1001");
    if (store is null)
    {
        store = new Store { Code = "1001", Name = "Demo Store 1001" };
        db.Stores.Add(store);
        await db.SaveChangesAsync();
    }

    // Seed role
    if (!await roleMgr.RoleExistsAsync("Manager"))
        await roleMgr.CreateAsync(new IdentityRole("Manager"));

    // Seed manager user with Real storeId
    var user = await userMgr.FindByNameAsync("manager");
    if (user is null)
    {
        user = new ApplicationUser
        {
            UserName = "manager",
            Email = "manager@demo.local",
            StoreId = store.Id
        };

        var result = await userMgr.CreateAsync(user, "Password123!");
        if (result.Succeeded)
        {
            await userMgr.AddToRoleAsync(user, "Manager");
        }
    }
    else
    {
        // if user already exists from earlier runs, ensure StoreId is correct
        if (user.StoreId != store.Id)
        {
            user.StoreId = store.Id;
            await userMgr.UpdateAsync(user);
        }

        // ensure role exists
        if (!await userMgr.IsInRoleAsync(user, "Manager"))
            await userMgr.AddToRoleAsync(user, "Manager");
    }

}