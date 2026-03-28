using FactoryFlow.Modules.Tickets.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Infrastructure.Services;

public class TicketNumberGenerator : ITicketNumberGenerator
{
    private readonly DbContext _db;

    public TicketNumberGenerator(DbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var result = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('ticket_number_seq') AS \"Value\"")
            .SingleAsync(ct);

        var year = DateTime.UtcNow.Year;
        return $"FF-{year}-{result:D6}";
    }
}
