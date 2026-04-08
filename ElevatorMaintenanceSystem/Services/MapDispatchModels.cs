using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Services;

public sealed record ElevatorTicketContext(
    Guid ElevatorId,
    IReadOnlyList<ElevatorTicketSummary> ActiveTickets);

public sealed record ElevatorTicketSummary(
    Guid TicketId,
    string Description,
    TicketPriority Priority,
    TicketStatus Status,
    DateTime RequestedDate,
    Guid? AssignedWorkerId);

public sealed record MapAssignmentResult(
    bool Success,
    Guid TicketId,
    Guid WorkerId,
    string StatusMessage,
    string? ErrorMessage = null);
