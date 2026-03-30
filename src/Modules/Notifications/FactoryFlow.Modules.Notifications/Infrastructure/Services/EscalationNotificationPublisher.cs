using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Notifications.Application;
using FactoryFlow.Modules.Notifications.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Notifications.Infrastructure.Services;

public sealed class EscalationNotificationPublisher : IEscalationNotificationPublisher
{
    private readonly DbContext _db;
    private readonly ILogger<EscalationNotificationPublisher> _logger;

    public EscalationNotificationPublisher(DbContext db, ILogger<EscalationNotificationPublisher> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task PublishAsync(
        IReadOnlyList<EscalatedTicketInfo> tickets,
        DateTime escalatedAtUtc,
        CancellationToken ct = default)
    {
        if (tickets.Count == 0)
            return;

        var roleIds = await _db.Set<IdentityRole>()
            .Where(r => r.Name == AppRoles.Supervisor || r.Name == AppRoles.Admin)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var recipientUserIds = await _db.Set<IdentityUserRole<string>>()
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);

        if (recipientUserIds.Count == 0)
        {
            _logger.LogWarning("No Supervisor/Admin recipients found for escalation notifications.");
            return;
        }

        foreach (var ticket in tickets)
        {
            foreach (var userId in recipientUserIds)
            {
                _db.Set<InternalNotification>().Add(new InternalNotification(
                    recipientUserId: userId,
                    notificationType: "TicketEscalated",
                    ticketId: ticket.TicketId,
                    ticketNumber: ticket.TicketNumber,
                    title: ticket.Title,
                    escalationLevel: ticket.EscalationLevel,
                    createdAtUtc: escalatedAtUtc));
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Published {Count} escalation notifications for {TicketCount} tickets to {RecipientCount} recipients.",
            tickets.Count * recipientUserIds.Count, tickets.Count, recipientUserIds.Count);
    }
}
