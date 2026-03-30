using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Notifications.Application;
using FactoryFlow.Modules.Tickets.Application.Commands.EscalateOverdueTickets;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.SharedKernel.Domain;
using FactoryFlow.Web.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactoryFlow.UnitTests.Application;

public class EscalateOverdueTicketsCommandHandlerTests : IDisposable
{
    private readonly FactoryFlowDbContext _db;
    private int _ticketCounter;

    public EscalateOverdueTicketsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<FactoryFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new FactoryFlowDbContext(options);
        SeedAllLookups();
    }

    [Fact]
    public async Task HandleAsync_WhenNotAuthenticated_ReturnsFailure()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);

        var handler = CreateHandler(currentUser: currentUser);

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nicht authentifiziert"));
    }

    [Fact]
    public async Task HandleAsync_WhenUserRole_ReturnsFailure()
    {
        var currentUser = CreateAuthenticatedUser(AppRoles.User);

        var handler = CreateHandler(currentUser: currentUser);

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Keine Berechtigung"));
    }

    [Fact]
    public async Task HandleAsync_NoCandidates_ReturnsZero()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Kein DueDate", DateTime.UtcNow.AddDays(-1)));
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_OverdueOpenTickets_EscalatesAll()
    {
        _db.Set<Ticket>().AddRange(
            CreateTicket("Ticket A", DateTime.UtcNow.AddDays(-2),
                dueAtUtc: DateTime.UtcNow.AddHours(-5)),
            CreateTicket("Ticket B", DateTime.UtcNow.AddDays(-3),
                dueAtUtc: DateTime.UtcNow.AddHours(-10)));
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(2);

        var tickets = await _db.Set<Ticket>().ToListAsync();
        tickets.Should().AllSatisfy(t =>
        {
            t.EscalationLevel.Should().Be(1);
            t.FirstEscalatedAtUtc.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task HandleAsync_AlreadyEscalated_IsSkipped()
    {
        var ticket = CreateTicket("Bereits eskaliert", DateTime.UtcNow.AddDays(-2),
            dueAtUtc: DateTime.UtcNow.AddHours(-5));
        ticket.Escalate(DateTime.UtcNow.AddHours(-1));

        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ClosedTicket_IsSkipped()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Geschlossen", DateTime.UtcNow.AddDays(-2),
                statusId: TicketsSeedData.StatusClosedId,
                dueAtUtc: DateTime.UtcNow.AddHours(-5)));
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_TicketWithoutDueDate_IsSkipped()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Ohne Fälligkeit", DateTime.UtcNow.AddDays(-2)));
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_FutureDueDate_IsSkipped()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Noch nicht fällig", DateTime.UtcNow.AddDays(-1),
                dueAtUtc: DateTime.UtcNow.AddDays(5)));
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WritesAuditEntries()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Audit-Test", DateTime.UtcNow.AddDays(-2),
                dueAtUtc: DateTime.UtcNow.AddHours(-3)));
        await _db.SaveChangesAsync();

        var auditWriter = Substitute.For<IAuditWriter>();
        var handler = CreateHandler(auditWriter: auditWriter);

        await handler.HandleAsync();

        await auditWriter.Received(1).RecordAsync(
            eventType: "TicketEscalated",
            entityType: "Ticket",
            entityId: Arg.Any<string>(),
            userId: "user-123",
            payload: Arg.Any<string>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PublishesNotifications()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Notification-Test", DateTime.UtcNow.AddDays(-2),
                dueAtUtc: DateTime.UtcNow.AddHours(-3)));
        await _db.SaveChangesAsync();

        var publisher = Substitute.For<IEscalationNotificationPublisher>();
        var handler = CreateHandler(notificationPublisher: publisher);

        await handler.HandleAsync();

        await publisher.Received(1).PublishAsync(
            Arg.Is<IReadOnlyList<EscalatedTicketInfo>>(list =>
                list.Count == 1
                && list[0].Title == "Notification-Test"
                && list[0].EscalationLevel == 1),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoCandidates_DoesNotPublishNotifications()
    {
        _db.Set<Ticket>().Add(
            CreateTicket("Kein DueDate", DateTime.UtcNow.AddDays(-1)));
        await _db.SaveChangesAsync();

        var publisher = Substitute.For<IEscalationNotificationPublisher>();
        var handler = CreateHandler(notificationPublisher: publisher);

        await handler.HandleAsync();

        await publisher.DidNotReceive().PublishAsync(
            Arg.Any<IReadOnlyList<EscalatedTicketInfo>>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MixedTickets_OnlyEscalatesQualifying()
    {
        _db.Set<Ticket>().AddRange(
            CreateTicket("Overdue+Open", DateTime.UtcNow.AddDays(-3),
                dueAtUtc: DateTime.UtcNow.AddHours(-5)),
            CreateTicket("Closed+Overdue", DateTime.UtcNow.AddDays(-3),
                statusId: TicketsSeedData.StatusClosedId,
                dueAtUtc: DateTime.UtcNow.AddHours(-5)),
            CreateTicket("Open+NoDue", DateTime.UtcNow.AddDays(-1)),
            CreateTicket("Open+FutureDue", DateTime.UtcNow.AddDays(-1),
                dueAtUtc: DateTime.UtcNow.AddDays(7)));
        await _db.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.HandleAsync();

        result.Succeeded.Should().BeTrue();
        result.Value!.EscalatedCount.Should().Be(1);

        var escalatedTicket = await _db.Set<Ticket>()
            .SingleAsync(t => t.EscalationLevel == 1);
        escalatedTicket.Title.Should().Be("Overdue+Open");
    }

    private EscalateOverdueTicketsCommandHandler CreateHandler(
        ICurrentUserService? currentUser = null,
        IAuditWriter? auditWriter = null,
        IEscalationNotificationPublisher? notificationPublisher = null)
    {
        currentUser ??= CreateAuthenticatedUser(AppRoles.Supervisor);
        auditWriter ??= Substitute.For<IAuditWriter>();
        notificationPublisher ??= Substitute.For<IEscalationNotificationPublisher>();
        var logger = Substitute.For<ILogger<EscalateOverdueTicketsCommandHandler>>();

        return new EscalateOverdueTicketsCommandHandler(_db, currentUser, auditWriter, notificationPublisher, logger);
    }

    private static ICurrentUserService CreateAuthenticatedUser(string role)
    {
        var user = Substitute.For<ICurrentUserService>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns("user-123");
        user.UserName.Returns("testuser");
        user.IsInRole(role).Returns(true);
        return user;
    }

    private void SeedAllLookups()
    {
        _db.Set<TicketType>().Add(new TicketType(
            TicketsSeedData.TypeMachineFailureId, "Maschinenstörung", "machine_failure"));

        _db.Set<TicketPriority>().AddRange(
            new TicketPriority(TicketsSeedData.PriorityCriticalId, "Kritisch", "critical", 1),
            new TicketPriority(TicketsSeedData.PriorityHighId, "Hoch", "high", 2));

        _db.Set<TicketStatus>().AddRange(
            new TicketStatus(TicketsSeedData.StatusNewId, "Neu", "new", 1),
            new TicketStatus(TicketsSeedData.StatusInProgressId, "In Bearbeitung", "in_progress", 2),
            new TicketStatus(TicketsSeedData.StatusClosedId, "Geschlossen", "closed", 3));

        _db.SaveChanges();
    }

    private Ticket CreateTicket(
        string title,
        DateTime createdAtUtc,
        Guid? statusId = null,
        Guid? priorityId = null,
        DateTime? dueAtUtc = null)
    {
        _ticketCounter++;
        var ticket = Ticket.Create(
            title: title,
            description: "Testbeschreibung",
            ticketTypeId: TicketsSeedData.TypeMachineFailureId,
            priorityId: priorityId ?? TicketsSeedData.PriorityCriticalId,
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: statusId ?? TicketsSeedData.StatusNewId,
            createdByUserId: "test-user");

        typeof(Ticket)
            .GetProperty(nameof(Ticket.TicketNumber))!
            .SetValue(ticket, $"FF-ESC-{_ticketCounter:D3}");

        typeof(Ticket).BaseType!
            .GetProperty(nameof(Ticket.CreatedAtUtc))!
            .SetValue(ticket, createdAtUtc);

        if (dueAtUtc.HasValue)
        {
            typeof(Ticket)
                .GetProperty(nameof(Ticket.DueAtUtc))!
                .SetValue(ticket, dueAtUtc.Value);
        }

        return ticket;
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
