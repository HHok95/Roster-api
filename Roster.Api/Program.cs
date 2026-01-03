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

// Map controllers
app.MapControllers();

await SeedAuthAsync(app);

app.Run();

static async Task SeedAuthAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleMgr.RoleExistsAsync("Manager"))
        await roleMgr.CreateAsync(new IdentityRole("Manager"));

    var user = await userMgr.FindByNameAsync("manager");
    if (user is null)
    {
        user = new ApplicationUser
        {
            UserName = "manager",
            Email = "manager@demo.local",
            StoreId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        var result = await userMgr.CreateAsync(user, "Password123!");
        if (result.Succeeded)
        {
            await userMgr.AddToRoleAsync(user, "Manager");
        }
    }

}