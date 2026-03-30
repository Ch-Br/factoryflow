namespace FactoryFlow.Modules.Tickets.Application.Commands.EscalateOverdueTickets;

public sealed record EscalateOverdueTicketsCommand;

public sealed record EscalateOverdueTicketsResponse(int EscalatedCount);
