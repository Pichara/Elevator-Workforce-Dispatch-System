using CommunityToolkit.Mvvm.ComponentModel;
using ElevatorMaintenanceSystem.Infrastructure.Commands;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ElevatorMaintenanceSystem.ViewModels;

public partial class WorkerManagementViewModel : ViewModelBase
{
    private readonly IWorkerService _workerService;
    private readonly ILogger<WorkerManagementViewModel> _logger;

    [ObservableProperty]
    private Worker? _selectedWorker;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _skillsText = string.Empty;

    [ObservableProperty]
    private WorkerAvailabilityStatus _availabilityStatus = WorkerAvailabilityStatus.Available;

    private double? _latitude;

    private double? _longitude;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    [ObservableProperty]
    private bool _isBusy;

    public WorkerManagementViewModel(
        IWorkerService workerService,
        ILogger<WorkerManagementViewModel> logger)
    {
        _workerService = workerService ?? throw new ArgumentNullException(nameof(workerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadCommand = new AsyncRelayCommand(LoadWorkersAsync, () => !IsBusy);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        UpdateCommand = new AsyncRelayCommand(UpdateAsync, CanUpdate);
        UpdateLocationCommand = new AsyncRelayCommand(UpdateLocationAsync, CanUpdateLocation);
        ResetCommand = new AsyncRelayCommand(ResetAsync, () => !IsBusy);
        DeactivateCommand = new AsyncRelayCommand(DeactivateAsync, CanDeactivate);
    }

    public ObservableCollection<Worker> Workers { get; } = new();

    public double? Latitude
    {
        get => _latitude;
        set
        {
            SetProperty(ref _latitude, value);
            RefreshCommandStates();
        }
    }

    public double? Longitude
    {
        get => _longitude;
        set
        {
            SetProperty(ref _longitude, value);
            RefreshCommandStates();
        }
    }

    public AsyncRelayCommand LoadCommand { get; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand UpdateCommand { get; }

    public AsyncRelayCommand UpdateLocationCommand { get; }

    public AsyncRelayCommand ResetCommand { get; }

    public AsyncRelayCommand DeactivateCommand { get; }

    public async Task LoadWorkersAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var workers = await _workerService.GetActiveAsync();

            Workers.Clear();
            AddRange(Workers, workers.OrderBy(worker => worker.FullName));
            StatusMessage = $"Loaded {Workers.Count} worker records.";
        }, "Loading workers failed.");
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var created = await _workerService.CreateAsync(BuildWorker(Guid.NewGuid()), Latitude!.Value, Longitude!.Value);
            Workers.Add(created);
            SelectedWorker = created;
            StatusMessage = $"Created worker '{created.FullName}'.";
        }, "Saving worker failed.");
    }

    private async Task UpdateAsync()
    {
        if (!CanUpdate())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var updated = await _workerService.UpdateAsync(BuildWorker(SelectedWorker!.Id), Latitude!.Value, Longitude!.Value);
            ReplaceWorker(updated);
            SelectedWorker = updated;
            StatusMessage = $"Updated worker '{updated.FullName}'.";
        }, "Updating worker failed.");
    }

    private async Task UpdateLocationAsync()
    {
        if (!CanUpdateLocation())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var updated = await _workerService.UpdateLocationAsync(SelectedWorker!.Id, Latitude!.Value, Longitude!.Value);
            ReplaceWorker(updated);
            SelectedWorker = updated;
            StatusMessage = $"Updated worker location for '{updated.FullName}'.";
        }, "Updating worker location failed.");
    }

    private async Task DeactivateAsync()
    {
        if (!CanDeactivate())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var workerId = SelectedWorker!.Id;
            var deactivated = await _workerService.DeactivateAsync(workerId);
            var toRemove = Workers.FirstOrDefault(worker => worker.Id == workerId);
            if (toRemove != null)
            {
                Workers.Remove(toRemove);
            }

            ResetEditor();
            StatusMessage = $"Deactivated worker '{deactivated.FullName}'.";
        }, "Deactivating worker failed.");
    }

    private Task ResetAsync()
    {
        ResetEditor();
        StatusMessage = "Worker editor reset.";
        return Task.CompletedTask;
    }

    partial void OnSelectedWorkerChanged(Worker? value)
    {
        if (value == null)
        {
            ResetEditor(clearStatus: false);
            return;
        }

        FullName = value.FullName;
        Email = value.Email;
        PhoneNumber = value.PhoneNumber;
        SkillsText = string.Join(", ", value.Skills);
        AvailabilityStatus = value.AvailabilityStatus;
        Latitude = value.Location?.Coordinates.Latitude;
        Longitude = value.Location?.Coordinates.Longitude;
        StatusMessage = $"Editing worker '{value.FullName}'.";
        RefreshCommandStates();
    }

    partial void OnFullNameChanged(string value) => RefreshCommandStates();
    partial void OnEmailChanged(string value) => RefreshCommandStates();
    partial void OnPhoneNumberChanged(string value) => RefreshCommandStates();
    partial void OnSkillsTextChanged(string value) => RefreshCommandStates();
    partial void OnAvailabilityStatusChanged(WorkerAvailabilityStatus value) => RefreshCommandStates();
    partial void OnIsBusyChanged(bool value) => RefreshCommandStates();

    private Worker BuildWorker(Guid id)
    {
        return new Worker
        {
            Id = id,
            FullName = FullName.Trim(),
            Email = Email.Trim(),
            PhoneNumber = PhoneNumber.Trim(),
            Skills = SkillsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            AvailabilityStatus = AvailabilityStatus
        };
    }

    private bool CanSave()
    {
        return !IsBusy && HasRequiredFields();
    }

    private bool CanUpdate()
    {
        return !IsBusy && SelectedWorker != null && HasRequiredFields();
    }

    private bool CanUpdateLocation()
    {
        return !IsBusy && SelectedWorker != null && Latitude.HasValue && Longitude.HasValue;
    }

    private bool CanDeactivate()
    {
        return !IsBusy && SelectedWorker != null;
    }

    private bool HasRequiredFields()
    {
        return !string.IsNullOrWhiteSpace(FullName)
            && !string.IsNullOrWhiteSpace(Email)
            && !string.IsNullOrWhiteSpace(PhoneNumber)
            && Latitude.HasValue
            && Longitude.HasValue;
    }

    private void ReplaceWorker(Worker updated)
    {
        var index = Workers
            .Select((worker, position) => new { worker, position })
            .FirstOrDefault(entry => entry.worker.Id == updated.Id)?
            .position;

        if (index.HasValue)
        {
            Workers[index.Value] = updated;
        }
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

    private void ResetEditor(bool clearStatus = true)
    {
        SelectedWorker = null;
        FullName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        SkillsText = string.Empty;
        AvailabilityStatus = WorkerAvailabilityStatus.Available;
        Latitude = null;
        Longitude = null;

        if (clearStatus)
        {
            StatusMessage = "Ready.";
        }

        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        LoadCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        UpdateCommand.RaiseCanExecuteChanged();
        UpdateLocationCommand.RaiseCanExecuteChanged();
        ResetCommand.RaiseCanExecuteChanged();
        DeactivateCommand.RaiseCanExecuteChanged();
    }
}
