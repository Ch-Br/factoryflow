using System.Text.Json;
using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Tickets.Application.Commands.UpdateTicket;

public sealed class UpdateTicketCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditWriter _auditWriter;
    private readonly ILogger<UpdateTicketCommandHandler> _logger;

    public UpdateTicketCommandHandler(
        DbContext db,
        ICurrentUserService currentUser,
        IAuditWriter auditWriter,
        ILogger<UpdateTicketCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        Guid ticketId,
        UpdateTicketCommand command,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure("Benutzer ist nicht authentifiziert.");

        if (!_currentUser.IsInRole(AppRoles.Supervisor) && !_currentUser.IsInRole(AppRoles.Admin))
            return Result.Failure("Keine Berechtigung für diese Aktion.");

        var ticket = await _db.Set<Ticket>()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return Result.Failure("Ticket nicht gefunden.");

        if (!await _db.Set<TicketPriority>().AnyAsync(p => p.Id == command.PriorityId && p.IsActive, ct))
            return Result.Failure("Ungültige Priorität.");

        var previousTitle = ticket.Title;
        var previousDescription = ticket.Description;
        var previousPriorityId = ticket.PriorityId;
        var previousDueAtUtc = ticket.DueAtUtc;

        bool changed;
        try
        {
            changed = ticket.Update(
                command.Title,
                command.Description,
                command.PriorityId,
                command.DueAtUtc);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }

        if (!changed)
            return Result.Success();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            await _db.SaveChangesAsync(ct);

            var payload = new Dictionary<string, string>();
            if (previousTitle != ticket.Title)
            {
                payload["PreviousTitle"] = previousTitle;
                payload["NewTitle"] = ticket.Title;
            }
            if (previousDescription != ticket.Description)
            {
                payload["PreviousDescription"] = previousDescription;
                payload["NewDescription"] = ticket.Description;
            }
            if (previousPriorityId != ticket.PriorityId)
            {
                payload["PreviousPriorityId"] = previousPriorityId.ToString();
                payload["NewPriorityId"] = ticket.PriorityId.ToString();
            }

            if (previousDueAtUtc != ticket.DueAtUtc)
            {
                payload["PreviousDueAtUtc"] = previousDueAtUtc.HasValue
                    ? previousDueAtUtc.Value.ToString("o")
                    : "";
                payload["NewDueAtUtc"] = ticket.DueAtUtc.HasValue
                    ? ticket.DueAtUtc.Value.ToString("o")
                    : "";
            }

            await _auditWriter.RecordAsync(
                eventType: "TicketUpdated",
                entityType: nameof(Ticket),
                entityId: ticket.Id.ToString(),
                userId: _currentUser.UserId,
                payload: JsonSerializer.Serialize(payload),
                ct: ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Ticket {TicketId} updated by {UserId}",
                ticketId, _currentUser.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ticket {TicketId}", ticketId);
            await transaction.RollbackAsync(ct);
            return Result.Failure("Ticket konnte nicht aktualisiert werden.");
        }
    }
}
