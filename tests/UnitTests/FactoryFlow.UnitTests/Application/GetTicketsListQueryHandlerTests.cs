using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.Web.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.UnitTests.Application;

public class GetTicketsListQueryHandlerTests : IDisposable
{
    private readonly FactoryFlowDbContext _db;
    private int _ticketCounter;

    public GetTicketsListQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<FactoryFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new FactoryFlowDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var handler = new GetTicketsListQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_MultipleTickets_ReturnsSortedByCreatedAtDescending()
    {
        SeedLookups();
        var older = CreateTicket("Altes Ticket", new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        var newer = CreateTicket("Neues Ticket", new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().AddRange(older, newer);
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);

        var result = await handler.HandleAsync();

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].Title.Should().Be("Neues Ticket");
        result.Items[1].Title.Should().Be("Altes Ticket");
    }

    [Fact]
    public async Task HandleAsync_ProjectsNavigationPropertyNames()
    {
        SeedLookups();
        var ticket = CreateTicket("Projektions-Test", new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        _db.Set<Ticket>().Add(ticket);
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);

        var result = await handler.HandleAsync();

        var item = result.Items.Should().ContainSingle().Subject;
        item.Id.Should().NotBeEmpty();
        item.TicketNumber.Should().Be("FF-TEST-001");
        item.Title.Should().Be("Projektions-Test");
        item.TicketTypeName.Should().Be("Maschinenstörung");
        item.PriorityName.Should().Be("Kritisch");
        item.StatusName.Should().Be("Neu");
        item.CreatedAtUtc.Should().Be(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        item.DueAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsAllTickets()
    {
        SeedAllLookups();
        _db.Set<Ticket>().AddRange(
            CreateTicket("T1", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateTicket("T2", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusClosedId, priorityId: TicketsSeedData.PriorityHighId),
            CreateTicket("T3", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusInProgressId));
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);
        var result = await handler.HandleAsync(new GetTicketsListQuery());

        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task HandleAsync_FilterByStatusId_ReturnsOnlyMatchingStatus()
    {
        SeedAllLookups();
        _db.Set<Ticket>().AddRange(
            CreateTicket("Neu1", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateTicket("InProgress1", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusInProgressId),
            CreateTicket("Closed1", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusClosedId));
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);
        var result = await handler.HandleAsync(
            new GetTicketsListQuery(StatusId: TicketsSeedData.StatusInProgressId));

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("InProgress1");
    }

    [Fact]
    public async Task HandleAsync_FilterByPriorityId_ReturnsOnlyMatchingPriority()
    {
        SeedAllLookups();
        _db.Set<Ticket>().AddRange(
            CreateTicket("Kritisch1", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateTicket("Hoch1", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                priorityId: TicketsSeedData.PriorityHighId));
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);
        var result = await handler.HandleAsync(
            new GetTicketsListQuery(PriorityId: TicketsSeedData.PriorityHighId));

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Hoch1");
    }

    [Fact]
    public async Task HandleAsync_OnlyOpen_ExcludesClosedTickets()
    {
        SeedAllLookups();
        _db.Set<Ticket>().AddRange(
            CreateTicket("Offen1", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateTicket("InProgress1", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusInProgressId),
            CreateTicket("Geschlossen1", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusClosedId));
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);
        var result = await handler.HandleAsync(new GetTicketsListQuery(OnlyOpen: true));

        result.Items.Should().HaveCount(2);
        result.Items.Should().NotContain(i => i.Title == "Geschlossen1");
    }

    [Fact]
    public async Task HandleAsync_CombinedFilters_ReturnsIntersection()
    {
        SeedAllLookups();
        _db.Set<Ticket>().AddRange(
            CreateTicket("Match", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusInProgressId, priorityId: TicketsSeedData.PriorityHighId),
            CreateTicket("WrongStatus", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusNewId, priorityId: TicketsSeedData.PriorityHighId),
            CreateTicket("WrongPriority", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusInProgressId, priorityId: TicketsSeedData.PriorityCriticalId),
            CreateTicket("Closed", new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                statusId: TicketsSeedData.StatusClosedId, priorityId: TicketsSeedData.PriorityHighId));
        await _db.SaveChangesAsync();

        var handler = new GetTicketsListQueryHandler(_db);
        var result = await handler.HandleAsync(new GetTicketsListQuery(
            StatusId: TicketsSeedData.StatusInProgressId,
            PriorityId: TicketsSeedData.PriorityHighId,
            OnlyOpen: true));

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Match");
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
        Guid? priorityId = null)
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

        return ticket;
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
