namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;

public sealed record TicketListItemDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string TicketTypeName,
    string PriorityName,
    string StatusName,
    string? DepartmentName,
    string? SiteName,
    string? MachineOrWorkstation,
    DateTime CreatedAtUtc,
    DateTime? DueAtUtc,
    string DueState);

public sealed record TicketListResultDto(
    IReadOnlyList<TicketListItemDto> Items,
    int TotalCount);
