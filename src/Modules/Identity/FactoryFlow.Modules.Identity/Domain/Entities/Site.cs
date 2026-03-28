using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Identity.Domain.Entities;

public class Site : Entity<Guid>
{
    private Site() { }

    public Site(Guid id, string name, string code)
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
