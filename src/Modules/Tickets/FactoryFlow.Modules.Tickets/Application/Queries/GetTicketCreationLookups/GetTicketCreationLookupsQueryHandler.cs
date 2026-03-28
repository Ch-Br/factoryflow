using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;

public sealed class GetTicketCreationLookupsQueryHandler
{
    private readonly DbContext _db;

    public GetTicketCreationLookupsQueryHandler(DbContext db)
    {
        _db = db;
    }

    public async Task<TicketCreationLookupsDto> HandleAsync(CancellationToken ct = default)
    {
        var types = await _db.Set<TicketType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new LookupItemDto(t.Id, t.Name))
            .ToListAsync(ct);

        var priorities = await _db.Set<TicketPriority>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new LookupItemDto(p.Id, p.Name))
            .ToListAsync(ct);

        var departments = await _db.Set<Department>()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new LookupItemDto(d.Id, d.Name))
            .ToListAsync(ct);

        var sites = await _db.Set<Site>()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new LookupItemDto(s.Id, s.Name))
            .ToListAsync(ct);

        return new TicketCreationLookupsDto(types, priorities, departments, sites);
    }
}
