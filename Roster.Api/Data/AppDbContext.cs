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
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RosterDay> RosterDays => Set<RosterDay>();
    public DbSet<Shift> Shifts => Set<Shift>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Store>(e =>
        {
            e.Property(s => s.Code)
                .IsRequired()
                .HasMaxLength(4)
                .IsUnicode(false);

            e.HasIndex(s => s.Code).IsUnique();

            // SQL Server CHECK constraint: exactly 4 digits
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Store_Code_4Digits",
                "LEN([Code]) = 4 AND [Code] NOT LIKE '%[^0-9]%'"
            ));
        });

        builder.Entity<Employee>(e =>
        {
            // DataAnnotations handle required + length for DisplayName,
            // but we still want the StoreId index for performance.
            e.HasIndex(x => x.StoreId);

            e.HasIndex(x => new { x.StoreId, x.IsActive });
        });

        builder.Entity<RosterDay>(e =>
        {
            e.Property(x => x.Date).HasColumnType("date");

            // one roster per store per date
            e.HasIndex(x => new { x.StoreId, x.Date }).IsUnique();

            e.HasMany(x => x.Shifts)
            .WithOne(x => x.RosterDay!)
            .HasForeignKey(x => x.RosterDayId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Shift>(e =>
        {
            e.Property(x => x.ExternalShiftId).IsRequired().HasMaxLength(80);

            // prevent duplicates from the same day
            e.HasIndex(x => new { x.RosterDayId, x.ExternalShiftId }).IsUnique();

            e.HasIndex(x => x.EmployeeId);
        });


    }

}

// For testing purpose
public class PingRow
{
    public int Id { get; set; }
    public string Message { get; set; } = "Hello";
}
