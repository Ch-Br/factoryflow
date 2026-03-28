using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Audit.Infrastructure.Services;

public class AuditWriter : IAuditWriter
{
    private readonly DbContext _db;

    public AuditWriter(DbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(
        string eventType,
        string entityType,
        string entityId,
        string userId,
        string? payload = null,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry(eventType, entityType, entityId, userId, payload);
        _db.Set<AuditEntry>().Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
