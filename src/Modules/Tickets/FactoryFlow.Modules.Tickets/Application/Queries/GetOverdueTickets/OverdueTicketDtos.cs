namespace FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;

public sealed record OverdueTicketDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string TicketTypeName,
    string PriorityName,
    string StatusName,
    string? DepartmentName,
    string? SiteName,
    string? MachineOrWorkstation,
    DateTime DueAtUtc,
    TimeSpan OverdueBy,
    DateTime CreatedAtUtc,
    int EscalationLevel);

public sealed record OverdueTicketsResultDto(
    IReadOnlyList<OverdueTicketDto> Items,
    int TotalCount);
