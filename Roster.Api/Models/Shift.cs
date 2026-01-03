using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Models;

public class Shift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid RosterDayId { get; set; }
    public RosterDay? RosterDay { get; set; }
    [Required] public Guid EmployeeId { get; set; }

    // store your frontend shift id like: "shift-111..."
    [Required][StringLength(80)] public string ExternalShiftId { get; set; } = string.Empty;

    // 56 slots (0..55). EndSlot is exclusive (1..56)
    public int StartSlot { get; set; }
    public int EndSlot { get; set; }

    // keep MVP simple: store as JSON strings
    public string BreaksJson { get; set; } = "[]";
    public string RolesJson { get; set; } = "[]";

}
