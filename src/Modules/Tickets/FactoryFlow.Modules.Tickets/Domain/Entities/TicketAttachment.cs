using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Tickets.Domain.Entities;

public class TicketAttachment : AuditableEntity<Guid>
{
    private TicketAttachment() { }

    public static TicketAttachment Create(
        Guid ticketId,
        string fileName,
        string contentType,
        long fileSize,
        string storageKey,
        string createdByUserId)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("TicketId darf nicht leer sein.", nameof(ticketId));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dateiname darf nicht leer sein.", nameof(fileName));

        if (fileSize <= 0)
            throw new ArgumentException("Dateigröße muss größer als 0 sein.", nameof(fileSize));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("StorageKey darf nicht leer sein.", nameof(storageKey));

        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new ArgumentException("Ersteller muss gesetzt sein.", nameof(createdByUserId));

        return new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            FileName = fileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            FileSize = fileSize,
            StorageKey = storageKey,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public Guid TicketId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
}
