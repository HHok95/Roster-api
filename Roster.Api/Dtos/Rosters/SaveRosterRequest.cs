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
    }
}
