using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Rosters;

public sealed record ShiftDto(
    [Required] string ExternalShiftId,
    [Required] Guid EmployeeId,

    [Range(0, 55)] int StartSlot,
    [Range(1, 56)] int EndSlot,

    List<BreakDto>? Breaks,
    List<RoleDto>? Roles
);
