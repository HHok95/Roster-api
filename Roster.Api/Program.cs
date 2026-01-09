using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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

const string CorsPolicyName = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// EF Core Health Check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");



var app = builder.Build();

// testing db
// app.MapGet("/api/db/ping", async (AppDbContext db) =>
// {
//     db.PingRows.Add(new PingRow { Message = "ok" });
//     await db.SaveChangesAsync();
//     return Results.Ok(await db.PingRows.OrderByDescending(x => x.Id).Take(5).ToListAsync());
// });


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Cors
app.UseCors(CorsPolicyName);

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