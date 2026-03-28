using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Tickets.Domain.Entities;

public class TicketType : Entity<Guid>
{
    private TicketType() { }

    public TicketType(Guid id, string name, string code)
    {
        Id = id;
        Name = name;
        Code = code;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}
