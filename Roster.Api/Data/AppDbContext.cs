using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roster.Api.Models;

namespace Roster.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<PingRow> PingRows => Set<PingRow>();

}

// For testing purpose
public class PingRow
{
    public int Id { get; set; }
    public string Message { get; set; } = "Hello";
}
