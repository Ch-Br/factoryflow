using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketStatusLookups;

public sealed class GetTicketStatusLookupsQueryHandler
{
    private readonly DbContext _db;

    public GetTicketStatusLookupsQueryHandler(DbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LookupItemDto>> HandleAsync(CancellationToken ct = default)
    {
        return await _db.Set<TicketStatus>()
            .OrderBy(s => s.SortOrder)
            .Select(s => new LookupItemDto(s.Id, s.Name))
            .ToListAsync(ct);
    }
}
