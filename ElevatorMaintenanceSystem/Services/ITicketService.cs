using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Services;

public interface ITicketService
{
    Task<IReadOnlyList<Ticket>> GetActiveAsync();

    Task<Ticket> CreateAsync(Guid elevatorId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate);

    Task<Ticket> UpdateDetailsAsync(Guid ticketId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate);

    Task<Ticket> AssignWorkerAsync(Guid ticketId, Guid workerId);

    Task<Ticket> UnassignWorkerAsync(Guid ticketId);

    Task<Ticket> ChangeStatusAsync(Guid ticketId, TicketStatus nextStatus);

    Task<Ticket> CancelAsync(Guid ticketId);

    Task DeleteCanceledAsync(Guid ticketId);

    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus? status);
    Task<IEnumerable<Ticket>> GetByElevatorAsync(Guid elevatorId);
    Task<IEnumerable<Ticket>> GetByWorkerAsync(Guid workerId);
    Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<Ticket>> GetFilteredAsync(TicketStatus? status, Guid? elevatorId, Guid? workerId, DateTime? fromDate, DateTime? toDate);
}
