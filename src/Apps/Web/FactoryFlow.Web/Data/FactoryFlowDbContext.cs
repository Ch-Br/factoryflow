using FactoryFlow.Modules.Audit.Domain.Entities;
using FactoryFlow.Modules.Audit.Infrastructure.Configurations;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Identity.Infrastructure.Configurations;
using FactoryFlow.Modules.Identity.Infrastructure.Seeds;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Configurations;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Web.Data;

public class FactoryFlowDbContext : IdentityDbContext<ApplicationUser>
{
    public FactoryFlowDbContext(DbContextOptions<FactoryFlowDbContext> options)
        : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity module
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new DepartmentConfiguration());
        builder.ApplyConfiguration(new SiteConfiguration());
        IdentitySeedData.SeedDepartments(builder);
        IdentitySeedData.SeedSites(builder);

        // Tickets module
        builder.ApplyConfiguration(new TicketConfiguration());
        builder.ApplyConfiguration(new TicketCommentConfiguration());
        builder.ApplyConfiguration(new TicketAttachmentConfiguration());
        builder.ApplyConfiguration(new TicketTypeConfiguration());
        builder.ApplyConfiguration(new TicketPriorityConfiguration());
        builder.ApplyConfiguration(new TicketStatusConfiguration());
        TicketsSeedData.Seed(builder);

        // Audit module
        builder.ApplyConfiguration(new AuditEntryConfiguration());

        // PostgreSQL sequence for ticket numbers
        builder.HasSequence<long>("ticket_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);
    }
}
