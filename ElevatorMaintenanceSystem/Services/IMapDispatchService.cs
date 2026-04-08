namespace ElevatorMaintenanceSystem.Services;

public interface IMapDispatchService
{
    Task<ElevatorTicketContext> LoadElevatorTicketContextAsync(Guid elevatorId, CancellationToken cancellationToken = default);

    Task<MapAssignmentResult> AssignWorkerToTicketAsync(Guid ticketId, Guid workerId, CancellationToken cancellationToken = default);
}
