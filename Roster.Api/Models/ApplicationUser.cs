using Microsoft.AspNetCore.Identity;

namespace Roster.Api.Models;

public class ApplicationUser : IdentityUser
{
    public Guid StoreId { get; set; }
}
