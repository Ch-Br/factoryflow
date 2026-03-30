using FactoryFlow.Modules.Tickets.Domain.Enums;
using FactoryFlow.Modules.Tickets.Domain.Services;
using FluentAssertions;

namespace FactoryFlow.UnitTests.Domain;

public class DueStateCalculatorTests
{
    private static readonly DateTime ReferenceNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Calculate_NoDueDate_ReturnsNoDueDate()
    {
        var result = DueStateCalculator.Calculate(null, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.NoDueDate);
    }

    [Fact]
    public void Calculate_NoDueDate_ClosedTicket_ReturnsNoDueDate()
    {
        var result = DueStateCalculator.Calculate(null, isClosed: true, ReferenceNow);

        result.Should().Be(DueState.NoDueDate);
    }

    [Fact]
    public void Calculate_ClosedTicket_WithOverdueDueDate_ReturnsOnTrack()
    {
        var pastDue = ReferenceNow.AddHours(-5);

        var result = DueStateCalculator.Calculate(pastDue, isClosed: true, ReferenceNow);

        result.Should().Be(DueState.OnTrack);
    }

    [Fact]
    public void Calculate_ClosedTicket_WithFutureDueDate_ReturnsOnTrack()
    {
        var futureDue = ReferenceNow.AddDays(7);

        var result = DueStateCalculator.Calculate(futureDue, isClosed: true, ReferenceNow);

        result.Should().Be(DueState.OnTrack);
    }

    [Fact]
    public void Calculate_PastDueDate_NotClosed_ReturnsOverdue()
    {
        var pastDue = ReferenceNow.AddHours(-1);

        var result = DueStateCalculator.Calculate(pastDue, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.Overdue);
    }

    [Fact]
    public void Calculate_DueExactlyNow_ReturnsOverdue()
    {
        var result = DueStateCalculator.Calculate(ReferenceNow, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.Overdue);
    }

    [Fact]
    public void Calculate_DueWithin24Hours_ReturnsDueSoon()
    {
        var dueSoon = ReferenceNow.AddHours(12);

        var result = DueStateCalculator.Calculate(dueSoon, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.DueSoon);
    }

    [Fact]
    public void Calculate_DueExactlyAtThreshold_ReturnsDueSoon()
    {
        var dueAtThreshold = ReferenceNow + DueStateCalculator.DueSoonThreshold;

        var result = DueStateCalculator.Calculate(dueAtThreshold, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.DueSoon);
    }

    [Fact]
    public void Calculate_DueOneSecondPastThreshold_ReturnsOnTrack()
    {
        var justPastThreshold = ReferenceNow + DueStateCalculator.DueSoonThreshold + TimeSpan.FromSeconds(1);

        var result = DueStateCalculator.Calculate(justPastThreshold, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.OnTrack);
    }

    [Fact]
    public void Calculate_DueFarInFuture_ReturnsOnTrack()
    {
        var farFuture = ReferenceNow.AddDays(30);

        var result = DueStateCalculator.Calculate(farFuture, isClosed: false, ReferenceNow);

        result.Should().Be(DueState.OnTrack);
    }
}
