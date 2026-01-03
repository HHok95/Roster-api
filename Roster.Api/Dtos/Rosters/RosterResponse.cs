namespace Roster.Api.Dtos.Rosters;

public sealed record EmployeeMiniDto(Guid Id, string DisplayName);

public sealed record RosterShiftResponseDto(
    Guid Id,
    string ExternalShiftId,
    Guid EmployeeId,
    int StartSlot,
    int EndSlot,
    object Breaks,
    object Roles
);

public sealed record RosterResponseDto(
    string Date,
    int SlotsPerDay,
    int SlotMinutes,
    List<EmployeeMiniDto> Employees,
    List<RosterShiftResponseDto> Shifts,
    bool CanEdit
);
