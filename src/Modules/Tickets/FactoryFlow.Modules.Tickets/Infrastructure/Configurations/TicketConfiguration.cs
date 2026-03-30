using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryFlow.Modules.Tickets.Infrastructure.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketNumber)
            .HasMaxLength(30)
            .IsRequired();
        builder.HasIndex(t => t.TicketNumber).IsUnique();

        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(4000).IsRequired();

        builder.Property(t => t.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Property(t => t.MachineOrWorkstation).HasMaxLength(200);

        builder.Property(t => t.DueAtUtc).IsRequired(false);

        builder.HasOne(t => t.TicketType)
            .WithMany()
            .HasForeignKey(t => t.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Priority)
            .WithMany()
            .HasForeignKey(t => t.PriorityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Status)
            .WithMany()
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.DepartmentId);
        builder.HasIndex(t => t.StatusId);
        builder.HasIndex(t => t.CreatedAtUtc);
    }
}
