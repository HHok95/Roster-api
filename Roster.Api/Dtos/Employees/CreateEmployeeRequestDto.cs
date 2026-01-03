using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos;

public record CreateEmployeeRequestDto
(
    [Required, StringLength(100, MinimumLength = 1)] string DisplayName
);
