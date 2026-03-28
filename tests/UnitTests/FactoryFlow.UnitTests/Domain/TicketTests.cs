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
}
