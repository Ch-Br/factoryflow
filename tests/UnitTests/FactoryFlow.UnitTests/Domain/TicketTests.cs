using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FluentAssertions;

namespace FactoryFlow.UnitTests.Domain;

public class TicketTests
{
    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var ticket = Ticket.Create(
            title: "Fräsmaschine ausgefallen",
            description: "Spindel dreht nicht mehr, Fehlermeldung E42.",
            ticketTypeId: TicketsSeedData.TypeMachineFailureId,
            priorityId: TicketsSeedData.PriorityCriticalId,
            departmentId: Guid.NewGuid(),
            siteId: Guid.NewGuid(),
            machineOrWorkstation: "CNC-Fräse 7",
            statusNewId: TicketsSeedData.StatusNewId,
            createdByUserId: "user-123");

        ticket.Id.Should().NotBeEmpty();
        ticket.Title.Should().Be("Fräsmaschine ausgefallen");
        ticket.Description.Should().Be("Spindel dreht nicht mehr, Fehlermeldung E42.");
        ticket.StatusId.Should().Be(TicketsSeedData.StatusNewId);
        ticket.CreatedByUserId.Should().Be("user-123");
        ticket.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        ticket.MachineOrWorkstation.Should().Be("CNC-Fräse 7");
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsArgumentException()
    {
        var act = () => Ticket.Create(
            title: "  ",
            description: "Beschreibung",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: Guid.NewGuid(),
            createdByUserId: "user-123");

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Create_WithEmptyDescription_ThrowsArgumentException()
    {
        var act = () => Ticket.Create(
            title: "Titel",
            description: "",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: Guid.NewGuid(),
            createdByUserId: "user-123");

        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => Ticket.Create(
            title: "Titel",
            description: "Beschreibung",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: Guid.NewGuid(),
            createdByUserId: "");

        act.Should().Throw<ArgumentException>().WithParameterName("createdByUserId");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var ticket = Ticket.Create(
            title: "  Titel mit Whitespace  ",
            description: "  Beschreibung  ",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: "  Maschine  ",
            statusNewId: Guid.NewGuid(),
            createdByUserId: "user-1");

        ticket.Title.Should().Be("Titel mit Whitespace");
        ticket.Description.Should().Be("Beschreibung");
        ticket.MachineOrWorkstation.Should().Be("Maschine");
    }

    [Fact]
    public void Create_WithOptionalFieldsNull_Succeeds()
    {
        var ticket = Ticket.Create(
            title: "Titel",
            description: "Beschreibung",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: Guid.NewGuid(),
            createdByUserId: "user-1");

        ticket.SiteId.Should().BeNull();
        ticket.MachineOrWorkstation.Should().BeNull();
    }

    [Fact]
    public void ChangeStatus_WithDifferentStatus_UpdatesStatusId()
    {
        var originalStatusId = TicketsSeedData.StatusNewId;
        var newStatusId = TicketsSeedData.StatusInProgressId;

        var ticket = CreateValidTicket(statusNewId: originalStatusId);

        var previousStatusId = ticket.ChangeStatus(newStatusId);

        ticket.StatusId.Should().Be(newStatusId);
        previousStatusId.Should().Be(originalStatusId);
    }

    [Fact]
    public void ChangeStatus_WithSameStatus_ThrowsInvalidOperationException()
    {
        var statusId = TicketsSeedData.StatusNewId;
        var ticket = CreateValidTicket(statusNewId: statusId);

        var act = () => ticket.ChangeStatus(statusId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*identisch*");
    }

    [Fact]
    public void ChangeStatus_ReturnsPreviousStatusId()
    {
        var ticket = CreateValidTicket(statusNewId: TicketsSeedData.StatusNewId);

        var previous = ticket.ChangeStatus(TicketsSeedData.StatusInProgressId);
        previous.Should().Be(TicketsSeedData.StatusNewId);

        var previous2 = ticket.ChangeStatus(TicketsSeedData.StatusClosedId);
        previous2.Should().Be(TicketsSeedData.StatusInProgressId);

        ticket.StatusId.Should().Be(TicketsSeedData.StatusClosedId);
    }

    [Fact]
    public void Update_WithChangedValues_ReturnsTrue_AndSetsProperties()
    {
        var ticket = CreateValidTicket();
        var newPriorityId = Guid.NewGuid();

        var changed = ticket.Update("Neuer Titel", "Neue Beschreibung", newPriorityId, null);

        changed.Should().BeTrue();
        ticket.Title.Should().Be("Neuer Titel");
        ticket.Description.Should().Be("Neue Beschreibung");
        ticket.PriorityId.Should().Be(newPriorityId);
    }

    [Fact]
    public void Update_WithIdenticalValues_ReturnsFalse()
    {
        var ticket = CreateValidTicket();

        var changed = ticket.Update(ticket.Title, ticket.Description, ticket.PriorityId, ticket.DueAtUtc);

        changed.Should().BeFalse();
    }

    [Fact]
    public void Update_WithWhitespaceOnlyDifference_ReturnsFalse()
    {
        var ticket = CreateValidTicket();

        var changed = ticket.Update("  " + ticket.Title + "  ", "  " + ticket.Description + "  ", ticket.PriorityId, ticket.DueAtUtc);

        changed.Should().BeFalse();
    }

    [Fact]
    public void Update_WithEmptyTitle_ThrowsArgumentException()
    {
        var ticket = CreateValidTicket();

        var act = () => ticket.Update("  ", "Neue Beschreibung", Guid.NewGuid(), null);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Update_WithEmptyDescription_ThrowsArgumentException()
    {
        var ticket = CreateValidTicket();

        var act = () => ticket.Update("Neuer Titel", "", Guid.NewGuid(), null);

        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Create_WithValidDueAtUtc_SetsProperty()
    {
        var due = DateTime.UtcNow.AddDays(7);

        var ticket = Ticket.Create(
            title: "Titel",
            description: "Beschreibung",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: Guid.NewGuid(),
            createdByUserId: "user-1",
            dueAtUtc: due);

        ticket.DueAtUtc.Should().Be(due);
    }

    [Fact]
    public void Create_WithoutDueAtUtc_LeavesPropertyNull()
    {
        var ticket = CreateValidTicket();

        ticket.DueAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_WithDueAtUtcInPast_ThrowsArgumentException()
    {
        var act = () => Ticket.Create(
            title: "Titel",
            description: "Beschreibung",
            ticketTypeId: Guid.NewGuid(),
            priorityId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: Guid.NewGuid(),
            createdByUserId: "user-1",
            dueAtUtc: DateTime.UtcNow.AddDays(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("dueAtUtc");
    }

    [Fact]
    public void Update_WithValidDueAtUtc_SetsProperty_ReturnsTrue()
    {
        var ticket = CreateValidTicket();
        var due = DateTime.UtcNow.AddDays(5);

        var changed = ticket.Update(ticket.Title, ticket.Description, ticket.PriorityId, due);

        changed.Should().BeTrue();
        ticket.DueAtUtc.Should().Be(due);
    }

    [Fact]
    public void Update_WithDueAtUtcBeforeCreatedAtUtc_ThrowsArgumentException()
    {
        var ticket = CreateValidTicket();

        var act = () => ticket.Update(
            ticket.Title,
            ticket.Description,
            ticket.PriorityId,
            ticket.CreatedAtUtc.AddMinutes(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("dueAtUtc");
    }

    [Fact]
    public void Update_OnlyDueAtUtcChanged_ReturnsTrue()
    {
        var ticket = CreateValidTicket();
        var due = DateTime.UtcNow.AddDays(3);

        var changed = ticket.Update(ticket.Title, ticket.Description, ticket.PriorityId, due);

        changed.Should().BeTrue();
        ticket.DueAtUtc.Should().Be(due);
    }

    private static Ticket CreateValidTicket(Guid? statusNewId = null)
    {
        return Ticket.Create(
            title: "Test-Ticket",
            description: "Beschreibung",
            ticketTypeId: TicketsSeedData.TypeMachineFailureId,
            priorityId: TicketsSeedData.PriorityCriticalId,
            departmentId: Guid.NewGuid(),
            siteId: null,
            machineOrWorkstation: null,
            statusNewId: statusNewId ?? TicketsSeedData.StatusNewId,
            createdByUserId: "user-123");
    }
}
