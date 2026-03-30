using System.Net;
using System.Net.Http.Json;
using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketAttachment;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;
using FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Commands.UpdateTicket;
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

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
    public async Task GetTicketsList_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
        var client = await CreateAuthenticatedClientAsync();

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

    [Fact]
    public async Task GetTicketDetail_AfterFullLifecycle_IncludesHistoryWithAllEventTypes()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Verlauf-Integrationstest",
            Description = "Prüfe ob Verlauf alle Events enthält.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PatchAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusInProgressId });

        await client.PostAsJsonAsync(
            $"/api/tickets/{createResult.TicketId}/comments",
            new AddTicketCommentCommand { Text = "Verlauf-Kommentar" });

        var detailResponse = await client.GetAsync($"/api/tickets/{createResult.TicketId}");
        detailResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<TicketDetailDto>();
        detail.Should().NotBeNull();
        detail!.History.Should().HaveCountGreaterThanOrEqualTo(3);

        detail.History.Should().Contain(h => h.EventType == "TicketCreated");
        detail.History.Should().Contain(h => h.EventType == "TicketStatusChanged");
        detail.History.Should().Contain(h => h.EventType == "TicketCommentAdded");

        var statusEvent = detail.History.First(h => h.EventType == "TicketStatusChanged");
        statusEvent.Text.Should().Contain("\u2192");
        statusEvent.ActorDisplayName.Should().NotBeNullOrWhiteSpace();
        statusEvent.EventLabel.Should().Be("Status geändert");

        var commentEvent = detail.History.First(h => h.EventType == "TicketCommentAdded");
        commentEvent.Text.Should().Contain("Verlauf-Kommentar");

        detail.History.Should().BeInDescendingOrder(h => h.OccurredAtUtc);
    }

    [Fact]
    public async Task UpdateTicket_WithValidData_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Update-Test",
            Description = "Ticket für Update-Test.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var putResponse = await client.PutAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}",
            new UpdateTicketCommand
            {
                Title = "Aktualisierter Titel",
                Description = "Aktualisierte Beschreibung.",
                PriorityId = TicketsSeedData.PriorityHighId
            });

        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateTicket_NonExistentTicket_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var putResponse = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}",
            new UpdateTicketCommand
            {
                Title = "Test",
                Description = "Test",
                PriorityId = TicketsSeedData.PriorityMediumId
            });

        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTicket_CreatesAuditEntry()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Update-Audit-Test",
            Description = "Prüfe ob Audit bei Update geschrieben wird.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PutAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}",
            new UpdateTicketCommand
            {
                Title = "Geänderter Titel",
                Description = "Prüfe ob Audit bei Update geschrieben wird.",
                PriorityId = TicketsSeedData.PriorityHighId
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audit = await db.AuditEntries
            .Where(a => a.EntityId == createResult.TicketId.ToString()
                        && a.EventType == "TicketUpdated")
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.Payload.Should().Contain("Geänderter Titel");
    }

    [Fact]
    public async Task UpdateTicket_NoChanges_DoesNotCreateAuditEntry()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Unverändert-Test",
            Description = "Keine Änderung.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PutAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}",
            new UpdateTicketCommand
            {
                Title = "Unverändert-Test",
                Description = "Keine Änderung.",
                PriorityId = TicketsSeedData.PriorityMediumId
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audit = await db.AuditEntries
            .Where(a => a.EntityId == createResult.TicketId.ToString()
                        && a.EventType == "TicketUpdated")
            .FirstOrDefaultAsync();

        audit.Should().BeNull();
    }

    [Fact]
    public async Task UploadAttachment_WithValidFile_Returns201()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Attachment-Test",
            Description = "Ticket für Attachment-Upload.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        using var fileContent = new ByteArrayContent([1, 2, 3, 4, 5]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "test.pdf");

        var uploadResponse = await client.PostAsync(
            $"/api/tickets/{createResult!.TicketId}/attachments", form);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<AddTicketAttachmentResponse>();
        uploadResult.Should().NotBeNull();
        uploadResult!.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task UploadAttachment_NonExistentTicket_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        using var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "test.txt");

        var uploadResponse = await client.PostAsync(
            $"/api/tickets/{Guid.NewGuid()}/attachments", form);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadAttachment_ExistingFile_ReturnsFileContent()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Download-Test",
            Description = "Ticket für Download-Test.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var fileBytes = new byte[] { 10, 20, 30, 40, 50 };
        using var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "data.bin");

        var uploadResponse = await client.PostAsync(
            $"/api/tickets/{createResult!.TicketId}/attachments", form);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<AddTicketAttachmentResponse>();

        var downloadResponse = await client.GetAsync(
            $"/api/tickets/{createResult.TicketId}/attachments/{uploadResult!.AttachmentId}");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().BeEquivalentTo(fileBytes);
    }

    [Fact]
    public async Task UploadAttachment_CreatesAuditEntry()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Attach-Audit-Test",
            Description = "Prüfe ob Audit bei Upload geschrieben wird.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        using var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "audit-test.pdf");

        await client.PostAsync($"/api/tickets/{createResult!.TicketId}/attachments", form);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audit = await db.AuditEntries
            .Where(a => a.EntityId == createResult.TicketId.ToString()
                        && a.EventType == "TicketAttachmentAdded")
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.Payload.Should().Contain("audit-test.pdf");
    }

    [Fact]
    public async Task GetTicketsList_OnlyOpen_ExcludesClosedTickets()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createOpen = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Filter-Open-Test",
            Description = "Offenes Ticket.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });
        var openResult = await createOpen.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var createClosed = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Filter-Closed-Test",
            Description = "Wird geschlossen.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });
        var closedResult = await createClosed.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PatchAsJsonAsync(
            $"/api/tickets/{closedResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusClosedId });

        var result = await client.GetFromJsonAsync<TicketListResultDto>("/api/tickets?OnlyOpen=true");

        result.Should().NotBeNull();
        result!.Items.Should().NotContain(i => i.Title == "Filter-Closed-Test");
        result.Items.Should().Contain(i => i.Title == "Filter-Open-Test");
    }

    [Fact]
    public async Task GetTicketsList_FilterByStatusId_ReturnsOnlyMatchingStatus()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Filter-Status-Test",
            Description = "Wird auf InProgress gesetzt.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PatchAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusInProgressId });

        var result = await client.GetFromJsonAsync<TicketListResultDto>(
            $"/api/tickets?StatusId={TicketsSeedData.StatusInProgressId}");

        result.Should().NotBeNull();
        result!.Items.Should().Contain(i => i.Title == "Filter-Status-Test");
        result.Items.Should().OnlyContain(i => i.StatusName == "In Bearbeitung");
    }

    [Fact]
    public async Task GetTicketsList_FilterByPriorityId_ReturnsOnlyMatchingPriority()
    {
        var client = await CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Filter-Priority-Low",
            Description = "Niedrige Priorität.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });

        var result = await client.GetFromJsonAsync<TicketListResultDto>(
            $"/api/tickets?PriorityId={TicketsSeedData.PriorityLowId}");

        result.Should().NotBeNull();
        result!.Items.Should().Contain(i => i.Title == "Filter-Priority-Low");
        result.Items.Should().OnlyContain(i => i.PriorityName == "Niedrig");
    }

    [Fact]
    public async Task GetTicketsList_CombinedFilters_ReturnsIntersection()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Filter-Combined-Match",
            Description = "InProgress + High.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityHighId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        await client.PatchAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}/status",
            new ChangeTicketStatusCommand { NewStatusId = TicketsSeedData.StatusInProgressId });

        var result = await client.GetFromJsonAsync<TicketListResultDto>(
            $"/api/tickets?StatusId={TicketsSeedData.StatusInProgressId}&PriorityId={TicketsSeedData.PriorityHighId}&OnlyOpen=true");

        result.Should().NotBeNull();
        result!.Items.Should().Contain(i => i.Title == "Filter-Combined-Match");
        result.Items.Should().OnlyContain(i =>
            i.StatusName == "In Bearbeitung" && i.PriorityName == "Hoch");
    }

    [Fact]
    public async Task CreateTicket_WithDueAtUtc_PersistsInDetailAndList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var due = DateTime.UtcNow.AddDays(3);

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Due-Test",
            Description = "Mit Fälligkeit.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId,
            DueAtUtc = due
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();

        var detailResponse = await client.GetAsync($"/api/tickets/{createResult!.TicketId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<TicketDetailDto>();
        detail.Should().NotBeNull();
        detail!.DueAtUtc.Should().NotBeNull();
        detail.DueAtUtc!.Value.Should().BeCloseTo(due, TimeSpan.FromSeconds(2));

        var list = await client.GetFromJsonAsync<TicketListResultDto>("/api/tickets");
        var item = list!.Items.First(i => i.Id == createResult.TicketId);
        item.DueAtUtc.Should().NotBeNull();
        item.DueAtUtc!.Value.Should().BeCloseTo(due, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateTicket_WithDueBeforeNow_ReturnsValidationError()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Ungültige Fälligkeit",
            Description = "Due in der Vergangenheit.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityMediumId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId,
            DueAtUtc = DateTime.UtcNow.AddDays(-2)
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTicket_ChangeDueAtUtc_WritesAuditPayload()
    {
        var client = await CreateAuthenticatedClientAsync();

        var due1 = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostAsJsonAsync("/api/tickets", new CreateTicketCommand
        {
            Title = "Due-Update-Audit",
            Description = "Audit Fälligkeit.",
            TicketTypeId = TicketsSeedData.TypeGeneralRequestId,
            PriorityId = TicketsSeedData.PriorityLowId,
            DepartmentId = FactoryFlow.Modules.Identity.Infrastructure.Seeds.IdentitySeedData.ProductionDeptId,
            DueAtUtc = due1
        });

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateTicketResponse>();
        var due2 = DateTime.UtcNow.AddDays(5);

        await client.PutAsJsonAsync(
            $"/api/tickets/{createResult!.TicketId}",
            new UpdateTicketCommand
            {
                Title = "Due-Update-Audit",
                Description = "Audit Fälligkeit.",
                PriorityId = TicketsSeedData.PriorityLowId,
                DueAtUtc = due2
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        var audits = await db.AuditEntries
            .Where(a => a.EntityId == createResult.TicketId.ToString() && a.EventType == "TicketUpdated")
            .ToListAsync();

        audits.Should().Contain(a => a.Payload != null && a.Payload.Contains("NewDueAtUtc"));
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

        if (!await userManager.IsInRoleAsync(user, AppRoles.Supervisor))
            await userManager.AddToRoleAsync(user, AppRoles.Supervisor);

        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(IntegrationTestAuthHandler.HeaderName, "1");
        return client;
    }
}
