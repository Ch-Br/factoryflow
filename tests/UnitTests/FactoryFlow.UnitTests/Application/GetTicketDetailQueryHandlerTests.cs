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
