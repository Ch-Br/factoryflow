using FactoryFlow.Modules.Identity.Domain.Entities;
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

        if (query.DepartmentId.HasValue)
            q = q.Where(t => t.DepartmentId == query.DepartmentId.Value);

        if (query.OnlyOpen)
            q = q.Where(t => t.StatusId != TicketsSeedData.StatusClosedId);

        var raw = await (
            from t in q.OrderByDescending(t => t.CreatedAtUtc)
            join d in _db.Set<Department>() on t.DepartmentId equals d.Id into departments
            from d in departments.DefaultIfEmpty()
            join s in _db.Set<Site>() on t.SiteId equals s.Id into sites
            from s in sites.DefaultIfEmpty()
            select new
            {
                t.Id,
                t.TicketNumber,
                t.Title,
                TicketTypeName = t.TicketType!.Name,
                PriorityName = t.Priority!.Name,
                StatusName = t.Status!.Name,
                DepartmentName = d != null ? d.Name : null,
                SiteName = s != null ? s.Name : null,
                t.MachineOrWorkstation,
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
            t.DepartmentName,
            t.SiteName,
            t.MachineOrWorkstation,
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
