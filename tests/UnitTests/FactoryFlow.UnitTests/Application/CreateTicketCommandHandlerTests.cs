using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.Modules.Tickets.Domain.Services;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.SharedKernel.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactoryFlow.UnitTests.Application;

public class CreateTicketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNotAuthenticated_ReturnsFailure()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);

        var handler = CreateHandler(currentUser: currentUser);

        var result = await handler.HandleAsync(ValidCommand());

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nicht authentifiziert"));
    }

    private static CreateTicketCommand ValidCommand() => new()
    {
        Title = "Test-Ticket",
        Description = "Beschreibung des Problems",
        TicketTypeId = TicketsSeedData.TypeMachineFailureId,
        PriorityId = TicketsSeedData.PriorityCriticalId,
        DepartmentId = Guid.NewGuid()
    };

    private static CreateTicketCommandHandler CreateHandler(
        ICurrentUserService? currentUser = null,
        ITicketNumberGenerator? numberGenerator = null,
        IAuditWriter? auditWriter = null)
    {
        var dbContext = Substitute.For<DbContext>();
        currentUser ??= CreateAuthenticatedUser();
        numberGenerator ??= Substitute.For<ITicketNumberGenerator>();
        numberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
            .Returns("FF-2026-000001");
        auditWriter ??= Substitute.For<IAuditWriter>();
        var logger = Substitute.For<ILogger<CreateTicketCommandHandler>>();

        return new CreateTicketCommandHandler(dbContext, currentUser, numberGenerator, auditWriter, logger);
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
