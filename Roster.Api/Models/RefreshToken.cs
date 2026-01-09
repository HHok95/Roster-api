using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Models;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }
}
