namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;

public sealed record TicketDetailDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string Description,
    string TicketTypeName,
    string PriorityName,
    Guid StatusId,
    string StatusName,
    string? DepartmentName,
    string? SiteName,
    string? MachineOrWorkstation,
    DateTime CreatedAtUtc,
    string CreatedByDisplayName,
    IReadOnlyList<TicketCommentDto> Comments);

public sealed record TicketCommentDto(
    Guid Id,
    string Text,
    DateTime CreatedAtUtc,
    string CreatedByDisplayName);
