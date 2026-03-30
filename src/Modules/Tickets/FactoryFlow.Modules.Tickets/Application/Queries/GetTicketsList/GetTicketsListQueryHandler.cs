using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Domain.Services;
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

        var raw = await q
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Title,
                TicketTypeName = t.TicketType!.Name,
                PriorityName = t.Priority!.Name,
                StatusName = t.Status!.Name,
                t.StatusId,
                t.CreatedAtUtc,
                t.DueAtUtc
            })
            .ToListAsync(ct);

        var utcNow = DateTime.UtcNow;

        var items = raw.Select(t => new TicketListItemDto(
            t.Id,
            t.TicketNumber,
            t.Title,
            t.TicketTypeName,
            t.PriorityName,
            t.StatusName,
            t.CreatedAtUtc,
            t.DueAtUtc,
            DueStateCalculator.Calculate(
                t.DueAtUtc,
                t.StatusId == TicketsSeedData.StatusClosedId,
                utcNow).ToString()
        )).ToList();

        return new TicketListResultDto(items, items.Count);
    }
}
