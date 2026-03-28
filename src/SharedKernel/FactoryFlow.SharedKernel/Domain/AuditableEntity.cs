namespace FactoryFlow.SharedKernel.Domain;

public abstract class AuditableEntity<TId> : Entity<TId> where TId : notnull
{
    public DateTime CreatedAtUtc { get; protected set; }
    public string CreatedByUserId { get; protected set; } = string.Empty;
}
