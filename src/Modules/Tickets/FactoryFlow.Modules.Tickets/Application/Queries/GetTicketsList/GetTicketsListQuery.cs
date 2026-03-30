namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;

public sealed record GetTicketsListQuery(
    Guid? StatusId = null,
    Guid? PriorityId = null,
    Guid? DepartmentId = null,
    bool OnlyOpen = false);
