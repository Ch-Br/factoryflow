using FactoryFlow.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryFlow.Modules.Identity.Infrastructure.Seeds;

public static class IdentitySeedData
{
    public static readonly Guid ProductionDeptId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    public static readonly Guid MaintenanceDeptId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000002");
    public static readonly Guid QualityDeptId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000003");
    public static readonly Guid LogisticsDeptId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000004");
    public static readonly Guid EngineeringDeptId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000005");

    public static readonly Guid MainSiteId = Guid.Parse("b2c3d4e5-0001-0000-0000-000000000001");
    public static readonly Guid WarehouseSiteId = Guid.Parse("b2c3d4e5-0001-0000-0000-000000000002");

    public static void SeedDepartments(ModelBuilder builder)
    {
        builder.Entity<Department>().HasData(
            new { Id = ProductionDeptId, Name = "Produktion", Code = "PROD", IsActive = true },
            new { Id = MaintenanceDeptId, Name = "Instandhaltung", Code = "MAINT", IsActive = true },
            new { Id = QualityDeptId, Name = "Qualität", Code = "QA", IsActive = true },
            new { Id = LogisticsDeptId, Name = "Logistik / Lager", Code = "LOG", IsActive = true },
            new { Id = EngineeringDeptId, Name = "Konstruktion", Code = "ENG", IsActive = true }
        );
    }

    public static void SeedSites(ModelBuilder builder)
    {
        builder.Entity<Site>().HasData(
            new { Id = MainSiteId, Name = "Hauptwerk", Code = "HW", IsActive = true },
            new { Id = WarehouseSiteId, Name = "Lager Süd", Code = "LS", IsActive = true }
        );
    }

    public static async Task SeedUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByNameAsync("admin@factoryflow.local") is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = "admin@factoryflow.local",
            Email = "admin@factoryflow.local",
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            DepartmentId = MaintenanceDeptId,
            SiteId = MainSiteId
        };

        await userManager.CreateAsync(admin, "Admin123!");
    }
}
