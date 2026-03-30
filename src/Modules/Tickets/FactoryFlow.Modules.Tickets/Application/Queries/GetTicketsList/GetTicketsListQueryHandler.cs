using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;

public sealed class GetTicketsListQueryHandler
{
    private readonly DbContext _db;

    public GetTicketsListQueryHandler(DbContext db)
    {
        _db = db;
    }

    public async Task<TicketListResultDto> HandleAsync(CancellationToken ct = default)
    {
        var items = await _db.Set<Ticket>()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TicketListItemDto(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.TicketType!.Name,
                t.Priority!.Name,
                t.Status!.Name,
                t.CreatedAtUtc))
            .ToListAsync(ct);

        return new TicketListResultDto(items, items.Count);
    }
}
