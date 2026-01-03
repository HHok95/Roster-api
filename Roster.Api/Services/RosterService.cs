using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Roster.Api.Data;
using Roster.Api.Dtos.Rosters;
using Roster.Api.Models;

namespace Roster.Api.Services;

public sealed class RosterService
{
    private readonly AppDbContext _db;

    public RosterService(AppDbContext db) => _db = db;

    public async Task<(bool ok, int status, object body)> SaveRosterAsync(Guid storeId, DateOnly date, SaveRosterRequest req)
    {
        // Validate employee ids exist for this store
        var employeeIds = req.Shifts.Select(s => s.EmployeeId).Distinct().ToList();
        var validCount = await _db.Employees.CountAsync(e =>
            e.StoreId == storeId && e.IsActive && employeeIds.Contains(e.Id));

        if (validCount != employeeIds.Count)
            return (false, 400, new { message = "One or more EmployeeId values are invalid for this store." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var rosterDay = await _db.RosterDays
                .FirstOrDefaultAsync(r => r.StoreId == storeId && r.Date == date);

            if (rosterDay is null)
            {
                rosterDay = new RosterDay { StoreId = storeId, Date = date };
                _db.RosterDays.Add(rosterDay);
                await _db.SaveChangesAsync();
            }

            rosterDay.UpdatedAtUtc = DateTime.UtcNow;

            await _db.Shifts
                .Where(s => s.RosterDayId == rosterDay.Id)
                .ExecuteDeleteAsync();

            var newShifts = req.Shifts.Select(s => new Shift
            {
                RosterDayId = rosterDay.Id,
                EmployeeId = s.EmployeeId,
                ExternalShiftId = (s.ExternalShiftId ?? "").Trim(),
                StartSlot = s.StartSlot,
                EndSlot = s.EndSlot,
                BreaksJson = JsonSerializer.Serialize(s.Breaks ?? new()),
                RolesJson = JsonSerializer.Serialize(s.Roles ?? new())
            });

            _db.Shifts.AddRange(newShifts);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, 200, new { saved = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            return (false, 409, new { message = "Roster was modified by another request. Refresh and try again." });
        }
        catch (DbUpdateException ex) when (IsSqlUniqueConstraint(ex))
        {
            await tx.RollbackAsync();
            return (false, 400, new { message = "Duplicate shift detected (employee or externalShiftId). Check your payload." });
        }
    }

    private static bool IsSqlUniqueConstraint(DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && (sqlEx.Number is 2601 or 2627);
}
