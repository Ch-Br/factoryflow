using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Application.Commands.UpdateTicket;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.SharedKernel.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactoryFlow.UnitTests.Application;

public class UpdateTicketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNotAuthenticated_ReturnsFailure()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);

        var handler = CreateHandler(currentUser: currentUser);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new UpdateTicketCommand
            {
                Title = "Test",
                Description = "Test",
                PriorityId = TicketsSeedData.PriorityCriticalId
            });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nicht authentifiziert"));
    }

    private static UpdateTicketCommandHandler CreateHandler(
        ICurrentUserService? currentUser = null,
        IAuditWriter? auditWriter = null)
    {
        var dbContext = Substitute.For<DbContext>();
        currentUser ??= CreateAuthenticatedUser();
        auditWriter ??= Substitute.For<IAuditWriter>();
        var logger = Substitute.For<ILogger<UpdateTicketCommandHandler>>();

        return new UpdateTicketCommandHandler(dbContext, currentUser, auditWriter, logger);
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
