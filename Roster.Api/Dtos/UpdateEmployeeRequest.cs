using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos;

public record UpdateEmployeeRequest(
    [Required, StringLength(50, MinimumLength = 1)] string DisplayName,
    bool IsActive = true
);
