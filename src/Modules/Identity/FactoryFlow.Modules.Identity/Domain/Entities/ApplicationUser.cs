using Microsoft.AspNetCore.Identity;

namespace FactoryFlow.Modules.Identity.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
