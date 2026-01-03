using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Roster.Api.Dtos;
using Roster.Api.Models;

namespace Roster.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public AuthController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto req)
    {
        var user = await _users.FindByNameAsync(req.Username);
        if (user is null) return Unauthorized();

        var result = await _signIn.PasswordSignInAsync(user, req.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new LoginResponseDto(user.UserName!, roles));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new LoginResponseDto(user.UserName!, roles));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return NoContent();
    }

}
