using System.Text.Json;
using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Domain.Services;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;

public sealed class CreateTicketCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketNumberGenerator _numberGenerator;
    private readonly IAuditWriter _auditWriter;
    private readonly ILogger<CreateTicketCommandHandler> _logger;

    public CreateTicketCommandHandler(
        DbContext db,
        ICurrentUserService currentUser,
        ITicketNumberGenerator numberGenerator,
        IAuditWriter auditWriter,
        ILogger<CreateTicketCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _numberGenerator = numberGenerator;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    public async Task<Result<CreateTicketResponse>> HandleAsync(
        CreateTicketCommand command,
        CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure<CreateTicketResponse>("Benutzer ist nicht authentifiziert.");

        // Validate references exist
        var errors = new List<string>();

        if (!await _db.Set<TicketType>().AnyAsync(t => t.Id == command.TicketTypeId && t.IsActive, ct))
            errors.Add("Ungültiger Ticket-Typ.");

        if (!await _db.Set<TicketPriority>().AnyAsync(p => p.Id == command.PriorityId && p.IsActive, ct))
            errors.Add("Ungültige Priorität.");

        if (!await _db.Set<TicketStatus>().AnyAsync(s => s.Id == TicketsSeedData.StatusNewId, ct))
            errors.Add("Status 'Neu' ist nicht konfiguriert.");

        if (errors.Count > 0)
            return Result.Failure<CreateTicketResponse>(errors.ToArray());

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var ticketNumber = await _numberGenerator.GenerateAsync(ct);

            var ticket = Ticket.Create(
                title: command.Title,
                description: command.Description,
                ticketTypeId: command.TicketTypeId,
                priorityId: command.PriorityId,
                departmentId: command.DepartmentId,
                siteId: command.SiteId,
                machineOrWorkstation: command.MachineOrWorkstation,
                statusNewId: TicketsSeedData.StatusNewId,
                createdByUserId: _currentUser.UserId);

            ticket.TicketNumber = ticketNumber;

            _db.Set<Ticket>().Add(ticket);
            await _db.SaveChangesAsync(ct);

            var auditPayload = JsonSerializer.Serialize(new
            {
                ticket.TicketNumber,
                ticket.Title,
                TicketTypeId = command.TicketTypeId,
                PriorityId = command.PriorityId,
                DepartmentId = command.DepartmentId
            });

            await _auditWriter.RecordAsync(
                eventType: "TicketCreated",
                entityType: nameof(Ticket),
                entityId: ticket.Id.ToString(),
                userId: _currentUser.UserId,
                payload: auditPayload,
                ct: ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation("Ticket {TicketNumber} created by {UserId}", ticketNumber, _currentUser.UserId);

            return Result.Success(new CreateTicketResponse(ticket.Id, ticket.TicketNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ticket");
            await transaction.RollbackAsync(ct);
            return Result.Failure<CreateTicketResponse>("Ticket konnte nicht erstellt werden.");
        }
    }
}
