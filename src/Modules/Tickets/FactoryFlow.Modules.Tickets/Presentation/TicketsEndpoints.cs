using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
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

        group.MapPost("/", CreateTicketAsync)
            .WithName("CreateTicket")
            .Produces<CreateTicketResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/creation-lookups", GetCreationLookupsAsync)
            .WithName("GetTicketCreationLookups")
            .Produces<TicketCreationLookupsDto>();
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
}
