using FactoryFlow.Modules.Notifications.Application.Commands.MarkNotificationRead;
using FactoryFlow.Modules.Notifications.Application.Queries.GetMyNotifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FactoryFlow.Modules.Notifications.Presentation;

public static class NotificationsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications");

        group.MapGet("/", GetMyNotificationsAsync)
            .WithName("GetMyNotifications")
            .Produces<NotificationListResultDto>()
            .RequireAuthorization();

        group.MapPost("/{id:guid}/read", MarkNotificationReadAsync)
            .WithName("MarkNotificationRead")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetMyNotificationsAsync(
        GetMyNotificationsQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> MarkNotificationReadAsync(
        Guid id,
        MarkNotificationReadCommandHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, ct);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("nicht gefunden")))
                return Results.NotFound();

            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [""] = result.Errors.ToArray() });
        }

        return Results.NoContent();
    }
}
