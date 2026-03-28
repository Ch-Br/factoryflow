using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Tickets.Domain.Entities;

public class TicketStatus : Entity<Guid>
{
    private TicketStatus() { }

    public TicketStatus(Guid id, string name, string code, int sortOrder)
    {
        Id = id;
        Name = name;
        Code = code;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Machine-readable key, e.g. "new", "in_progress", "closed".</summary>
    public string Code { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }
}
