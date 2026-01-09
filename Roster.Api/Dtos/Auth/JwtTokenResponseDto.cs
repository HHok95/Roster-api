namespace Roster.Api.Dtos.Auth;

public record JwtTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserName,
    string StoreCode,
    Guid StoreId,
    IList<string> Roles
);
