using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketAttachment;
using FactoryFlow.SharedKernel.Domain;
using FactoryFlow.SharedKernel.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactoryFlow.UnitTests.Application;

public class AddTicketAttachmentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNotAuthenticated_ReturnsFailure()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);

        var handler = CreateHandler(currentUser: currentUser);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new AddTicketAttachmentCommand
            {
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                Content = new MemoryStream([1, 2, 3])
            });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nicht authentifiziert"));
    }

    private static AddTicketAttachmentCommandHandler CreateHandler(
        ICurrentUserService? currentUser = null,
        IAuditWriter? auditWriter = null,
        IFileStorage? fileStorage = null)
    {
        var dbContext = Substitute.For<DbContext>();
        currentUser ??= CreateAuthenticatedUser();
        auditWriter ??= Substitute.For<IAuditWriter>();
        fileStorage ??= Substitute.For<IFileStorage>();
        var configuration = new ConfigurationBuilder().Build();
        var logger = Substitute.For<ILogger<AddTicketAttachmentCommandHandler>>();

        return new AddTicketAttachmentCommandHandler(
            dbContext, currentUser, auditWriter, fileStorage, configuration, logger);
    }

    private static ICurrentUserService CreateAuthenticatedUser()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns("user-123");
        user.UserName.Returns("testuser");
        return user;
    }
}
