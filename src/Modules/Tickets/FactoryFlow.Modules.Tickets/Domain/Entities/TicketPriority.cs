using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Tickets.Domain.Entities;

public class TicketPriority : Entity<Guid>
{
    private TicketPriority() { }

    public TicketPriority(Guid id, string name, string code, int sortOrder)
    {
        Id = id;
        Name = name;
        Code = code;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    /// <summary>Lower value = higher urgency (1 = critical).</summary>
    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;
}
