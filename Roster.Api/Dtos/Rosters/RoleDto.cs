using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Rosters;

public sealed record RoleDto(
    [Range(0, 55)] int StartSlot,
    [Range(1, 56)] int EndSlot,
    [Required] string Type
);
