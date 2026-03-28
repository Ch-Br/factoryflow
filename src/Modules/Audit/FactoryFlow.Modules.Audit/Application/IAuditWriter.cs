namespace FactoryFlow.Modules.Audit.Application;

public interface IAuditWriter
{
    Task RecordAsync(
        string eventType,
        string entityType,
        string entityId,
        string userId,
        string? payload = null,
        CancellationToken ct = default);
}
