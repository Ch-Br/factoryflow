using System.Text.Json;
using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;

public sealed class AddTicketCommentCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditWriter _auditWriter;
    private readonly ILogger<AddTicketCommentCommandHandler> _logger;

    public AddTicketCommentCommandHandler(
        DbContext db,
        ICurrentUserService currentUser,
        IAuditWriter auditWriter,
        ILogger<AddTicketCommentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    public async Task<Result<AddTicketCommentResponse>> HandleAsync(
        Guid ticketId,
        AddTicketCommentCommand command,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure<AddTicketCommentResponse>("Benutzer ist nicht authentifiziert.");

        if (!await _db.Set<Ticket>().AnyAsync(t => t.Id == ticketId, ct))
            return Result.Failure<AddTicketCommentResponse>("Ticket nicht gefunden.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var comment = TicketComment.Create(ticketId, command.Text, _currentUser.UserId);

            _db.Set<TicketComment>().Add(comment);
            await _db.SaveChangesAsync(ct);

            var auditPayload = JsonSerializer.Serialize(new
            {
                CommentId = comment.Id,
                TextLength = comment.Text.Length
            });

            await _auditWriter.RecordAsync(
                eventType: "TicketCommentAdded",
                entityType: nameof(Ticket),
                entityId: ticketId.ToString(),
                userId: _currentUser.UserId,
                payload: auditPayload,
                ct: ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Comment {CommentId} added to ticket {TicketId} by {UserId}",
                comment.Id, ticketId, _currentUser.UserId);

            return Result.Success(new AddTicketCommentResponse(comment.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add comment to ticket {TicketId}", ticketId);
            await transaction.RollbackAsync(ct);
            return Result.Failure<AddTicketCommentResponse>("Kommentar konnte nicht gespeichert werden.");
        }
    }
}
