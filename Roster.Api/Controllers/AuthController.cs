using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roster.Api.Data;
using Roster.Api.Dtos;
using Roster.Api.Models;

namespace Roster.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db, SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _db = db;
        _signIn = signIn;
        _users = users;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto req)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Code == req.StoreCode);
        if (store is null) return Unauthorized(new { message = "Invalid credentials" });

        var user = await _users.FindByNameAsync(req.Username);
        if (user is null) return Unauthorized();

        // Critical: user must belong to that store
        if (user.StoreId != store.Id) return Unauthorized(new { message = "Invalid credentials" });

        var result = await _signIn.PasswordSignInAsync(
            user,
            req.Password,
            isPersistent: true,
            lockoutOnFailure: true
        );
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials" });

        var roles = await _users.GetRolesAsync(user);

        return Ok(new LoginResponseDto(
            user.UserName!,
            store.Code,
            store.Id,
            roles
        ));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new { username = user.UserName, storeId = user.StoreId, roles });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return NoContent();
    }

}
