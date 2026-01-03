using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Roster.Api.Models;

namespace Roster.Api.Utils;

public static class CurrentUser
{
    public static Task<ApplicationUser?> GetAsync(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal principal)
        => userManager.GetUserAsync(principal);
}
