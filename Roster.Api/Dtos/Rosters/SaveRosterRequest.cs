using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Rosters;

public sealed class SaveRosterRequest : IValidatableObject
{
    [Required]
    public List<ShiftDto> Shifts { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var s in Shifts)
        {
            if (s.EndSlot <= s.StartSlot)
                yield return new ValidationResult(
                    "EndSlot must be greater than StartSlot.",
                    new[] { nameof(Shifts) }
                );

            if (s.Breaks is not null)
            {
                foreach (var b in s.Breaks)
                {
                    if (b.End <= b.Start)
                        yield return new ValidationResult("Break End must be greater than Start.");

                    if (b.Start < s.StartSlot || b.End > s.EndSlot)
                        yield return new ValidationResult("Break must be inside the shift range.");
                }
            }
        }
        // NEW: prevent 2 shifts with same EmployeeId in the same save payload
        var dupEmployeeIds = Shifts
            .GroupBy(s => s.EmployeeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (dupEmployeeIds.Count > 0)
        {
            yield return new ValidationResult(
                $"An employee cannot have multiple shifts in the same roster day. Duplicate EmployeeId(s): {string.Join(", ", dupEmployeeIds)}",
                new[] { nameof(Shifts) }
            );
        }

        // Duplicate ExternalShiftId in the same payload
        var dupExternalIds = Shifts
            .Where(s => !string.IsNullOrWhiteSpace(s.ExternalShiftId))
            .GroupBy(s => s.ExternalShiftId.Trim())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (dupExternalIds.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate ExternalShiftId(s) in request: {string.Join(", ", dupExternalIds)}",
                new[] { nameof(Shifts) }
            );
        }
    }
}
