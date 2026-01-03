using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Models;

public class RosterDay
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid StoreId { get; set; }

    // One roster per store per date
    public DateOnly Date { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Shift> Shifts { get; set; } = new();


}
