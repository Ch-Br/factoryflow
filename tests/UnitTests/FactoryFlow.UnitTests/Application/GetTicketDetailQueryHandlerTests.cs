using System.Text.Json;
using FactoryFlow.Modules.Audit.Domain.Entities;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.Web.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FactoryFlow.UnitTests.Application;

public class GetTicketDetailQueryHandlerTests : IDisposable
{
    private readonly FactoryFlowDbContext _db;

    public GetTicketDetailQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<FactoryFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new FactoryFlowDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_NonExistentId_ReturnsNull()
    {
        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ExistingTicket_ProjectsAllFields()
    {
        SeedLookups();
        var dept = new Department(Guid.NewGuid(), "Produktion", "PROD");
        _db.Set<Department>().Add(dept);

        var site = new Site(Guid.NewGuid(), "Hauptwerk", "HW");
        _db.Set<Site>().Add(site);

        var ticket = CreateTicket(
            "Testticket",
            "Detailbeschreibung",
            dept.Id,
            site.Id,
            "CNC-Fräse 7",
            new DateTime(2026, 3, 1, 14, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(ticket.Id);
        result.TicketNumber.Should().Be("FF-TEST-001");
        result.Title.Should().Be("Testticket");
        result.Description.Should().Be("Detailbeschreibung");
        result.TicketTypeName.Should().Be("Maschinenstörung");
        result.PriorityName.Should().Be("Kritisch");
        result.StatusName.Should().Be("Neu");
        result.DepartmentName.Should().Be("Produktion");
        result.SiteName.Should().Be("Hauptwerk");
        result.MachineOrWorkstation.Should().Be("CNC-Fräse 7");
        result.CreatedAtUtc.Should().Be(new DateTime(2026, 3, 1, 14, 0, 0, DateTimeKind.Utc));
        result.Comments.Should().BeEmpty();
        result.History.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithUser_ProjectsDisplayName()
    {
        SeedLookups();

        var user = new ApplicationUser
        {
            Id = "user-42",
            UserName = "max@example.com",
            FirstName = "Max",
            LastName = "Mustermann"
        };
        _db.Set<ApplicationUser>().Add(user);

        var ticket = CreateTicket(
            "User-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc),
            createdByUserId: "user-42");
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.CreatedByDisplayName.Should().Be("Max Mustermann");
    }

    [Fact]
    public async Task HandleAsync_WithDeletedUser_FallsBackToUserId()
    {
        SeedLookups();

        var ticket = CreateTicket(
            "Orphan-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc),
            createdByUserId: "deleted-user-99");
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.CreatedByDisplayName.Should().Be("deleted-user-99");
    }

    [Fact]
    public async Task HandleAsync_TicketWithNoComments_ReturnsEmptyCommentsList()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Ohne Kommentare", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_TicketWithComments_ReturnsCommentsSortedDescending()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Mit Kommentaren", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var olderComment = TicketComment.Create(ticket.Id, "Erster Kommentar", "user-1");
        SetCreatedAtUtc(olderComment, new DateTime(2026, 3, 10, 13, 0, 0, DateTimeKind.Utc));

        var newerComment = TicketComment.Create(ticket.Id, "Zweiter Kommentar", "user-2");
        SetCreatedAtUtc(newerComment, new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc));

        _db.Set<TicketComment>().AddRange(olderComment, newerComment);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.Comments.Should().HaveCount(2);
        result.Comments[0].Text.Should().Be("Zweiter Kommentar");
        result.Comments[1].Text.Should().Be("Erster Kommentar");
    }

    [Fact]
    public async Task HandleAsync_CommentWithUser_ProjectsDisplayName()
    {
        SeedLookups();
        var user = new ApplicationUser
        {
            Id = "comment-user",
            UserName = "anna@example.com",
            FirstName = "Anna",
            LastName = "Schmidt"
        };
        _db.Set<ApplicationUser>().Add(user);

        var ticket = CreateTicket(
            "Kommentar-User-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var comment = TicketComment.Create(ticket.Id, "Kommentar von Anna", "comment-user");
        _db.Set<TicketComment>().Add(comment);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.Comments.Should().HaveCount(1);
        result.Comments[0].CreatedByDisplayName.Should().Be("Anna Schmidt");
    }

    [Fact]
    public async Task HandleAsync_CommentWithDeletedUser_FallsBackToUserId()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Kommentar-Fallback-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 11, 11, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var comment = TicketComment.Create(ticket.Id, "Orphan-Kommentar", "deleted-user-77");
        _db.Set<TicketComment>().Add(comment);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.Comments.Should().HaveCount(1);
        result.Comments[0].CreatedByDisplayName.Should().Be("deleted-user-77");
    }

    [Fact]
    public async Task HandleAsync_NoAuditEntries_ReturnsEmptyHistory()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Kein Verlauf", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.History.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithAuditEntries_ReturnsHistorySortedDescending()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Verlauf-Sort-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var older = new AuditEntry("TicketCreated", "Ticket", ticket.Id.ToString(), "user-1");
        SetAuditOccurredAt(older, new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));

        var newer = new AuditEntry(
            "TicketStatusChanged", "Ticket", ticket.Id.ToString(), "user-1",
            JsonSerializer.Serialize(new { PreviousStatusId = TicketsSeedData.StatusNewId, NewStatusId = TicketsSeedData.StatusInProgressId }));
        SetAuditOccurredAt(newer, new DateTime(2026, 3, 20, 11, 0, 0, DateTimeKind.Utc));

        _db.Set<AuditEntry>().AddRange(older, newer);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result.Should().NotBeNull();
        result!.History.Should().HaveCount(2);
        result.History[0].EventType.Should().Be("TicketStatusChanged");
        result.History[1].EventType.Should().Be("TicketCreated");
    }

