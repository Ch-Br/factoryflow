namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;

public sealed record TicketDetailDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string Description,
    string TicketTypeName,
    Guid PriorityId,
    string PriorityName,
    Guid StatusId,
    string StatusName,
    string? DepartmentName,
    string? SiteName,
    string? MachineOrWorkstation,
    DateTime CreatedAtUtc,
    string CreatedByDisplayName,
    IReadOnlyList<TicketCommentDto> Comments,
    IReadOnlyList<TicketHistoryItemDto> History);

public sealed record TicketCommentDto(
    Guid Id,
    string Text,
    DateTime CreatedAtUtc,
    string CreatedByDisplayName);

public sealed record TicketHistoryItemDto(
    DateTime OccurredAtUtc,
    string EventType,
    string EventLabel,
    string ActorDisplayName,
    string Text);
