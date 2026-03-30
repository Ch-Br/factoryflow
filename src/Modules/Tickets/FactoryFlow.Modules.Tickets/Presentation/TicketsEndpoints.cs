using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;
using FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FactoryFlow.Modules.Tickets.Presentation;

public static class TicketsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        group.MapGet("/", GetTicketsListAsync)
            .WithName("GetTicketsList")
            .Produces<TicketListResultDto>();

        group.MapGet("/{id:guid}", GetTicketDetailAsync)
            .WithName("GetTicketDetail")
            .Produces<TicketDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTicketAsync)
            .WithName("CreateTicket")
            .Produces<CreateTicketResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/creation-lookups", GetCreationLookupsAsync)
            .WithName("GetTicketCreationLookups")
            .Produces<TicketCreationLookupsDto>();

        group.MapPatch("/{id:guid}/status", ChangeTicketStatusAsync)
            .WithName("ChangeTicketStatus")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/comments", AddTicketCommentAsync)
            .WithName("AddTicketComment")
            .Produces<AddTicketCommentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
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
        GetTicketsListQueryHandler handler,
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
}
