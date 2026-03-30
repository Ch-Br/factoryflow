namespace FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;

public sealed record OverdueTicketDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string TicketTypeName,
    string PriorityName,
    string StatusName,
    DateTime DueAtUtc,
    TimeSpan OverdueBy,
    DateTime CreatedAtUtc);

public sealed record OverdueTicketsResultDto(
    IReadOnlyList<OverdueTicketDto> Items,
    int TotalCount);
