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

    private static Ticket CreateTicket(string title, DateTime createdAtUtc)
    {
        var ticket = Ticket.Create(
            title: title,
            description: "Testbeschreibung",
            ticketTypeId: TicketsSeedData.TypeMachineFailureId,
            priorityId: TicketsSeedData.PriorityCriticalId,
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: TicketsSeedData.StatusNewId,
            createdByUserId: "test-user");

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
