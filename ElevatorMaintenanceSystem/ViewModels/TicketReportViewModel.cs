using CommunityToolkit.Mvvm.ComponentModel;
using ElevatorMaintenanceSystem.Infrastructure.Commands;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ElevatorMaintenanceSystem.ViewModels;

public partial class TicketReportViewModel : ViewModelBase
{
    private readonly ITicketService _ticketService;
    private readonly IElevatorService _elevatorService;
    private readonly IWorkerService _workerService;
    private readonly ILogger<TicketReportViewModel> _logger;

    [ObservableProperty]
    private TicketStatus? _selectedTicketStatus = null;

    [ObservableProperty]
    private Guid? _selectedElevatorId = null;

    [ObservableProperty]
    private Guid? _selectedWorkerId = null;

    [ObservableProperty]
    private DateTime? _fromDate = null;

    [ObservableProperty]
    private DateTime? _toDate = null;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<ReportTicketRowViewModel> ReportTicketRows { get; } = new();

    public ObservableCollection<Elevator> Elevators { get; } = new();

    public ObservableCollection<Worker> Workers { get; } = new();

    public AsyncRelayCommand LoadReportsCommand { get; }
    public AsyncRelayCommand ResetFiltersCommand { get; }
    public AsyncRelayCommand ApplyFiltersCommand { get; }

    public TicketReportViewModel(
        ITicketService ticketService,
        IElevatorService elevatorService,
        IWorkerService workerService,
        ILogger<TicketReportViewModel> logger)
    {
        _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        _elevatorService = elevatorService ?? throw new ArgumentNullException(nameof(elevatorService));
        _workerService = workerService ?? throw new ArgumentNullException(nameof(workerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadReportsCommand = new AsyncRelayCommand(LoadReportsAsync, () => !IsBusy);
        ResetFiltersCommand = new AsyncRelayCommand(ResetFiltersAsync, () => !IsBusy);
        ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync, () => !IsBusy);
    }

    partial void OnSelectedTicketStatusChanged(TicketStatus? value) => RefreshCommandStates();
    partial void OnSelectedElevatorIdChanged(Guid? value) => RefreshCommandStates();
    partial void OnSelectedWorkerIdChanged(Guid? value) => RefreshCommandStates();
    partial void OnFromDateChanged(DateTime? value) => RefreshCommandStates();
    partial void OnToDateChanged(DateTime? value) => RefreshCommandStates();
    partial void OnIsBusyChanged(bool value) => RefreshCommandStates();

    public async Task LoadReportsAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            await LoadDropdownDataAsync();
            await ApplyFiltersAsync();
            StatusMessage = "Reports loaded.";
        }, "Loading reports failed.");
    }

    public async Task ResetFiltersAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            SelectedTicketStatus = null;
            SelectedElevatorId = null;
            SelectedWorkerId = null;
            FromDate = null;
            ToDate = null;
            await ApplyFiltersAsync();
            StatusMessage = "Filters cleared.";
        }, "Resetting filters failed.");
    }

    public async Task ApplyFiltersAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var tickets = await _ticketService.GetFilteredAsync(
                SelectedTicketStatus,
                SelectedElevatorId,
                SelectedWorkerId,
                FromDate,
                ToDate);

            RebuildReportRows(tickets);
            StatusMessage = $"Showing {ReportTicketRows.Count} tickets.";
        }, "Applying filters failed.");
    }

    private async Task LoadDropdownDataAsync()
    {
        var elevators = await _elevatorService.GetActiveAsync();
        var workers = await _workerService.GetActiveAsync();

        ReplaceCollection(Elevators, elevators.OrderBy(e => e.Name));
        ReplaceCollection(Workers, workers.OrderBy(w => w.FullName));
    }

    private void RebuildReportRows(IEnumerable<Ticket> tickets)
    {
        ReportTicketRows.Clear();
        AddRange(ReportTicketRows, tickets.Select(BuildRow));
    }

    private ReportTicketRowViewModel BuildRow(Ticket ticket)
    {
        var elevatorName = Elevators
            .FirstOrDefault(e => e.Id == ticket.ElevatorId)
            ?.Name ?? "Unknown";

        var assignedWorkerName = "Unassigned";

        if (ticket.AssignedWorkerId.HasValue)
        {
            assignedWorkerName = Workers
                .FirstOrDefault(w => w.Id == ticket.AssignedWorkerId.Value)
                ?.FullName ?? ticket.AssignedWorkerId.Value.ToString();
        }

        return new ReportTicketRowViewModel
        {
            TicketId = ticket.Id,
            Description = ticket.Description,
            IssueType = ticket.IssueType.ToString(),
            Priority = ticket.Priority.ToString(),
            Status = ticket.Status.ToString(),
            RequestedDate = ticket.RequestedDate,
            ElevatorName = elevatorName,
            AssignedWorkerName = assignedWorkerName
        };
    }

    private async Task RunBusyOperationAsync(Func<Task> action, string failureMessage)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            _logger.LogError(ex, failureMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshCommandStates()
    {
        LoadReportsCommand.RaiseCanExecuteChanged();
        ResetFiltersCommand.RaiseCanExecuteChanged();
        ApplyFiltersCommand.RaiseCanExecuteChanged();
    }

    private void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        AddRange(collection, items);
    }
}
