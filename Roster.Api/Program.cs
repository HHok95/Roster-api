using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roster.Api.Data;
using Roster.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Db
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Identity (cookie auth)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Cookie.Name = "roster.session";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;

    // Dev-friendly: allow HTTP if you ever hit HTTP locally
    opt.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    opt.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    opt.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // so frontend/Swagger can read it
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.HeaderName = "X-XSRF-TOKEN"; // header you must send on writes
});


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

// Auth middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("api/auth/csrf", (IAntiforgery antiforgery, HttpContext ctx) =>
{
    var tokens = antiforgery.GetAndStoreTokens(ctx);
    return Results.Ok(new { token = tokens.RequestToken });
});

// Map controllers
app.MapControllers();

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