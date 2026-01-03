using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Roster.Api.Security;

public sealed class ValidateCsrfFilter : IAsyncActionFilter
{
    private readonly IAntiforgery _antiforgery;
    public ValidateCsrfFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
            await next();
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new { message = "Invalid CSRF token" });
        }
    }
}
