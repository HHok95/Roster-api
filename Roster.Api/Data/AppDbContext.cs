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
    public DbSet<Store> Stores => Set<Store>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Store>()
            .HasIndex(s => s.Code)
            .IsUnique();
    }

}

// For testing purpose
public class PingRow
{
    public int Id { get; set; }
    public string Message { get; set; } = "Hello";
}
