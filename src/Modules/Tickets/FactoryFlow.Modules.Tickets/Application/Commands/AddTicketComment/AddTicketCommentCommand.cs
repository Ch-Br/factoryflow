using System.ComponentModel.DataAnnotations;

namespace FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;

public sealed record AddTicketCommentCommand
{
    [Required(ErrorMessage = "Kommentartext ist erforderlich.")]
    [MaxLength(2000, ErrorMessage = "Kommentartext darf maximal 2000 Zeichen lang sein.")]
    public required string Text { get; init; }
}
