using Microsoft.AspNetCore.Mvc;

namespace Roster.Api.Security;

public sealed class ValidateCsrfAttribute : TypeFilterAttribute
{
    public ValidateCsrfAttribute() : base(typeof(ValidateCsrfFilter)) { }
}
