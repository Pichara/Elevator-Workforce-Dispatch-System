using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using ElevatorMaintenanceSystem.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ElevatorMaintenanceSystem.Tests.ViewModels;

public class TicketReportViewModelTests
{
    [Fact]
    public async Task LoadReportsAsync_PopulatesDropdownsAndBuildsReportRows()
    {
        var fixture = CreateFixture();

        await fixture.ViewModel.LoadReportsAsync();

        Assert.Equal(new[] { "Alpha Tower", "Zulu Tower" }, fixture.ViewModel.Elevators.Select(elevator => elevator.Name));
        Assert.Equal(new[] { "Ava Stone", "Noah Reed" }, fixture.ViewModel.Workers.Select(worker => worker.FullName));
        Assert.Equal(2, fixture.ViewModel.ReportTicketRows.Count);
        Assert.Equal("Alpha Tower", fixture.ViewModel.ReportTicketRows[0].ElevatorName);
        Assert.Equal("Ava Stone", fixture.ViewModel.ReportTicketRows[0].AssignedWorkerName);
        Assert.Equal("Unassigned", fixture.ViewModel.ReportTicketRows[1].AssignedWorkerName);
        Assert.Equal("Reports loaded.", fixture.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task ApplyFiltersCommand_UsesCurrentFiltersAndRefreshesRows()
    {
        var fixture = CreateFixture();
        await fixture.ViewModel.LoadReportsAsync();

        fixture.ViewModel.SelectedTicketStatus = TicketStatus.Pending;
        fixture.ViewModel.SelectedElevatorId = fixture.ElevatorA.Id;
        fixture.ViewModel.SelectedWorkerId = null;
        fixture.ViewModel.FromDate = new DateTime(2026, 4, 1);
        fixture.ViewModel.ToDate = new DateTime(2026, 4, 2);

        await fixture.ViewModel.ApplyFiltersCommand.ExecuteAsync();

        Assert.Equal(TicketStatus.Pending, fixture.Tickets.LastQuery.Status);
        Assert.Equal(fixture.ElevatorA.Id, fixture.Tickets.LastQuery.ElevatorId);
        Assert.Null(fixture.Tickets.LastQuery.WorkerId);
        Assert.Equal(new DateTime(2026, 4, 1), fixture.Tickets.LastQuery.FromDate);
        Assert.Equal(new DateTime(2026, 4, 2), fixture.Tickets.LastQuery.ToDate);
        Assert.Collection(
            fixture.ViewModel.ReportTicketRows,
            row => Assert.Equal(fixture.PendingUnassignedTicket.Id, row.TicketId));
        Assert.Equal("Showing 1 tickets.", fixture.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task ResetFiltersCommand_ClearsFiltersAndReloadsAllRows()
    {
        var fixture = CreateFixture();
        await fixture.ViewModel.LoadReportsAsync();

        fixture.ViewModel.SelectedTicketStatus = TicketStatus.Assigned;
        fixture.ViewModel.SelectedElevatorId = fixture.ElevatorB.Id;
        fixture.ViewModel.SelectedWorkerId = fixture.WorkerB.Id;
        fixture.ViewModel.FromDate = new DateTime(2026, 4, 3);
        fixture.ViewModel.ToDate = new DateTime(2026, 4, 3);

        await fixture.ViewModel.ResetFiltersCommand.ExecuteAsync();

        Assert.Null(fixture.ViewModel.SelectedTicketStatus);
        Assert.Null(fixture.ViewModel.SelectedElevatorId);
        Assert.Null(fixture.ViewModel.SelectedWorkerId);
        Assert.Null(fixture.ViewModel.FromDate);
        Assert.Null(fixture.ViewModel.ToDate);
        Assert.Null(fixture.Tickets.LastQuery.Status);
        Assert.Null(fixture.Tickets.LastQuery.ElevatorId);
        Assert.Null(fixture.Tickets.LastQuery.WorkerId);
        Assert.Null(fixture.Tickets.LastQuery.FromDate);
        Assert.Null(fixture.Tickets.LastQuery.ToDate);
        Assert.Equal(2, fixture.ViewModel.ReportTicketRows.Count);
        Assert.Equal("Filters cleared.", fixture.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task ApplyFiltersAsync_WhenServiceThrows_SurfacesExceptionMessage()
    {
        var fixture = CreateFixture();
        fixture.Tickets.ThrowOnFilter = new InvalidOperationException("Reporting service unavailable.");

        await fixture.ViewModel.ApplyFiltersAsync();

        Assert.Equal("Reporting service unavailable.", fixture.ViewModel.StatusMessage);
        Assert.False(fixture.ViewModel.IsBusy);
    }

    private static TicketReportFixture CreateFixture()
    {
        var elevatorA = new Elevator
        {
            Id = Guid.NewGuid(),
            Name = "Alpha Tower",
            Address = "100 Main St",
            BuildingName = "Alpha",
            FloorLabel = "L1",
            Manufacturer = "Otis",
            InstallationDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        var elevatorB = new Elevator
        {
            Id = Guid.NewGuid(),
            Name = "Zulu Tower",
            Address = "200 Main St",
            BuildingName = "Zulu",
            FloorLabel = "L2",
            Manufacturer = "Kone",
            InstallationDate = new DateTime(2021, 1, 1),
            IsActive = true
        };

        var workerA = new Worker
        {
            Id = Guid.NewGuid(),
            FullName = "Ava Stone",
            Email = "ava@example.com",
            PhoneNumber = "555-0100",
            Skills = ["Repair"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available
        };

        var workerB = new Worker
        {
            Id = Guid.NewGuid(),
            FullName = "Noah Reed",
            Email = "noah@example.com",
            PhoneNumber = "555-0101",
            Skills = ["Inspection"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available
        };

        var assignedTicket = CreateTicket(elevatorA.Id, workerA.Id, TicketStatus.Assigned, "Assigned ticket", new DateTime(2026, 4, 3));
        var pendingUnassignedTicket = CreateTicket(elevatorA.Id, null, TicketStatus.Pending, "Pending ticket", new DateTime(2026, 4, 2));
        var workerBTicket = CreateTicket(elevatorB.Id, workerB.Id, TicketStatus.Closed, "Closed ticket", new DateTime(2026, 4, 1), deletedAt: DateTime.UtcNow);

        var ticketService = new FakeTicketService(assignedTicket, pendingUnassignedTicket, workerBTicket);
        var elevatorService = new FakeElevatorService(elevatorB, elevatorA);
        var workerService = new FakeWorkerService(workerB, workerA);
        var viewModel = new TicketReportViewModel(
            ticketService,
            elevatorService,
            workerService,
            NullLogger<TicketReportViewModel>.Instance);

        return new TicketReportFixture(
            viewModel,
            ticketService,
            elevatorA,
            elevatorB,
            workerA,
            workerB,
            assignedTicket,
            pendingUnassignedTicket);
    }

    private static Ticket CreateTicket(
        Guid elevatorId,
        Guid? workerId,
        TicketStatus status,
        string description,
        DateTime requestedDate,
        DateTime? deletedAt = null)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            ElevatorId = elevatorId,
            AssignedWorkerId = workerId,
            Description = description,
            IssueType = TicketIssueType.Mechanical,
            Priority = TicketPriority.Medium,
            RequestedDate = requestedDate,
            Status = status,
            DeletedAt = deletedAt
        };
    }

    private sealed record TicketReportFixture(
        TicketReportViewModel ViewModel,
        FakeTicketService Tickets,
        Elevator ElevatorA,
        Elevator ElevatorB,
        Worker WorkerA,
        Worker WorkerB,
        Ticket AssignedTicket,
        Ticket PendingUnassignedTicket);

    private sealed class FakeElevatorService : IElevatorService
    {
        private readonly IReadOnlyList<Elevator> _elevators;

        public FakeElevatorService(params Elevator[] elevators)
        {
            _elevators = elevators;
        }

        public Task<IReadOnlyList<Elevator>> GetActiveAsync() => Task.FromResult(_elevators);
        public Task<Elevator> CreateAsync(Elevator elevator, double latitude, double longitude) => throw new NotSupportedException();
        public Task<Elevator> UpdateAsync(Elevator elevator, double latitude, double longitude) => throw new NotSupportedException();
        public Task DeleteInactiveAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class FakeWorkerService : IWorkerService
    {
        private readonly IReadOnlyList<Worker> _workers;

        public FakeWorkerService(params Worker[] workers)
        {
            _workers = workers;
        }

        public Task<IReadOnlyList<Worker>> GetActiveAsync() => Task.FromResult(_workers);
        public Task<Worker> CreateAsync(Worker worker, double latitude, double longitude) => throw new NotSupportedException();
        public Task<Worker> UpdateAsync(Worker worker, double latitude, double longitude) => throw new NotSupportedException();
        public Task<Worker> DeactivateAsync(Guid id) => throw new NotSupportedException();
        public Task<Worker> UpdateLocationAsync(Guid id, double latitude, double longitude) => throw new NotSupportedException();
    }

    private sealed class FakeTicketService : ITicketService
    {
        private readonly List<Ticket> _items;

        public FakeTicketService(params Ticket[] items)
        {
            _items = items.ToList();
        }

        public FilterQuery LastQuery { get; private set; } = new(null, null, null, null, null);

        public Exception? ThrowOnFilter { get; set; }

        public Task<IReadOnlyList<Ticket>> GetActiveAsync() => Task.FromResult<IReadOnlyList<Ticket>>(_items);
        public Task<Ticket> CreateAsync(Guid elevatorId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate) => throw new NotSupportedException();
        public Task<Ticket> UpdateDetailsAsync(Guid ticketId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate) => throw new NotSupportedException();
        public Task<Ticket> AssignWorkerAsync(Guid ticketId, Guid workerId) => throw new NotSupportedException();
        public Task<Ticket> UnassignWorkerAsync(Guid ticketId) => throw new NotSupportedException();
        public Task<Ticket> ChangeStatusAsync(Guid ticketId, TicketStatus nextStatus) => throw new NotSupportedException();
        public Task<Ticket> CancelAsync(Guid ticketId) => throw new NotSupportedException();
        public Task DeleteCanceledAsync(Guid ticketId) => throw new NotSupportedException();

        public Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus? status) => Task.FromResult(ApplyFilters(status, null, null, null, null));
        public Task<IEnumerable<Ticket>> GetByElevatorAsync(Guid elevatorId) => Task.FromResult(ApplyFilters(null, elevatorId, null, null, null));
        public Task<IEnumerable<Ticket>> GetByWorkerAsync(Guid workerId) => Task.FromResult(ApplyFilters(null, null, workerId, null, null));
        public Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate) => Task.FromResult(ApplyFilters(null, null, null, fromDate, toDate));

        public Task<IEnumerable<Ticket>> GetFilteredAsync(TicketStatus? status, Guid? elevatorId, Guid? workerId, DateTime? fromDate, DateTime? toDate)
        {
            if (ThrowOnFilter != null)
            {
                throw ThrowOnFilter;
            }

            LastQuery = new FilterQuery(status, elevatorId, workerId, fromDate, toDate);
            return Task.FromResult(ApplyFilters(status, elevatorId, workerId, fromDate, toDate));
        }

        private IEnumerable<Ticket> ApplyFilters(TicketStatus? status, Guid? elevatorId, Guid? workerId, DateTime? fromDate, DateTime? toDate)
        {
            return _items
                .Where(ticket => ticket.DeletedAt == null)
                .Where(ticket => !status.HasValue || ticket.Status == status.Value)
                .Where(ticket => !elevatorId.HasValue || ticket.ElevatorId == elevatorId.Value)
                .Where(ticket => !workerId.HasValue || ticket.AssignedWorkerId == workerId.Value)
                .Where(ticket => !fromDate.HasValue || ticket.RequestedDate >= fromDate.Value)
                .Where(ticket => !toDate.HasValue || ticket.RequestedDate <= toDate.Value)
                .OrderByDescending(ticket => ticket.RequestedDate)
                .ToList();
        }
    }

    private sealed record FilterQuery(
        TicketStatus? Status,
        Guid? ElevatorId,
        Guid? WorkerId,
        DateTime? FromDate,
        DateTime? ToDate);
}
