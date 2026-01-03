using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Rosters;

public sealed record RoleSlotDto(
    [Range(0, 55)] int SlotNumber,
    [Required] string Type
);
