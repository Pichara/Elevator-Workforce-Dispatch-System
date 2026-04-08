using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;

namespace ElevatorMaintenanceSystem.Tests.Services;

public class MapDispatchServiceTests
{
    [Fact]
    public async Task MAP_06_D_03_LoadElevatorTicketContextAsync_FiltersActiveAndSortsByPriorityThenRequestedDate()
    {
        var elevatorId = Guid.NewGuid();
        var ticketService = new StubTicketService
        {
            TicketsByElevator =
            [
                CreateTicket(elevatorId, TicketPriority.Low, TicketStatus.Pending, new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc)),
                CreateTicket(elevatorId, TicketPriority.Critical, TicketStatus.Canceled, new DateTime(2026, 4, 2, 9, 5, 0, DateTimeKind.Utc)),
                CreateTicket(elevatorId, TicketPriority.High, TicketStatus.Assigned, new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc)),
                CreateTicket(elevatorId, TicketPriority.High, TicketStatus.InProgress, new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)),
                CreateTicket(elevatorId, TicketPriority.Critical, TicketStatus.Resolved, new DateTime(2026, 4, 2, 7, 0, 0, DateTimeKind.Utc))
            ]
        };

        var service = new MapDispatchService(ticketService);

        var context = await service.LoadElevatorTicketContextAsync(elevatorId);

        Assert.Equal(elevatorId, context.ElevatorId);
        Assert.Collection(
            context.ActiveTickets,
            first =>
            {
                Assert.Equal(TicketPriority.High, first.Priority);
                Assert.Equal(TicketStatus.InProgress, first.Status);
            },
            second =>
            {
                Assert.Equal(TicketPriority.High, second.Priority);
                Assert.Equal(TicketStatus.Assigned, second.Status);
            },
            third =>
            {
                Assert.Equal(TicketPriority.Low, third.Priority);
                Assert.Equal(TicketStatus.Pending, third.Status);
            });
    }

    [Fact]
    public async Task MAP_05_D_01_D_02_AssignWorkerToTicketAsync_DelegatesToTicketServiceAssignWorkerAsync()
    {
        var elevatorId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var ticketService = new StubTicketService
        {
            AssignWorkerResult = CreateTicket(elevatorId, TicketPriority.Medium, TicketStatus.Assigned, DateTime.UtcNow, ticketId, workerId)
        };

        var service = new MapDispatchService(ticketService);

        var result = await service.AssignWorkerToTicketAsync(ticketId, workerId);

        Assert.True(result.Success);
        Assert.Equal(ticketId, ticketService.LastAssignedTicketId);
        Assert.Equal(workerId, ticketService.LastAssignedWorkerId);
        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal(workerId, result.WorkerId);
    }

    private static Ticket CreateTicket(
        Guid elevatorId,
        TicketPriority priority,
        TicketStatus status,
        DateTime requestedDate,
        Guid? ticketId = null,
        Guid? assignedWorkerId = null)
    {
        return new Ticket
        {
            Id = ticketId ?? Guid.NewGuid(),
            ElevatorId = elevatorId,
            AssignedWorkerId = assignedWorkerId,
            Description = "Dispatch test ticket",
            IssueType = TicketIssueType.Other,
            Priority = priority,
            RequestedDate = requestedDate,
            Status = status
        };
    }

    private sealed class StubTicketService : ITicketService
    {
        public IEnumerable<Ticket> TicketsByElevator { get; set; } = [];
        public Ticket? AssignWorkerResult { get; set; }
        public Guid LastAssignedTicketId { get; private set; }
        public Guid LastAssignedWorkerId { get; private set; }

        public Task<Ticket> AssignWorkerAsync(Guid ticketId, Guid workerId)
        {
            LastAssignedTicketId = ticketId;
            LastAssignedWorkerId = workerId;
            return Task.FromResult(AssignWorkerResult ?? throw new InvalidOperationException("AssignWorkerResult must be configured."));
        }

        public Task<IEnumerable<Ticket>> GetByElevatorAsync(Guid elevatorId)
        {
            return Task.FromResult(TicketsByElevator);
        }

        public Task<IReadOnlyList<Ticket>> GetActiveAsync() => throw new NotSupportedException();
        public Task<Ticket> CreateAsync(Guid elevatorId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate) => throw new NotSupportedException();
        public Task<Ticket> UpdateDetailsAsync(Guid ticketId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate) => throw new NotSupportedException();
        public Task<Ticket> UnassignWorkerAsync(Guid ticketId) => throw new NotSupportedException();
        public Task<Ticket> ChangeStatusAsync(Guid ticketId, TicketStatus nextStatus) => throw new NotSupportedException();
        public Task<Ticket> CancelAsync(Guid ticketId) => throw new NotSupportedException();
        public Task DeleteCanceledAsync(Guid ticketId) => throw new NotSupportedException();
        public Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus? status) => throw new NotSupportedException();
        public Task<IEnumerable<Ticket>> GetByWorkerAsync(Guid workerId) => throw new NotSupportedException();
        public Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate) => throw new NotSupportedException();
        public Task<IEnumerable<Ticket>> GetFilteredAsync(TicketStatus? status, Guid? elevatorId, Guid? workerId, DateTime? fromDate, DateTime? toDate) => throw new NotSupportedException();
    }
}
