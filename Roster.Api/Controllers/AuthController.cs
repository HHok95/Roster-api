using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Roster.Api.Data;
using Roster.Api.Dtos;
using Roster.Api.Dtos.Auth;
using Roster.Api.Models;
using Roster.Api.Services;

namespace Roster.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext db,
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _db = db;
        _signIn = signIn;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto req)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Code == req.StoreCode);
        if (store is null) return Unauthorized(new { message = "Invalid credentials" });

        var user = await _users.FindByNameAsync(req.Username);
        if (user is null) return Unauthorized(new { message = "Invalid credentials" });

        // Critical: user must belong to that store
        if (user.StoreId != store.Id) return Unauthorized(new { message = "Invalid credentials" });

        // Verify password (we still use SignInManager for password checking and lockout)
        var result = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials" });

        // Generate JWT tokens
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, store.Code);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Get JWT settings for expiry calculation
        var jwtSettings = _configuration.GetSection("Jwt").Get<JwtSettings>();
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings?.AccessTokenExpiryMinutes ?? 480);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(jwtSettings?.RefreshTokenExpiryDays ?? 7);

        // Store refresh token in database
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        // Set refresh token in HttpOnly cookie
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.None,
            Expires = refreshExpiresAt
        });

        var roles = await _users.GetRolesAsync(user);

        return Ok(new JwtTokenResponseDto(
            accessToken,
            string.Empty, // Don't expose refresh token in response body (it's in cookie)
            expiresAt,
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
        // Get user ID from JWT claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Revoke all active refresh tokens for this user
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken");

        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto req)
    {
        // Read refresh token from HttpOnly cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshTokenFromCookie))
            return BadRequest(new { message = "Refresh token not found" });

        // Validate the expired access token
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(req.AccessToken);
        if (principal == null)
            return BadRequest(new { message = "Invalid access token" });

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return BadRequest(new { message = "Invalid token claims" });

        // Find user
        var user = await _users.FindByIdAsync(userId);
        if (user == null)
            return BadRequest(new { message = "User not found" });

        // Validate refresh token from database
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Token == refreshTokenFromCookie &&
                !t.IsRevoked);

        if (storedToken == null)
            return BadRequest(new { message = "Invalid refresh token" });

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Refresh token expired" });

        // Get store code from claims or database
        var storeCode = principal.FindFirstValue("StoreCode");
        if (storeCode == null)
        {
            var store = await _db.Stores.FindAsync(user.StoreId);
            storeCode = store?.Code ?? "0000";
        }

        // Generate new tokens
        var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, storeCode);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Get JWT settings for expiry calculation
        var jwtSettings = _configuration.GetSection("Jwt").Get<JwtSettings>();
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings?.AccessTokenExpiryMinutes ?? 480);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(jwtSettings?.RefreshTokenExpiryDays ?? 7);

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Store new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newRefreshTokenEntity);
        await _db.SaveChangesAsync();

        // Set new refresh token in HttpOnly cookie
        Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.None,
            Expires = refreshExpiresAt
        });

        var roles = await _users.GetRolesAsync(user);

        return Ok(new JwtTokenResponseDto(
            newAccessToken,
            string.Empty, // Don't expose refresh token in response body (it's in cookie)
            expiresAt,
            user.UserName!,
            storeCode,
            user.StoreId,
            roles
        ));
    }

}