    [Fact]
    public async Task HandleAsync_TicketCreatedEvent_MapsCorrectly()
    {
        SeedLookups();
        var user = new ApplicationUser
        {
            Id = "history-user-1",
            UserName = "hans@example.com",
            FirstName = "Hans",
            LastName = "Meier"
        };
        _db.Set<ApplicationUser>().Add(user);

        var ticket = CreateTicket(
            "Verlauf-Created-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc),
            createdByUserId: "history-user-1");
        _db.Set<Ticket>().Add(ticket);

        var entry = new AuditEntry("TicketCreated", "Ticket", ticket.Id.ToString(), "history-user-1");
        SetAuditOccurredAt(entry, new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<AuditEntry>().Add(entry);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result!.History.Should().HaveCount(1);
        var item = result.History[0];
        item.EventType.Should().Be("TicketCreated");
        item.EventLabel.Should().Be("Ticket erstellt");
        item.Text.Should().Be("Ticket erstellt");
        item.ActorDisplayName.Should().Be("Hans Meier");
    }

    [Fact]
    public async Task HandleAsync_StatusChangedEvent_ResolvesStatusNames()
    {
        SeedLookups();
        SeedAdditionalStatuses();

        var ticket = CreateTicket(
            "Verlauf-Status-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var payload = JsonSerializer.Serialize(new
        {
            PreviousStatusId = TicketsSeedData.StatusNewId,
            NewStatusId = TicketsSeedData.StatusInProgressId
        });
        var entry = new AuditEntry("TicketStatusChanged", "Ticket", ticket.Id.ToString(), "test-user", payload);
        SetAuditOccurredAt(entry, new DateTime(2026, 3, 20, 11, 0, 0, DateTimeKind.Utc));
        _db.Set<AuditEntry>().Add(entry);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result!.History.Should().HaveCount(1);
        var item = result.History[0];
        item.EventLabel.Should().Be("Status geändert");
        item.Text.Should().Contain("Neu");
        item.Text.Should().Contain("In Bearbeitung");
        item.Text.Should().Contain("\u2192");
    }

    [Fact]
    public async Task HandleAsync_CommentAddedEvent_ShowsCommentText()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Verlauf-Kommentar-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var comment = TicketComment.Create(ticket.Id, "Das ist ein Testkommentar", "test-user");
        _db.Set<TicketComment>().Add(comment);

        var payload = JsonSerializer.Serialize(new { CommentId = comment.Id, TextLength = 25 });
        var entry = new AuditEntry("TicketCommentAdded", "Ticket", ticket.Id.ToString(), "test-user", payload);
        SetAuditOccurredAt(entry, new DateTime(2026, 3, 20, 12, 0, 0, DateTimeKind.Utc));
        _db.Set<AuditEntry>().Add(entry);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result!.History.Should().HaveCount(1);
        result.History[0].Text.Should().Be("Das ist ein Testkommentar");
    }

