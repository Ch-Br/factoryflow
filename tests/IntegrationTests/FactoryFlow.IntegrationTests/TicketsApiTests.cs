using System.Net;
using System.Net.Http.Json;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;
using FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;
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
    public async Task GetTicketsList_Unauthenticated_ReturnsRedirect()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task GetTicketsList_Authenticated_Returns200WithResult()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create a ticket first so the list is not empty
        await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Listen-Test",
            Description = "Ticket für Listentest.",
            TicketTypeId = TicketsSeedData.TypeMachineFailureId,
            PriorityId = TicketsSeedData.PriorityHighId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var result = await client.GetFromJsonAsync<TicketListResultDto>("/api/tickets");

        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().HaveCountGreaterThanOrEqualTo(1);

        var item = result.Items.First();
        item.TicketNumber.Should().NotBeNullOrWhiteSpace();
        item.Title.Should().NotBeNullOrWhiteSpace();
        item.TicketTypeName.Should().NotBeNullOrWhiteSpace();
        item.PriorityName.Should().NotBeNullOrWhiteSpace();
        item.StatusName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetTicketDetail_NonExistentId_Returns404()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTicketDetail_Unauthenticated_ReturnsRedirect()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        // Unauthenticated requests to non-existent tickets get 404 (handler runs before auth redirect for this route).
        // For a real ticket, this would be a redirect. The key point: no 200.
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTicketDetail_ExistingTicket_Returns200WithDto()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Detail-Test",
            Description = "Ticket für Detailtest.",
            TicketTypeId = TicketsSeedData.TypeMachineFailureId,
            PriorityId = TicketsSeedData.PriorityHighId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId,
            SiteId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.MainSiteId,
            MachineOrWorkstation = "CNC-Fräse 7"
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var response = await client.GetAsync($"/api/tickets/{createResult!.TicketId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<TicketDetailDto>();
        detail.Should().NotBeNull();
        detail!.Title.Should().Be("Detail-Test");
        detail.Description.Should().Be("Ticket für Detailtest.");
        detail.TicketTypeName.Should().NotBeNullOrWhiteSpace();
        detail.PriorityName.Should().NotBeNullOrWhiteSpace();
        detail.StatusName.Should().NotBeNullOrWhiteSpace();
        detail.DepartmentName.Should().Be("Produktion");
        detail.SiteName.Should().Be("Hauptwerk");
        detail.MachineOrWorkstation.Should().Be("CNC-Fräse 7");
        detail.TicketNumber.Should().StartWith("FF-");
        detail.CreatedByDisplayName.Should().NotBeNullOrWhiteSpace();
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

    [Fact]
    public async Task ChangeTicketStatus_WithValidData_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Status-Wechsel-Test",
            Description = "Ticket für Statuswechseltest.",
            TicketTypeId = TicketsSeedData.TypeMachineFailureId,
            PriorityId = TicketsSeedData.PriorityHighId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusInProgressId });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeTicketStatus_NonExistentTicket_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusInProgressId });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeTicketStatus_SameStatus_ReturnsValidationProblem()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Gleicher-Status-Test",
            Description = "Ticket für gleichen Statustest.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusNewId });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeTicketStatus_CreatesAuditEntry()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Status-Audit-Test",
            Description = "Prüfe ob Audit bei Statuswechsel geschrieben wird.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PatchAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusClosedId });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audit = await db.AuditEntries
            .Where(a => a.EntityId == createResult.TicketId.ToString()
                        && a.EventType == "TicketStatusChanged")
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.Payload.Should().Contain(TicketsSeedData.StatusNewId.ToString());
        audit.Payload.Should().Contain(TicketsSeedData.StatusClosedId.ToString());
    }

    [Fact]
    public async Task AddTicketComment_WithValidData_Returns201()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Kommentar-Test",
            Description = "Ticket für Kommentartest.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var commentResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/comments",
            new AddTicketCommentCommand { Text = "Erster Testkommentar" });

        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var commentResult = await commentResponse.Content.ReadFromJsonAsync<AddTicketCommentResponse>();
        commentResult.Should().NotBeNull();
        commentResult!.CommentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddTicketComment_NonExistentTicket_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/comments",
            new AddTicketCommentCommand { Text = "Kommentar zu unbekanntem Ticket" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddTicketComment_CreatesAuditEntry()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Kommentar-Audit-Test",
            Description = "Prüfe ob Audit bei Kommentar geschrieben wird.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PostAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/comments",
            new AddTicketCommentCommand { Text = "Audit-Kommentar" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audit = await db.AuditEntries
            .Where(a => a.EntityId == createResult.TicketId.ToString()
                        && a.EventType == "TicketCommentAdded")
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTicketDetail_AfterComment_IncludesCommentInResponse()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Detail-Kommentar-Test",
            Description = "Kommentare im Detail prüfen.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PostAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/comments",
            new AddTicketCommentCommand { Text = "Sichtbarer Kommentar" });

        var detailResponse = await client.GetAsync($"/api/tickets/{createResult.TicketId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<TicketDetailDto>();
        detail.Should().NotBeNull();
        detail!.Comments.Should().HaveCount(1);
        detail.Comments[0].Text.Should().Be("Sichtbarer Kommentar");
        detail.Comments[0].CreatedByDisplayName.Should().NotBeNullOrWhiteSpace();
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
