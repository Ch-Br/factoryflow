using FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.Web.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.UnitTests.Application;

public class GetOverdueTicketsQueryHandlerTests : IDisposable
{
    private readonly FactoryFlowDbContext _db;
    private int _ticketCounter;

    public GetOverdueTicketsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<FactoryFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new FactoryFlowDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_NoOverdueTickets_ReturnsEmptyList()
    {
        SeedAllLookups();

        _db.Set<Ticket>().AddRange(
            CreateTicket("Ohne DueDate", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateTicket("In der Zukunft", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                dueAtUtc: DateTime.UtcNow.AddDays(30)));
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_OverdueTicket_ReturnsIt()
    {
        SeedAllLookups();

        var ticket = CreateTicket("Ueberfaellig", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            dueAtUtc: DateTime.UtcNow.AddHours(-5));
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        var item = result.Items.Should().ContainSingle().Subject;
        item.Title.Should().Be("Ueberfaellig");
        item.TicketNumber.Should().NotBeNullOrWhiteSpace();
        item.TicketTypeName.Should().Be("Maschinenstörung");
        item.PriorityName.Should().Be("Kritisch");
        item.StatusName.Should().Be("Neu");
    }

    [Fact]
    public async Task HandleAsync_ClosedOverdueTicket_IsExcluded()
    {
        SeedAllLookups();

        _db.Set<Ticket>().AddRange(
            CreateTicket("Offen+Overdue", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                dueAtUtc: DateTime.UtcNow.AddHours(-5)),
            CreateTicket("Geschlossen+Overdue", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusClosedId,
                dueAtUtc: DateTime.UtcNow.AddHours(-3)));
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Offen+Overdue");
    }

    [Fact]
    public async Task HandleAsync_TicketWithoutDueDate_IsExcluded()
    {
        SeedAllLookups();

        _db.Set<Ticket>().AddRange(
            CreateTicket("Mit DueDate", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                dueAtUtc: DateTime.UtcNow.AddHours(-2)),
            CreateTicket("Ohne DueDate", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Mit DueDate");
    }

    [Fact]
    public async Task HandleAsync_SortsByDueAtUtcAscending()
    {
        SeedAllLookups();

        var recentlyOverdue = DateTime.UtcNow.AddHours(-1);
        var longOverdue = DateTime.UtcNow.AddDays(-3);

        _db.Set<Ticket>().AddRange(
            CreateTicket("Kurz ueberfaellig", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                dueAtUtc: recentlyOverdue),
            CreateTicket("Lange ueberfaellig", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                dueAtUtc: longOverdue));
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("Lange ueberfaellig");
        result.Items[1].Title.Should().Be("Kurz ueberfaellig");
    }

    [Fact]
    public async Task HandleAsync_CalculatesOverdueByCorrectly()
    {
        SeedAllLookups();

        var dueDate = DateTime.UtcNow.AddHours(-6);
        var ticket = CreateTicket("OverdueBy-Test", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            dueAtUtc: dueDate);
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);

        var result = await handler.HandleAsync();

        var item = result.Items.Should().ContainSingle().Subject;
        item.DueAtUtc.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(2));
        item.OverdueBy.Should().BeGreaterThan(TimeSpan.FromHours(5));
        item.OverdueBy.Should().BeLessThan(TimeSpan.FromHours(7));
    }

    [Fact]
    public async Task HandleAsync_FilterByPriorityId_ReturnsOnlyMatchingPriority()
    {
        SeedAllLookups();

        _db.Set<Ticket>().AddRange(
            CreateTicket("Kritisch-Overdue", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                dueAtUtc: DateTime.UtcNow.AddHours(-3)),
            CreateTicket("Hoch-Overdue", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                priorityId: TicketsSeedData.PriorityHighId,
                dueAtUtc: DateTime.UtcNow.AddHours(-1)));
        await _db.SaveChangesAsync();

        var handler = new GetOverdueTicketsQueryHandler(_db);
        var result = await handler.HandleAsync(
            new GetOverdueTicketsQuery(PriorityId: TicketsSeedData.PriorityHighId));

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Hoch-Overdue");
    }

    private void SeedAllLookups()
    {
        if (!_db.Set<TicketType>().Any())
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
            .SetValue(ticket, $"FF-TEST-{_ticketCounter:D3}");

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
