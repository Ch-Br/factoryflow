using System.ComponentModel.DataAnnotations;

namespace FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;

public sealed record ChangeTicketStatusCommand
{
    [Required]
    public required Guid NewStatusId { get; init; }
}
