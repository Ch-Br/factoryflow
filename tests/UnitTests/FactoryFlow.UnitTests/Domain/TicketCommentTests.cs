using FactoryFlow.Modules.Tickets.Domain.Entities;
using FluentAssertions;

namespace FactoryFlow.UnitTests.Domain;

public class TicketCommentTests
{
    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var ticketId = Guid.NewGuid();

        var comment = TicketComment.Create(ticketId, "  Ein Kommentar  ", "user-1");

        comment.Id.Should().NotBeEmpty();
        comment.TicketId.Should().Be(ticketId);
        comment.Text.Should().Be("Ein Kommentar");
        comment.CreatedByUserId.Should().Be("user-1");
        comment.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyText_ThrowsArgumentException(string? text)
    {
        var act = () => TicketComment.Create(Guid.NewGuid(), text!, "user-1");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("text");
    }

    [Fact]
    public void Create_WithTextExceeding2000Chars_ThrowsArgumentException()
    {
        var longText = new string('x', 2001);

        var act = () => TicketComment.Create(Guid.NewGuid(), longText, "user-1");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("text");
    }

    [Fact]
    public void Create_WithEmptyTicketId_ThrowsArgumentException()
    {
        var act = () => TicketComment.Create(Guid.Empty, "Kommentar", "user-1");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("ticketId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUserId_ThrowsArgumentException(string? userId)
    {
        var act = () => TicketComment.Create(Guid.NewGuid(), "Kommentar", userId!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("createdByUserId");
    }
}
