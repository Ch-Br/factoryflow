using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;

public sealed class GetTicketsListQueryHandler
{
    private readonly DbContext _db;

    public GetTicketsListQueryHandler(DbContext db)
    {
        _db = db;
    }

    public Task<TicketListResultDto> HandleAsync(CancellationToken ct = default)
        => HandleAsync(new GetTicketsListQuery(), ct);

    public async Task<TicketListResultDto> HandleAsync(GetTicketsListQuery query, CancellationToken ct = default)
    {
        var q = _db.Set<Ticket>().AsQueryable();

        if (query.StatusId.HasValue)
            q = q.Where(t => t.StatusId == query.StatusId.Value);

        if (query.PriorityId.HasValue)
            q = q.Where(t => t.PriorityId == query.PriorityId.Value);

        if (query.OnlyOpen)
            q = q.Where(t => t.StatusId != TicketsSeedData.StatusClosedId);

        var items = await q
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TicketListItemDto(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.TicketType!.Name,
                t.Priority!.Name,
                t.Status!.Name,
                t.CreatedAtUtc,
                t.DueAtUtc))
            .ToListAsync(ct);

        return new TicketListResultDto(items, items.Count);
    }
}
