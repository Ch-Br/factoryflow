using System.ComponentModel.DataAnnotations;

namespace FactoryFlow.Modules.Tickets.Application.Commands.UpdateTicket;

public sealed record UpdateTicketCommand
{
    [Required(ErrorMessage = "Titel ist erforderlich.")]
    [MaxLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
    public required string Title { get; init; }

    [Required(ErrorMessage = "Beschreibung ist erforderlich.")]
    [MaxLength(4000, ErrorMessage = "Beschreibung darf maximal 4000 Zeichen lang sein.")]
    public required string Description { get; init; }

    [Required]
    public required Guid PriorityId { get; init; }

    public DateTime? DueAtUtc { get; init; }
}
