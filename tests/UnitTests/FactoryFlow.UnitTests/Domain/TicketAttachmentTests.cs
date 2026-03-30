using FactoryFlow.Modules.Tickets.Domain.Entities;
using FluentAssertions;

namespace FactoryFlow.UnitTests.Domain;

public class TicketAttachmentTests
{
    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var ticketId = Guid.NewGuid();
        var attachment = TicketAttachment.Create(
            ticketId, "report.pdf", "application/pdf", 1024, "tickets/abc_report.pdf", "user-1");

        attachment.Id.Should().NotBeEmpty();
        attachment.TicketId.Should().Be(ticketId);
        attachment.FileName.Should().Be("report.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.FileSize.Should().Be(1024);
        attachment.StorageKey.Should().Be("tickets/abc_report.pdf");
        attachment.CreatedByUserId.Should().Be("user-1");
        attachment.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyFileName_ThrowsArgumentException()
    {
        var act = () => TicketAttachment.Create(
            Guid.NewGuid(), "  ", "application/pdf", 1024, "key", "user-1");

        act.Should().Throw<ArgumentException>().WithParameterName("fileName");
    }

    [Fact]
    public void Create_WithZeroFileSize_ThrowsArgumentException()
    {
        var act = () => TicketAttachment.Create(
            Guid.NewGuid(), "file.txt", "text/plain", 0, "key", "user-1");

        act.Should().Throw<ArgumentException>().WithParameterName("fileSize");
    }

    [Fact]
    public void Create_WithNegativeFileSize_ThrowsArgumentException()
    {
        var act = () => TicketAttachment.Create(
            Guid.NewGuid(), "file.txt", "text/plain", -1, "key", "user-1");

        act.Should().Throw<ArgumentException>().WithParameterName("fileSize");
    }

    [Fact]
    public void Create_WithEmptyStorageKey_ThrowsArgumentException()
    {
        var act = () => TicketAttachment.Create(
            Guid.NewGuid(), "file.txt", "text/plain", 1024, "", "user-1");

        act.Should().Throw<ArgumentException>().WithParameterName("storageKey");
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => TicketAttachment.Create(
            Guid.NewGuid(), "file.txt", "text/plain", 1024, "key", "");

        act.Should().Throw<ArgumentException>().WithParameterName("createdByUserId");
    }

    [Fact]
    public void Create_WithEmptyContentType_DefaultsToOctetStream()
    {
        var attachment = TicketAttachment.Create(
            Guid.NewGuid(), "file.bin", "", 1024, "key", "user-1");

        attachment.ContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public void Create_TrimsFileName()
    {
        var attachment = TicketAttachment.Create(
            Guid.NewGuid(), "  report.pdf  ", "application/pdf", 1024, "key", "user-1");

        attachment.FileName.Should().Be("report.pdf");
    }
}
