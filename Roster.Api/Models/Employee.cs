using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Models;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }

    [Required][StringLength(50)] public string DisplayName { get; set; } = String.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
