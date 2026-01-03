using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Roster.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestWriteController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;
    public TestWriteController(IAntiforgery antiforgery) => _antiforgery = antiforgery;

    [HttpPost("write")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Write()
    {
        await _antiforgery.ValidateRequestAsync(HttpContext); // CSRF check
        return Ok(new { ok = true });
    }
}
