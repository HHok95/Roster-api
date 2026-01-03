namespace Roster.Api.Dtos;

public record LoginResponseDto(string UserName, string StoreCode, Guid StoreId, IList<string> roles);
