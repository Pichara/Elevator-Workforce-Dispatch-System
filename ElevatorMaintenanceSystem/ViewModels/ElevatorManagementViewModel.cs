using CommunityToolkit.Mvvm.ComponentModel;
using ElevatorMaintenanceSystem.Infrastructure.Commands;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ElevatorMaintenanceSystem.ViewModels;

public partial class ElevatorManagementViewModel : ViewModelBase
{
    private readonly IElevatorService _elevatorService;
    private readonly ILogger<ElevatorManagementViewModel> _logger;

    [ObservableProperty]
    private Elevator? _selectedElevator;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _buildingName = string.Empty;

    [ObservableProperty]
    private string _floorLabel = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private DateTime _installationDate = DateTime.UtcNow.Date;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private double? _latitude;

    [ObservableProperty]
    private double? _longitude;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    [ObservableProperty]
    private bool _isBusy;

    public ElevatorManagementViewModel(
        IElevatorService elevatorService,
        ILogger<ElevatorManagementViewModel> logger)
    {
        _elevatorService = elevatorService ?? throw new ArgumentNullException(nameof(elevatorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadCommand = new AsyncRelayCommand(LoadElevatorsAsync, () => !IsBusy);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        UpdateCommand = new AsyncRelayCommand(UpdateAsync, CanUpdate);
        ResetCommand = new AsyncRelayCommand(ResetAsync, () => !IsBusy);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
    }

    public ObservableCollection<Elevator> Elevators { get; } = new();

    public AsyncRelayCommand LoadCommand { get; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand UpdateCommand { get; }

    public AsyncRelayCommand ResetCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    public async Task LoadElevatorsAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var elevators = await _elevatorService.GetActiveAsync();

            Elevators.Clear();
            AddRange(Elevators, elevators.OrderBy(elevator => elevator.Name));
            StatusMessage = $"Loaded {Elevators.Count} elevator records.";
        }, "Loading elevators failed.");
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var created = await _elevatorService.CreateAsync(BuildElevator(Guid.NewGuid()), Latitude!.Value, Longitude!.Value);
            Elevators.Add(created);
            SelectedElevator = created;
            StatusMessage = $"Created elevator '{created.Name}'.";
        }, "Saving elevator failed.");
    }

    private async Task UpdateAsync()
    {
        if (!CanUpdate())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var updated = await _elevatorService.UpdateAsync(BuildElevator(SelectedElevator!.Id), Latitude!.Value, Longitude!.Value);
            ReplaceElevator(updated);
            SelectedElevator = updated;
            StatusMessage = $"Updated elevator '{updated.Name}'.";
        }, "Updating elevator failed.");
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete())
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var elevatorId = SelectedElevator!.Id;
            await _elevatorService.DeleteInactiveAsync(elevatorId);

            var toRemove = Elevators.FirstOrDefault(elevator => elevator.Id == elevatorId);
            if (toRemove != null)
            {
                Elevators.Remove(toRemove);
            }

            ResetEditor();
            StatusMessage = "Deleted inactive elevator.";
        }, "Deleting elevator failed.");
    }

    private Task ResetAsync()
    {
        ResetEditor();
        StatusMessage = "Elevator editor reset.";
        return Task.CompletedTask;
    }

    partial void OnSelectedElevatorChanged(Elevator? value)
    {
        if (value == null)
        {
            ResetEditor(clearStatus: false);
            return;
        }

        Name = value.Name;
        Address = value.Address;
        BuildingName = value.BuildingName;
        FloorLabel = value.FloorLabel;
        Manufacturer = value.Manufacturer;
        InstallationDate = value.InstallationDate;
        IsActive = value.IsActive;
        Latitude = value.Location?.Coordinates.Latitude;
        Longitude = value.Location?.Coordinates.Longitude;
        StatusMessage = $"Editing elevator '{value.Name}'.";
        RefreshCommandStates();
    }

    partial void OnNameChanged(string value) => RefreshCommandStates();
    partial void OnAddressChanged(string value) => RefreshCommandStates();
    partial void OnBuildingNameChanged(string value) => RefreshCommandStates();
    partial void OnFloorLabelChanged(string value) => RefreshCommandStates();
    partial void OnManufacturerChanged(string value) => RefreshCommandStates();
    partial void OnInstallationDateChanged(DateTime value) => RefreshCommandStates();
    partial void OnIsActiveChanged(bool value) => RefreshCommandStates();
    partial void OnLatitudeChanged(double? value) => RefreshCommandStates();
    partial void OnLongitudeChanged(double? value) => RefreshCommandStates();
    partial void OnIsBusyChanged(bool value) => RefreshCommandStates();

    private Elevator BuildElevator(Guid id)
    {
        return new Elevator
        {
            Id = id,
            Name = Name.Trim(),
            Address = Address.Trim(),
            BuildingName = BuildingName.Trim(),
            FloorLabel = FloorLabel.Trim(),
            Manufacturer = Manufacturer.Trim(),
            InstallationDate = InstallationDate,
            IsActive = IsActive
        };
    }

    private bool CanSave()
    {
        return !IsBusy && HasRequiredFields();
    }

    private bool CanUpdate()
    {
        return !IsBusy && SelectedElevator != null && HasRequiredFields();
    }

    private bool CanDelete()
    {
        return !IsBusy && SelectedElevator != null;
    }

    private bool HasRequiredFields()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(Address)
            && !string.IsNullOrWhiteSpace(BuildingName)
            && !string.IsNullOrWhiteSpace(FloorLabel)
            && !string.IsNullOrWhiteSpace(Manufacturer)
            && Latitude.HasValue
            && Longitude.HasValue;
    }

    private void ReplaceElevator(Elevator updated)
    {
        var index = Elevators
            .Select((elevator, position) => new { elevator, position })
            .FirstOrDefault(entry => entry.elevator.Id == updated.Id)?
            .position;

        if (index.HasValue)
        {
            Elevators[index.Value] = updated;
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
        SelectedElevator = null;
        Name = string.Empty;
        Address = string.Empty;
        BuildingName = string.Empty;
        FloorLabel = string.Empty;
        Manufacturer = string.Empty;
        InstallationDate = DateTime.UtcNow.Date;
        IsActive = true;
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
        ResetCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }
}
