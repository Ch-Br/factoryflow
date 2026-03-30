namespace FactoryFlow.Modules.Tickets.Application.Commands.AddTicketAttachment;

public sealed class AddTicketAttachmentCommand
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required Stream Content { get; init; }
}
