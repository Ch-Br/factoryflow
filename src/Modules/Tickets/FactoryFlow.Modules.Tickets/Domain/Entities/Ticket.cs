using FactoryFlow.SharedKernel.Domain;

namespace FactoryFlow.Modules.Tickets.Domain.Entities;

public class Ticket : AuditableEntity<Guid>
{
    private Ticket() { }

    public static Ticket Create(
        string title,
        string description,
        Guid ticketTypeId,
        Guid priorityId,
        Guid departmentId,
        Guid? siteId,
        string? machineOrWorkstation,
        Guid statusNewId,
        string createdByUserId,
        DateTime? dueAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Titel darf nicht leer sein.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Beschreibung darf nicht leer sein.", nameof(description));

        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new ArgumentException("Ersteller muss gesetzt sein.", nameof(createdByUserId));

        if (dueAtUtc.HasValue && dueAtUtc.Value < DateTime.UtcNow)
            throw new ArgumentException("Fälligkeit darf nicht in der Vergangenheit liegen.", nameof(dueAtUtc));

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            TicketTypeId = ticketTypeId,
            PriorityId = priorityId,
            DepartmentId = departmentId,
            SiteId = siteId,
            MachineOrWorkstation = machineOrWorkstation?.Trim(),
            StatusId = statusNewId,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            DueAtUtc = dueAtUtc
        };
    }

    public string TicketNumber { get; internal set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public Guid TicketTypeId { get; private set; }
    public TicketType? TicketType { get; private set; }

    public Guid PriorityId { get; private set; }
    public TicketPriority? Priority { get; private set; }

    public Guid StatusId { get; private set; }
    public TicketStatus? Status { get; private set; }

    public DateTime? DueAtUtc { get; private set; }

    /// <summary>
    /// Updates editable fields. Returns <c>true</c> if any value changed, <c>false</c> if all values were identical.
    /// </summary>
    public bool Update(string title, string description, Guid priorityId, DateTime? dueAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Titel darf nicht leer sein.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Beschreibung darf nicht leer sein.", nameof(description));

        if (dueAtUtc.HasValue && dueAtUtc.Value < CreatedAtUtc)
            throw new ArgumentException("Fälligkeit darf nicht vor dem Erstelldatum liegen.", nameof(dueAtUtc));

        var trimmedTitle = title.Trim();
        var trimmedDescription = description.Trim();

        if (trimmedTitle == Title && trimmedDescription == Description && priorityId == PriorityId && dueAtUtc == DueAtUtc)
            return false;

        Title = trimmedTitle;
        Description = trimmedDescription;
        PriorityId = priorityId;
        DueAtUtc = dueAtUtc;
        return true;
    }

    public Guid ChangeStatus(Guid newStatusId)
    {
        if (newStatusId == StatusId)
            throw new InvalidOperationException("Der neue Status ist identisch mit dem aktuellen Status.");

        var previousStatusId = StatusId;
        StatusId = newStatusId;
        return previousStatusId;
    }

    public Guid DepartmentId { get; private set; }

    public Guid? SiteId { get; private set; }

    /// <summary>Optional free-text until Assets module provides a proper FK.</summary>
    public string? MachineOrWorkstation { get; private set; }
}
