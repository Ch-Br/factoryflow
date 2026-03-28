using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryFlow.Modules.Tickets.Infrastructure.Configurations;

public class TicketPriorityConfiguration : IEntityTypeConfiguration<TicketPriority>
{
    public void Configure(EntityTypeBuilder<TicketPriority> builder)
    {
        builder.ToTable("TicketPriorities");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
    }
}
