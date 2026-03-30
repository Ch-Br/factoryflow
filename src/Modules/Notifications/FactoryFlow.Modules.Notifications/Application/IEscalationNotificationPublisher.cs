namespace FactoryFlow.Modules.Notifications.Application;

public sealed record EscalatedTicketInfo(
    Guid TicketId,
    string TicketNumber,
    string Title,
    int EscalationLevel);

public interface IEscalationNotificationPublisher
{
    Task PublishAsync(
        IReadOnlyList<EscalatedTicketInfo> tickets,
        DateTime escalatedAtUtc,
        CancellationToken ct = default);
}
