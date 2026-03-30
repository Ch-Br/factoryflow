using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Tickets.Domain.Entities;

public class TicketComment : AuditableEntity<Guid>
{
    private TicketComment() { }

    public static TicketComment Create(Guid ticketId, string text, string createdByUserId)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("TicketId darf nicht leer sein.", nameof(ticketId));

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Kommentartext darf nicht leer sein.", nameof(text));

        if (text.Length > 2000)
            throw new ArgumentException("Kommentartext darf maximal 2000 Zeichen lang sein.", nameof(text));

        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new ArgumentException("Ersteller muss gesetzt sein.", nameof(createdByUserId));

        return new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Text = text.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public Guid TicketId { get; private set; }

    public string Text { get; private set; } = string.Empty;
}
