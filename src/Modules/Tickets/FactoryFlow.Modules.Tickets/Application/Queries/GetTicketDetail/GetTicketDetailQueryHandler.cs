using System.Text.Json;
using FactoryFlow.Modules.Audit.Domain.Entities;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;

public sealed class GetTicketDetailQueryHandler
{
    private static readonly string[] RelevantEventTypes =
        ["TicketCreated", "TicketStatusChanged", "TicketCommentAdded", "TicketUpdated"];

    private static readonly Dictionary<string, string> EventLabels = new()
    {
        ["TicketCreated"] = "Ticket erstellt",
        ["TicketStatusChanged"] = "Status geändert",
        ["TicketCommentAdded"] = "Kommentar hinzugefügt",
        ["TicketUpdated"] = "Ticket bearbeitet"
    };

    private readonly DbContext _db;

    public GetTicketDetailQueryHandler(DbContext db)
    {
        _db = db;
    }

    public async Task<TicketDetailDto?> HandleAsync(Guid ticketId, CancellationToken ct = default)
    {
        var detail = await (
            from t in _db.Set<Ticket>()
            join d in _db.Set<Department>() on t.DepartmentId equals d.Id into departments
            from d in departments.DefaultIfEmpty()
            join s in _db.Set<Site>() on t.SiteId equals s.Id into sites
            from s in sites.DefaultIfEmpty()
            join u in _db.Set<ApplicationUser>() on t.CreatedByUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            where t.Id == ticketId
            select new
            {
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Description,
                TicketTypeName = t.TicketType!.Name,
                t.PriorityId,
                PriorityName = t.Priority!.Name,
                t.StatusId,
                StatusName = t.Status!.Name,
                DepartmentName = d != null ? d.Name : null,
                SiteName = s != null ? s.Name : null,
                t.MachineOrWorkstation,
                t.CreatedAtUtc,
                CreatedByDisplayName = u != null ? (u.FirstName + " " + u.LastName).Trim() : t.CreatedByUserId
            }
        ).FirstOrDefaultAsync(ct);

        if (detail is null)
            return null;

        var comments = await (
            from c in _db.Set<TicketComment>()
            join u in _db.Set<ApplicationUser>() on c.CreatedByUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            where c.TicketId == ticketId
            orderby c.CreatedAtUtc descending
            select new TicketCommentDto(
                c.Id,
                c.Text,
                c.CreatedAtUtc,
                u != null ? (u.FirstName + " " + u.LastName).Trim() : c.CreatedByUserId)
        ).ToListAsync(ct);

        var history = await BuildHistoryAsync(ticketId, ct);

        return new TicketDetailDto(
            detail.Id,
            detail.TicketNumber,
            detail.Title,
            detail.Description,
            detail.TicketTypeName,
            detail.PriorityId,
            detail.PriorityName,
            detail.StatusId,
            detail.StatusName,
            detail.DepartmentName,
            detail.SiteName,
            detail.MachineOrWorkstation,
            detail.CreatedAtUtc,
            detail.CreatedByDisplayName,
            comments,
            history);
    }

    private async Task<IReadOnlyList<TicketHistoryItemDto>> BuildHistoryAsync(
        Guid ticketId, CancellationToken ct)
    {
        var ticketIdStr = ticketId.ToString();

        var auditEntries = await _db.Set<AuditEntry>()
            .Where(a => a.EntityType == "Ticket"
                        && a.EntityId == ticketIdStr
                        && RelevantEventTypes.Contains(a.EventType))
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToListAsync(ct);

        if (auditEntries.Count == 0)
            return [];

        var userIds = auditEntries.Select(a => a.UserId).Distinct().ToList();
        var userLookup = await _db.Set<ApplicationUser>()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim(), ct);

        var statusLookup = await _db.Set<TicketStatus>()
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var commentIds = new List<Guid>();
        foreach (var entry in auditEntries.Where(a => a.EventType == "TicketCommentAdded"))
        {
            var commentId = ExtractGuidFromPayload(entry.Payload, "CommentId");
            if (commentId.HasValue)
                commentIds.Add(commentId.Value);
        }

        var commentTextLookup = commentIds.Count > 0
            ? await _db.Set<TicketComment>()
                .Where(c => commentIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Text, ct)
            : new Dictionary<Guid, string>();

        var items = new List<TicketHistoryItemDto>(auditEntries.Count);

        foreach (var entry in auditEntries)
        {
            var actorName = userLookup.GetValueOrDefault(entry.UserId, entry.UserId);
            var label = EventLabels.GetValueOrDefault(entry.EventType, entry.EventType);
            var text = BuildEventText(entry, statusLookup, commentTextLookup);

            items.Add(new TicketHistoryItemDto(
                entry.OccurredAtUtc,
                entry.EventType,
                label,
                actorName,
                text));
        }

        return items;
    }

    private static string BuildEventText(
        AuditEntry entry,
        Dictionary<Guid, string> statusLookup,
        Dictionary<Guid, string> commentTextLookup)
    {
        switch (entry.EventType)
        {
            case "TicketCreated":
                return "Ticket erstellt";

            case "TicketStatusChanged":
                var prevId = ExtractGuidFromPayload(entry.Payload, "PreviousStatusId");
                var newId = ExtractGuidFromPayload(entry.Payload, "NewStatusId");
                var prevName = prevId.HasValue
                    ? statusLookup.GetValueOrDefault(prevId.Value, prevId.Value.ToString())
                    : "?";
                var newName = newId.HasValue
                    ? statusLookup.GetValueOrDefault(newId.Value, newId.Value.ToString())
                    : "?";
                return $"{prevName} \u2192 {newName}";

            case "TicketCommentAdded":
                var commentId = ExtractGuidFromPayload(entry.Payload, "CommentId");
                if (commentId.HasValue && commentTextLookup.TryGetValue(commentId.Value, out var text))
                {
                    return text.Length > 120 ? text[..120] + "\u2026" : text;
                }
                return "Kommentar hinzugefügt";

            case "TicketUpdated":
                return BuildUpdateText(entry.Payload);

            default:
                return entry.EventType;
        }
    }

    private static string BuildUpdateText(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
            return "Ticket bearbeitet";

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var parts = new List<string>();

            if (doc.RootElement.TryGetProperty("NewTitle", out _))
                parts.Add("Titel");
            if (doc.RootElement.TryGetProperty("NewDescription", out _))
                parts.Add("Beschreibung");
            if (doc.RootElement.TryGetProperty("NewPriorityId", out _))
                parts.Add("Priorität");

            return parts.Count > 0
                ? $"Geändert: {string.Join(", ", parts)}"
                : "Ticket bearbeitet";
        }
        catch (JsonException)
        {
            return "Ticket bearbeitet";
        }
    }

    private static Guid? ExtractGuidFromPayload(string? payload, string propertyName)
    {
        if (string.IsNullOrEmpty(payload))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty(propertyName, out var prop)
                && prop.ValueKind == JsonValueKind.String
                && Guid.TryParse(prop.GetString(), out var guid))
            {
                return guid;
            }
        }
        catch (JsonException)
        {
            // Malformed payload -- return null gracefully
        }

        return null;
    }
}
