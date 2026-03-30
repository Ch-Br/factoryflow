namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;

public sealed record TicketListItemDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string TicketTypeName,
    string PriorityName,
    string StatusName,
    DateTime CreatedAtUtc,
    DateTime? DueAtUtc);

public sealed record TicketListResultDto(
    IReadOnlyList<TicketListItemDto> Items,
    int TotalCount);
