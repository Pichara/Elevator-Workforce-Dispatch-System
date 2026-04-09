using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Api.Dtos.Tickets;

public sealed class TicketDto
{
    public Guid Id { get; init; }
    public Guid ElevatorId { get; init; }
    public Guid? AssignedWorkerId { get; init; }
    public string Description { get; init; } = string.Empty;
    public TicketIssueType IssueType { get; init; }
    public TicketPriority Priority { get; init; }
    public DateTime RequestedDate { get; init; }
    public TicketStatus Status { get; init; }
    public IReadOnlyList<TicketAuditEntryDto> History { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
