namespace Roster.Api.Dtos;

public record LoginResponseDto(string UserName, IList<string> roles);
