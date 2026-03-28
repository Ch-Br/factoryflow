using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Infrastructure.Seeds;

public static class TicketsSeedData
{
    // Statuses
    public static readonly Guid StatusNewId = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    public static readonly Guid StatusInProgressId = Guid.Parse("c0000000-0001-0000-0000-000000000002");
    public static readonly Guid StatusClosedId = Guid.Parse("c0000000-0001-0000-0000-000000000003");

    // Types
    public static readonly Guid TypeMachineFailureId = Guid.Parse("d0000000-0001-0000-0000-000000000001");
    public static readonly Guid TypeQualityIssueId = Guid.Parse("d0000000-0001-0000-0000-000000000002");
    public static readonly Guid TypeMaterialShortageId = Guid.Parse("d0000000-0001-0000-0000-000000000003");
    public static readonly Guid TypeMaintenanceId = Guid.Parse("d0000000-0001-0000-0000-000000000004");
    public static readonly Guid TypeGeneralRequestId = Guid.Parse("d0000000-0001-0000-0000-000000000005");

    // Priorities
    public static readonly Guid PriorityCriticalId = Guid.Parse("e0000000-0001-0000-0000-000000000001");
    public static readonly Guid PriorityHighId = Guid.Parse("e0000000-0001-0000-0000-000000000002");
    public static readonly Guid PriorityMediumId = Guid.Parse("e0000000-0001-0000-0000-000000000003");
    public static readonly Guid PriorityLowId = Guid.Parse("e0000000-0001-0000-0000-000000000004");

    public static void Seed(ModelBuilder builder)
    {
        SeedStatuses(builder);
        SeedTypes(builder);
        SeedPriorities(builder);
    }

    private static void SeedStatuses(ModelBuilder builder)
    {
        builder.Entity<TicketStatus>().HasData(
            new { Id = StatusNewId, Name = "Neu", Code = "new", SortOrder = 1 },
            new { Id = StatusInProgressId, Name = "In Bearbeitung", Code = "in_progress", SortOrder = 2 },
            new { Id = StatusClosedId, Name = "Geschlossen", Code = "closed", SortOrder = 3 }
        );
    }

    private static void SeedTypes(ModelBuilder builder)
    {
        builder.Entity<TicketType>().HasData(
            new { Id = TypeMachineFailureId, Name = "Maschinenstörung", Code = "machine_failure", IsActive = true },
            new { Id = TypeQualityIssueId, Name = "Qualitätsabweichung", Code = "quality_issue", IsActive = true },
            new { Id = TypeMaterialShortageId, Name = "Materialmangel", Code = "material_shortage", IsActive = true },
            new { Id = TypeMaintenanceId, Name = "Wartungsbedarf", Code = "maintenance", IsActive = true },
            new { Id = TypeGeneralRequestId, Name = "Allgemeine Anfrage", Code = "general_request", IsActive = true }
        );
    }

    private static void SeedPriorities(ModelBuilder builder)
    {
        builder.Entity<TicketPriority>().HasData(
            new { Id = PriorityCriticalId, Name = "Kritisch", Code = "critical", SortOrder = 1, IsActive = true },
            new { Id = PriorityHighId, Name = "Hoch", Code = "high", SortOrder = 2, IsActive = true },
            new { Id = PriorityMediumId, Name = "Mittel", Code = "medium", SortOrder = 3, IsActive = true },
            new { Id = PriorityLowId, Name = "Niedrig", Code = "low", SortOrder = 4, IsActive = true }
        );
    }
}
