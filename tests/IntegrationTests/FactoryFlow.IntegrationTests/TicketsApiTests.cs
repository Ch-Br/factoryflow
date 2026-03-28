using System.Net;
using System.Net.Http.Json;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Infrastructure.Seeds;
using FactoryFlow.Web.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryFlow.IntegrationTests;

public class TicketsApiTests : IClassFixture<FactoryFlowWebApplicationFactory>
{
    private readonly FactoryFlowWebApplicationFactory _factory;

    public TicketsApiTests(FactoryFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTicket_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Test",
            Description = "Test",
            TicketTypeId = TicketsSeedData.TypeMachineFailureId,
            PriorityId = TicketsSeedData.PriorityCriticalId,
            DepartmentId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task GetCreationLookups_ReturnsSeededData()
    {
        var client = await CreateAuthenticatedClientAsync();

        var lookups = await client.GetFromJsonAsync<TicketCreationLookupsDto>("/api/tickets/creation-lookups");

        lookups.Should().NotBeNull();
        lookups!.TicketTypes.Should().HaveCountGreaterThanOrEqualTo(5);
        lookups.Priorities.Should().HaveCountGreaterThanOrEqualTo(4);
        lookups.Departments.Should().HaveCountGreaterThanOrEqualTo(5);
        lookups.Sites.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CreateTicket_WithValidData_Returns201()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Fräsmaschine ausgefallen",
            Description = "Spindel dreht nicht mehr, Fehlermeldung E42.",
            TicketTypeId = TicketsSeedData.TypeMachineFailureId,
            PriorityId = TicketsSeedData.PriorityCriticalId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId,
            SiteId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.MainSiteId,
            MachineOrWorkstation = "CNC-Fräse 7"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateTicketResponse>();
        result.Should().NotBeNull();
        result!.TicketNumber.Should().StartWith("FF-");
        result.TicketId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTicket_CreatesAuditEntry()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Audit-Test",
            Description = "Prüfe ob AuditEntry geschrieben wird.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateTicketResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audit = await db.AuditEntries
            .Where(a => a.EntityId == result!.TicketId.ToString())
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.EventType.Should().Be("TicketCreated");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string email = "integrationtest@factoryflow.local";
        var user = await userManager.FindByNameAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User"
            };
            await userManager.CreateAsync(user, "Test123!");
        }

        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        // Sign in via cookie
        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["_model.Email"] = email,
                ["_model.Password"] = "Test123!"
            }));

        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
            }
        }

        return client;
    }
}
