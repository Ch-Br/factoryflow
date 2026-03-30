namespace FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;

public sealed record GetOverdueTicketsQuery(
    Guid? PriorityId = null,
    Guid? DepartmentId = null,
    Guid? StatusId = null);
