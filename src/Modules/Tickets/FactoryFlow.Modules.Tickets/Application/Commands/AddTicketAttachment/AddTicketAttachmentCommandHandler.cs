using System.Text.Json;
using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using FactoryFlow.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Tickets.Application.Commands.AddTicketAttachment;

public sealed class AddTicketAttachmentCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditWriter _auditWriter;
    private readonly IFileStorage _fileStorage;
    private readonly long _maxFileSizeBytes;
    private readonly ILogger<AddTicketAttachmentCommandHandler> _logger;

    public AddTicketAttachmentCommandHandler(
        DbContext db,
        ICurrentUserService currentUser,
        IAuditWriter auditWriter,
        IFileStorage fileStorage,
        IConfiguration configuration,
        ILogger<AddTicketAttachmentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
        _fileStorage = fileStorage;
        _logger = logger;
        _maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 10 * 1024 * 1024);
    }

    public async Task<Result<AddTicketAttachmentResponse>> HandleAsync(
        Guid ticketId,
        AddTicketAttachmentCommand command,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure<AddTicketAttachmentResponse>("Benutzer ist nicht authentifiziert.");

        if (!await _db.Set<Ticket>().AnyAsync(t => t.Id == ticketId, ct))
            return Result.Failure<AddTicketAttachmentResponse>("Ticket nicht gefunden.");

        if (command.FileSize > _maxFileSizeBytes)
            return Result.Failure<AddTicketAttachmentResponse>(
                $"Datei ist zu groß. Maximale Größe: {_maxFileSizeBytes / (1024 * 1024)} MB.");

        if (command.FileSize <= 0)
            return Result.Failure<AddTicketAttachmentResponse>("Datei ist leer.");

        string storageKey;
        try
        {
            storageKey = await _fileStorage.SaveAsync("tickets", command.FileName, command.Content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store file {FileName} for ticket {TicketId}", command.FileName, ticketId);
            return Result.Failure<AddTicketAttachmentResponse>("Datei konnte nicht gespeichert werden.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var attachment = TicketAttachment.Create(
                ticketId,
                command.FileName,
                command.ContentType,
                command.FileSize,
                storageKey,
                _currentUser.UserId);

            _db.Set<TicketAttachment>().Add(attachment);
            await _db.SaveChangesAsync(ct);

            var auditPayload = JsonSerializer.Serialize(new
            {
                AttachmentId = attachment.Id,
                attachment.FileName,
                attachment.FileSize,
                attachment.ContentType
            });

            await _auditWriter.RecordAsync(
                eventType: "TicketAttachmentAdded",
                entityType: nameof(Ticket),
                entityId: ticketId.ToString(),
                userId: _currentUser.UserId,
                payload: auditPayload,
                ct: ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Attachment {AttachmentId} ({FileName}) added to ticket {TicketId} by {UserId}",
                attachment.Id, attachment.FileName, ticketId, _currentUser.UserId);

            return Result.Success(new AddTicketAttachmentResponse(attachment.Id, attachment.FileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save attachment metadata for ticket {TicketId}", ticketId);
            await transaction.RollbackAsync(ct);
            return Result.Failure<AddTicketAttachmentResponse>("Anhang konnte nicht gespeichert werden.");
        }
    }
}
