using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Api.Dtos.Tickets;

public sealed class TicketAuditEntryDto
{
    public DateTime OccurredAtUtc { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public TicketAuditEntryType EntryType { get; init; }
    public TicketStatus? FromStatus { get; init; }
    public TicketStatus? ToStatus { get; init; }
    public Guid? FromWorkerId { get; init; }
    public Guid? ToWorkerId { get; init; }
    public string Message { get; init; } = string.Empty;
}
