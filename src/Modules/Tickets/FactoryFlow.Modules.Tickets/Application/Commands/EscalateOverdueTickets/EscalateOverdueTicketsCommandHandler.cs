using System.Text.Json;
using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Notifications.Application;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Tickets.Application.Commands.EscalateOverdueTickets;

public sealed class EscalateOverdueTicketsCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditWriter _auditWriter;
    private readonly IEscalationNotificationPublisher _notificationPublisher;
    private readonly ILogger<EscalateOverdueTicketsCommandHandler> _logger;

    public EscalateOverdueTicketsCommandHandler(
        DbContext db,
        ICurrentUserService currentUser,
        IAuditWriter auditWriter,
        IEscalationNotificationPublisher notificationPublisher,
        ILogger<EscalateOverdueTicketsCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result<EscalateOverdueTicketsResponse>> HandleAsync(CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure<EscalateOverdueTicketsResponse>("Benutzer ist nicht authentifiziert.");

        if (!_currentUser.IsInRole(AppRoles.Supervisor) && !_currentUser.IsInRole(AppRoles.Admin))
            return Result.Failure<EscalateOverdueTicketsResponse>("Keine Berechtigung für diese Aktion.");

        var utcNow = DateTime.UtcNow;

        var candidates = await _db.Set<Ticket>()
            .Where(t => t.DueAtUtc != null
                        && t.DueAtUtc <= utcNow
                        && t.StatusId != TicketsSeedData.StatusClosedId
                        && t.EscalationLevel == 0)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("Escalation run found no candidates.");
            return Result.Success(new EscalateOverdueTicketsResponse(0));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var ticket in candidates)
            {
                ticket.Escalate(utcNow);
            }

            await _db.SaveChangesAsync(ct);

            foreach (var ticket in candidates)
            {
                var overdueBy = utcNow - ticket.DueAtUtc!.Value;
                var payload = JsonSerializer.Serialize(new
                {
                    EscalationLevel = 1,
                    ticket.DueAtUtc,
                    OverdueByMinutes = (int)overdueBy.TotalMinutes
                });

                await _auditWriter.RecordAsync(
                    eventType: "TicketEscalated",
                    entityType: nameof(Ticket),
                    entityId: ticket.Id.ToString(),
                    userId: _currentUser.UserId,
                    payload: payload,
                    ct: ct);
            }

            var escalatedInfos = candidates.Select(t => new EscalatedTicketInfo(
                t.Id, t.TicketNumber, t.Title, t.EscalationLevel)).ToList();
            await _notificationPublisher.PublishAsync(escalatedInfos, utcNow, ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Escalation run completed: {Count} tickets escalated by {UserId}.",
                candidates.Count, _currentUser.UserId);

            return Result.Success(new EscalateOverdueTicketsResponse(candidates.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Escalation run failed.");
            await transaction.RollbackAsync(ct);
            return Result.Failure<EscalateOverdueTicketsResponse>("Eskalation konnte nicht durchgeführt werden.");
        }
    }
}