    [Fact]
    public async Task HandleAsync_HistoryWithDeletedUser_FallsBackToUserId()
    {
        SeedLookups();
        var ticket = CreateTicket(
            "Verlauf-Fallback-Test", "Beschreibung", Guid.NewGuid(), null, null,
            new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);

        var entry = new AuditEntry("TicketCreated", "Ticket", ticket.Id.ToString(), "ghost-user-99");
        SetAuditOccurredAt(entry, new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<AuditEntry>().Add(entry);
        await _db.SaveChangesAsync();

        var handler = new GetTicketDetailQueryHandler(_db);

        var result = await handler.HandleAsync(ticket.Id);

        result!.History.Should().HaveCount(1);
        result.History[0].ActorDisplayName.Should().Be("ghost-user-99");
    }

    private static void SetAuditOccurredAt(AuditEntry entry, DateTime value)
    {
        typeof(AuditEntry)
            .GetProperty(nameof(AuditEntry.OccurredAtUtc))!
            .SetValue(entry, value);
    }

    private void SeedAdditionalStatuses()
    {
        if (!_db.Set<TicketStatus>().Any(s => s.Id == TicketsSeedData.StatusInProgressId))
        {
            _db.Set<TicketStatus>().Add(new TicketStatus(
                TicketsSeedData.StatusInProgressId, "In Bearbeitung", "in_progress", 2));
            _db.Set<TicketStatus>().Add(new TicketStatus(
                TicketsSeedData.StatusClosedId, "Geschlossen", "closed", 3));
            _db.SaveChanges();
        }
    }

    private static void SetCreatedAtUtc(TicketComment comment, DateTime value)
    {
        typeof(TicketComment).BaseType!
            .GetProperty(nameof(TicketComment.CreatedAtUtc))!
            .SetValue(comment, value);
    }

    private void SeedLookups()
    {
        if (!_db.Set<TicketType>().Any())
        {
            _db.Set<TicketType>().Add(new TicketType(
                TicketsSeedData.TypeMachineFailureId, "Maschinenstörung", "machine_failure"));
            _db.Set<TicketPriority>().Add(new TicketPriority(
                TicketsSeedData.PriorityCriticalId, "Kritisch", "critical", 1));
            _db.Set<TicketStatus>().Add(new TicketStatus(
                TicketsSeedData.StatusNewId, "Neu", "new", 1));
            _db.SaveChanges();
        }
    }

    private static Ticket CreateTicket(
        string title,
        string description,
        Guid departmentId,
        Guid? siteId,
        string? machineOrWorkstation,
        DateTime createdAtUtc,
        string createdByUserId = "test-user")
    {
        var ticket = Ticket.Create(
            title: title,
            description: description,
            ticketTypeId: TicketsSeedData.TypeMachineFailureId,
            priorityId: TicketsSeedData.PriorityCriticalId,
            departmentId: departmentId,
            siteId: siteId,
            machineOrWorkstation: machineOrWorkstation,
            statusNewId: TicketsSeedData.StatusNewId,
            createdByUserId: createdByUserId);

        typeof(Ticket)
            .GetProperty(nameof(Ticket.TicketNumber))!
            .SetValue(ticket, "FF-TEST-001");

        typeof(Ticket).BaseType!
            .GetProperty(nameof(Ticket.CreatedAtUtc))!
            .SetValue(ticket, createdAtUtc);

        return ticket;
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
