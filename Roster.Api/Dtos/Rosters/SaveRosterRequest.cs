using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Rosters;

public sealed class SaveRosterRequest : IValidatableObject
{
    [Required]
    public List<ShiftDto> Shifts { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var i = 0; i < Shifts.Count; i++)
        {
            var s = Shifts[i];

            if (s.EndSlot <= s.StartSlot)
            {
                yield return new ValidationResult(
                    $"Shift[{i}] EndSlot must be greater than StartSlot.",
                    new[] { nameof(Shifts) }
                );
            }

            // Break validation
            if (s.Breaks is not null)
            {
                for (var j = 0; j < s.Breaks.Count; j++)
                {
                    var b = s.Breaks[j];

                    if (b.End <= b.Start)
                        yield return new ValidationResult($"Shift[{i}] Break[{j}] End must be greater than Start.");

                    if (b.Start < s.StartSlot || b.End > s.EndSlot)
                        yield return new ValidationResult($"Shift[{i}] Break[{j}] must be inside the shift range.");
                }
            }

            // NEW: Role segments validation (Option A)
            if (s.Roles is not null && s.Roles.Count > 0)
            {
                // basic segment checks
                for (var j = 0; j < s.Roles.Count; j++)
                {
                    var r = s.Roles[j];

                    if (r.EndSlot <= r.StartSlot)
                        yield return new ValidationResult($"Shift[{i}] Role[{j}] EndSlot must be greater than StartSlot.");

                    if (r.StartSlot < s.StartSlot || r.EndSlot > s.EndSlot)
                        yield return new ValidationResult($"Shift[{i}] Role[{j}] must be inside the shift range.");
                }

                // overlap check (sort by StartSlot, then compare)
                var ordered = s.Roles
                    .OrderBy(x => x.StartSlot)
                    .ThenBy(x => x.EndSlot)
                    .ToList();

                for (var k = 1; k < ordered.Count; k++)
                {
                    var prev = ordered[k - 1];
                    var curr = ordered[k];

                    // overlap if current starts before previous ends
                    if (curr.StartSlot < prev.EndSlot)
                        yield return new ValidationResult($"Shift[{i}] Role segments overlap.");
                }
            }
        }

        // Prevent 2 shifts with same EmployeeId in the same save payload
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

        // Duplicate ExternalShiftId in the same payload (trim + ignore empty)
        var dupExternalIds = Shifts
            .Select(s => (s.ExternalShiftId ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
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
