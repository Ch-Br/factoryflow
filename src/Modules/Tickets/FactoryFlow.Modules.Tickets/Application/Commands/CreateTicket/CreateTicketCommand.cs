using System.ComponentModel.DataAnnotations;

namespace FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;

public sealed record CreateTicketCommand
{
    [Required, MaxLength(300)]
    public required string Title { get; init; }

    [Required, MaxLength(4000)]
    public required string Description { get; init; }

    [Required]
    public required Guid TicketTypeId { get; init; }

    [Required]
    public required Guid PriorityId { get; init; }

    [Required]
    public required Guid DepartmentId { get; init; }

    public Guid? SiteId { get; init; }

    [MaxLength(200)]
    public string? MachineOrWorkstation { get; init; }
}
