using FactoryFlow.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeedData));

        string[] roles = [AppRoles.User, AppRoles.Supervisor, AppRoles.Admin];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Seeded role {Role}", role);
            }
        }

        await EnsureUserAsync(userManager, logger,
            email: "admin@factoryflow.local",
            firstName: "System", lastName: "Administrator",
            password: "Admin123!",
            role: AppRoles.Admin,
            departmentId: MaintenanceDeptId, siteId: MainSiteId);

        await EnsureUserAsync(userManager, logger,
            email: "supervisor@factoryflow.local",
            firstName: "Demo", lastName: "Supervisor",
            password: "Demo123!",
            role: AppRoles.Supervisor,
            departmentId: ProductionDeptId, siteId: MainSiteId);

        await EnsureUserAsync(userManager, logger,
            email: "user@factoryflow.local",
            firstName: "Demo", lastName: "User",
            password: "Demo123!",
            role: AppRoles.User,
            departmentId: ProductionDeptId, siteId: MainSiteId);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email, string firstName, string lastName,
        string password, string role,
        Guid departmentId, Guid siteId)
    {
        var user = await userManager.FindByNameAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                DepartmentId = departmentId,
                SiteId = siteId
            };
            await userManager.CreateAsync(user, password);
            logger.LogInformation("Seeded user {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Assigned role {Role} to {Email}", role, email);
        }
    }
}
