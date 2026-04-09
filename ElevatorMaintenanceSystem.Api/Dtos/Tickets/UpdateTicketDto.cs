using ElevatorMaintenanceSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ElevatorMaintenanceSystem.Api.Dtos.Tickets;

public sealed class UpdateTicketDto
{
    [Required]
    [StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    public TicketIssueType IssueType { get; init; }

    public TicketPriority Priority { get; init; }

    public DateTime RequestedDate { get; init; }
}
