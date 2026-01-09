using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roster.Api.Data;
using Roster.Api.Dtos;
using Roster.Api.Models;
using Roster.Api.Utils;

namespace Roster.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public EmployeesController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ---------- Read endpoints ----------
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var q = _db.Employees
            .AsNoTracking()
            .Where(e => e.StoreId == user.StoreId);

        if (!includeInactive)
            q = q.Where(e => e.IsActive);

        var items = await q
            .OrderBy(e => e.DisplayName)
            .Select(e => new EmployeeResponseDto(e.Id, e.DisplayName, e.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var e = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.StoreId == user.StoreId);

        if (e is null) return NotFound();

        return Ok(new EmployeeResponseDto(e.Id, e.DisplayName, e.IsActive));
    }

    // ---------- Write endpoints (Manager) ----------
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto req)
    {
        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var employee = new Employee
        {
            StoreId = user.StoreId,
            DisplayName = req.DisplayName.Trim(),
            IsActive = true
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            new EmployeeResponseDto(employee.Id, employee.DisplayName, employee.IsActive)
        );
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest req)
    {
        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var e = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id && x.StoreId == user.StoreId);
        if (e is null) return NotFound();

        e.DisplayName = req.DisplayName.Trim();
        e.IsActive = req.IsActive;

        await _db.SaveChangesAsync();

        return Ok(new EmployeeResponseDto(e.Id, e.DisplayName, e.IsActive));
    }

    // Soft delete
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var user = await CurrentUser.GetAsync(_users, User);
        if (user is null) return Unauthorized();

        var e = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id && x.StoreId == user.StoreId);
        if (e is null) return NotFound();

        e.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
