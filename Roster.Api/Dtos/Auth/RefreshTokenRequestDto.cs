using System.ComponentModel.DataAnnotations;

namespace Roster.Api.Dtos.Auth;

public record RefreshTokenRequestDto(
    [Required] string AccessToken
);
