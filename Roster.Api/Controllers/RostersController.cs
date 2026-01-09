using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Roster.Api.Data;
using Roster.Api.Dtos.Rosters;
using Roster.Api.Models;
using Roster.Api.Services;
using Roster.Api.Utils;

namespace Roster.Api.Controllers;

[ApiController]
[Route("api/rosters")]
[Authorize]
public class RostersController : ControllerBase
{
    private const int SlotsPerDay = 56;
    private const int SlotMinutes = 15;

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RosterService _rosterService;

    public RostersController(AppDbContext db, UserManager<ApplicationUser> users, RosterService rosterService)
    {
        _db = db;
        _users = users;
        _rosterService = rosterService;
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(string date)
    {
        if (!DateOnly.TryParse(date, out var d))
            return BadRequest(new { message = "Invalid date. Use. YYYY-MM-DD." });

        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var storeId = user.StoreId;
        var canEdit = User.IsInRole("Manager");

        var rosterDay = await _db.RosterDays
            .AsNoTracking()
            .Include(r => r.Shifts)
            .FirstOrDefaultAsync(r => r.StoreId == storeId && r.Date == d);

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.StoreId == storeId && e.IsActive)
            .OrderBy(e => e.DisplayName)
            .Select(e => new EmployeeMiniDto(e.Id, e.DisplayName))
            .ToListAsync();

        var shifts = (rosterDay?.Shifts ?? new List<Shift>())
            .OrderBy(s => s.StartSlot)
            .Select(s => new RosterShiftResponseDto
            (
                s.Id,
                s.ExternalShiftId,
                s.EmployeeId,
                s.StartSlot,
                s.EndSlot,
                JsonSerializer.Deserialize<List<BreakDto>>(s.BreaksJson) ?? new(),
                JsonSerializer.Deserialize<List<RoleDto>>(s.RolesJson) ?? new()
            ))
            .ToList();

        var response = new RosterResponseDto(
            d.ToString("yyyy-MM-dd"),
            SlotsPerDay,
            SlotMinutes,
            employees,
            shifts,
            canEdit
        );
        return Ok(response);
    }

    [HttpPut("{date}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> SaveByDate(string date, [FromBody] SaveRosterRequest req)
    {
        if (!DateOnly.TryParse(date, out var d))
            return BadRequest(new { message = "Invalid date. Use YYYY-MM-DD." });

        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var (ok, status, body) = await _rosterService.SaveRosterAsync(user.StoreId, d, req);
        return StatusCode(status, body);
    }

}


