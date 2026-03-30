namespace FactoryFlow.Modules.Notifications.Application.Queries.GetMyNotifications;

public sealed record NotificationDto(
    Guid Id,
    string NotificationType,
    Guid TicketId,
    string TicketNumber,
    string Title,
    int EscalationLevel,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record NotificationListResultDto(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount);
