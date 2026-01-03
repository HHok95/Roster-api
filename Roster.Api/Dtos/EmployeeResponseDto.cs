namespace Roster.Api.Dtos;

public record EmployeeResponseDto(
    Guid Id,
    string DisplayName,
    bool IsActive
);
