using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryFlow.Modules.Tickets.Infrastructure.Configurations;

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.FileSize).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(a => a.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TicketId, a.CreatedAtUtc });
    }
}
