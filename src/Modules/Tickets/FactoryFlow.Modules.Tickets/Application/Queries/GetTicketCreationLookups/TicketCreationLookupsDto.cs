namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;

public sealed record TicketCreationLookupsDto(
    IReadOnlyList<LookupItemDto> TicketTypes,
    IReadOnlyList<LookupItemDto> Priorities,
    IReadOnlyList<LookupItemDto> Departments,
    IReadOnlyList<LookupItemDto> Sites);

public sealed record LookupItemDto(Guid Id, string Name);
