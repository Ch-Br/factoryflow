using FactoryFlow.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryFlow.Modules.Notifications.Infrastructure.Configurations;

public class InternalNotificationConfiguration : IEntityTypeConfiguration<InternalNotification>
{
    public void Configure(EntityTypeBuilder<InternalNotification> builder)
    {
        builder.ToTable("InternalNotifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).HasMaxLength(450).IsRequired();
        builder.Property(n => n.NotificationType).HasMaxLength(100).IsRequired();
        builder.Property(n => n.TicketId).IsRequired();
        builder.Property(n => n.TicketNumber).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(500).IsRequired();
        builder.Property(n => n.EscalationLevel).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.ReadAtUtc).IsRequired(false);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAtUtc })
            .IsDescending(false, true);
    }
}
