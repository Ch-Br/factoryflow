using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;

public sealed class GetOverdueTicketsQueryHandler
{
    private readonly DbContext _db;

    public GetOverdueTicketsQueryHandler(DbContext db)
    {
        _db = db;
    }

    public async Task<OverdueTicketsResultDto> HandleAsync(CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;

        var raw = await _db.Set<Ticket>()
            .Where(t => t.DueAtUtc != null
                        && t.DueAtUtc <= utcNow
                        && t.StatusId != TicketsSeedData.StatusClosedId)
            .OrderBy(t => t.DueAtUtc)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Title,
                TicketTypeName = t.TicketType!.Name,
                PriorityName = t.Priority!.Name,
                StatusName = t.Status!.Name,
                DueAtUtc = t.DueAtUtc!.Value,
                t.CreatedAtUtc,
                t.EscalationLevel
            })
            .ToListAsync(ct);

        var items = raw.Select(t => new OverdueTicketDto(
            t.Id,
            t.TicketNumber,
            t.Title,
            t.TicketTypeName,
            t.PriorityName,
            t.StatusName,
            t.DueAtUtc,
            utcNow - t.DueAtUtc,
            t.CreatedAtUtc,
            t.EscalationLevel
        )).ToList();

        return new OverdueTicketsResultDto(items, items.Count);
    }
}
