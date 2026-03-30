using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketAttachment;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;
using FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Commands.UpdateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using FactoryFlow.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Presentation;

public static class TicketsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .DisableAntiforgery();

        group.MapGet("/", GetTicketsListAsync)
            .WithName("GetTicketsList")
            .Produces<TicketListResultDto>()
            .RequireAuthorization(AuthPolicies.TicketsUse);

        group.MapGet("/overdue", GetOverdueTicketsAsync)
            .WithName("GetOverdueTickets")
            .Produces<OverdueTicketsResultDto>()
            .RequireAuthorization(AuthPolicies.TicketsManage);

        group.MapGet("/{id:guid}", GetTicketDetailAsync)
            .WithName("GetTicketDetail")
            .Produces<TicketDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.TicketsUse);

        group.MapPost("/", CreateTicketAsync)
            .WithName("CreateTicket")
            .Produces<CreateTicketResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.TicketsUse);

        group.MapGet("/creation-lookups", GetCreationLookupsAsync)
            .WithName("GetTicketCreationLookups")
            .Produces<TicketCreationLookupsDto>()
            .RequireAuthorization(AuthPolicies.TicketsUse);

        group.MapPut("/{id:guid}", UpdateTicketAsync)
            .WithName("UpdateTicket")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.TicketsManage);

        group.MapPatch("/{id:guid}/status", ChangeTicketStatusAsync)
            .WithName("ChangeTicketStatus")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.TicketsManage);

        group.MapPost("/{id:guid}/comments", AddTicketCommentAsync)
            .WithName("AddTicketComment")
            .Produces<AddTicketCommentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.TicketsUse);

        group.MapPost("/{id:guid}/attachments", UploadAttachmentAsync)
            .WithName("UploadAttachment")
            .Produces<AddTicketAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.TicketsUse);

        group.MapGet("/{id:guid}/attachments/{attachmentId:guid}", DownloadAttachmentAsync)
            .WithName("DownloadAttachment")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.TicketsUse);
    }

    private static async Task<IResult> CreateTicketAsync(
        CreateTicketCommand command,
        CreateTicketCommandHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);

        if (!result.Succeeded)
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [""] = result.Errors.ToArray() });

        return Results.Created($"/api/tickets/{result.Value!.TicketId}", result.Value);
    }

    private static async Task<IResult> GetCreationLookupsAsync(
        GetTicketCreationLookupsQueryHandler handler,
        CancellationToken ct)
    {
        var lookups = await handler.HandleAsync(ct);
        return Results.Ok(lookups);
    }

    private static async Task<IResult> GetTicketsListAsync(
        [AsParameters] GetTicketsListQuery query,
        GetTicketsListQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOverdueTicketsAsync(
        GetOverdueTicketsQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTicketDetailAsync(
        Guid id,
        GetTicketDetailQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateTicketAsync(
        Guid id,
        UpdateTicketCommand command,
        UpdateTicketCommandHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, command, ct);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("nicht gefunden")))
                return Results.NotFound();

            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [""] = result.Errors.ToArray() });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ChangeTicketStatusAsync(
        Guid id,
        ChangeTicketStatusCommand command,
        ChangeTicketStatusCommandHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, command, ct);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("nicht gefunden")))
                return Results.NotFound();

            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [""] = result.Errors.ToArray() });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> AddTicketCommentAsync(
        Guid id,
        AddTicketCommentCommand command,
        AddTicketCommentCommandHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, command, ct);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("nicht gefunden")))
                return Results.NotFound();

            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [""] = result.Errors.ToArray() });
        }

        return Results.Created($"/api/tickets/{id}/comments/{result.Value!.CommentId}", result.Value);
    }

    private static async Task<IResult> UploadAttachmentAsync(
        Guid id,
        IFormFile file,
        AddTicketAttachmentCommandHandler handler,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var command = new AddTicketAttachmentCommand
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Content = stream
        };

        var result = await handler.HandleAsync(id, command, ct);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("nicht gefunden")))
                return Results.NotFound();

            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [""] = result.Errors.ToArray() });
        }

        return Results.Created(
            $"/api/tickets/{id}/attachments/{result.Value!.AttachmentId}", result.Value);
    }

    private static async Task<IResult> DownloadAttachmentAsync(
        Guid id,
        Guid attachmentId,
        DbContext db,
        IFileStorage fileStorage,
        CancellationToken ct)
    {
        var attachment = await db.Set<TicketAttachment>()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id, ct);

        if (attachment is null)
            return Results.NotFound();

        try
        {
            var stream = await fileStorage.LoadAsync(attachment.StorageKey, ct);
            return Results.File(stream, attachment.ContentType, attachment.FileName);
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
