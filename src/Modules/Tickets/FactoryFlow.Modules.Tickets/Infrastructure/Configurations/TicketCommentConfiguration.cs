using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryFlow.Modules.Tickets.Infrastructure.Configurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TicketId, c.CreatedAtUtc });
    }
}
