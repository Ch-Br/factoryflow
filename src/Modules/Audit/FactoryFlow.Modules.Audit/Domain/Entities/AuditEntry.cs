using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Audit.Domain.Entities;

public class AuditEntry : Entity<Guid>
{
    private AuditEntry() { }

    public AuditEntry(
        string eventType,
        string entityType,
        string entityId,
        string userId,
        string? payload = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        Payload = payload;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public string EventType { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string? Payload { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
}
