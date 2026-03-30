using System.Text.Json;
using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;

public sealed class ChangeTicketStatusCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditWriter _auditWriter;
    private readonly ILogger<ChangeTicketStatusCommandHandler> _logger;

    public ChangeTicketStatusCommandHandler(
        DbContext db,
        ICurrentUserService currentUser,
        IAuditWriter auditWriter,
        ILogger<ChangeTicketStatusCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(
        Guid ticketId,
        ChangeTicketStatusCommand command,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure("Benutzer ist nicht authentifiziert.");

        var ticket = await _db.Set<Ticket>()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            return Result.Failure("Ticket nicht gefunden.");

        if (!await _db.Set<TicketStatus>().AnyAsync(s => s.Id == command.NewStatusId, ct))
            return Result.Failure("Zielstatus existiert nicht.");

        if (ticket.StatusId == command.NewStatusId)
            return Result.Failure("Der neue Status ist identisch mit dem aktuellen Status.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var previousStatusId = ticket.ChangeStatus(command.NewStatusId);

            await _db.SaveChangesAsync(ct);

            var auditPayload = JsonSerializer.Serialize(new
            {
                PreviousStatusId = previousStatusId,
                NewStatusId = command.NewStatusId
            });

            await _auditWriter.RecordAsync(
                eventType: "TicketStatusChanged",
                entityType: nameof(Ticket),
                entityId: ticket.Id.ToString(),
                userId: _currentUser.UserId,
                payload: auditPayload,
                ct: ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Ticket {TicketId} status changed from {PreviousStatusId} to {NewStatusId} by {UserId}",
                ticketId, previousStatusId, command.NewStatusId, _currentUser.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change status for ticket {TicketId}", ticketId);
            await transaction.RollbackAsync(ct);
            return Result.Failure("Statusänderung konnte nicht durchgeführt werden.");
        }
    }
}
