using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Notifications.Domain.Entities;

public class InternalNotification : Entity<Guid>
{
    private InternalNotification() { }

    public InternalNotification(
        string recipientUserId,
        string notificationType,
        Guid ticketId,
        string ticketNumber,
        string title,
        int escalationLevel,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        NotificationType = notificationType;
        TicketId = ticketId;
        TicketNumber = ticketNumber;
        Title = title;
        EscalationLevel = escalationLevel;
        CreatedAtUtc = createdAtUtc;
    }

    public string RecipientUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public Guid TicketId { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int EscalationLevel { get; private set; }

    public void MarkAsRead(DateTime utcNow)
    {
        ReadAtUtc = utcNow;
    }
}
