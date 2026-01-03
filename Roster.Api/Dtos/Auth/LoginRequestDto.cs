using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos;

public record LoginRequestDto(
    [Required, RegularExpression(@"^\d{4}$", ErrorMessage = "Store Code must be 4 digits")] string StoreCode,
    [Required] string Username,
    [Required] string Password
);
