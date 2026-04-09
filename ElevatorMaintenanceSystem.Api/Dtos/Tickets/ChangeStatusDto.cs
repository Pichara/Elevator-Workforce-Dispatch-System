using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Api.Dtos.Tickets;

public sealed class ChangeStatusDto
{
    public TicketStatus Status { get; init; }
}
