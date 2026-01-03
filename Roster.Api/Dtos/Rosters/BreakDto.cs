using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Rosters;

public sealed record BreakDto(
    [Range(0, 56)] int Start,
    [Range(0, 56)] int End,
    [Required] string Type
);
