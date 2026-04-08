using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.Services;
using ElevatorMaintenanceSystem.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElevatorMaintenanceSystem.Tests.ViewModels;

public class MapViewModelDispatchTests
{
    [Fact]
    public async Task MAP_05_D_01_D_02_HandleWorkerDroppedOnElevatorAsync_WithoutSelectedTicket_ShowsUserErrorStatusAndSkipsAssignment()
    {
        var dispatchService = new StubMapDispatchService();
        var viewModel = CreateViewModel(dispatchService);

        await viewModel.HandleWorkerDroppedOnElevatorAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(0, dispatchService.AssignCallCount);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.MapErrorMessage));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.StatusMessage));
        Assert.Contains("ticket", viewModel.MapErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ticket", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MAP_06_D_03_HandleElevatorFocusedAsync_LoadsSelectedElevatorTicketContext()
    {
        var elevatorId = Guid.NewGuid();
        var dispatchService = new StubMapDispatchService
        {
            ContextToReturn = new ElevatorTicketContext(
                elevatorId,
                [
                    new ElevatorTicketSummary(Guid.NewGuid(), "Door issue", Models.TicketPriority.Critical, Models.TicketStatus.Pending, new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc), null)
                ])
        };
        var viewModel = CreateViewModel(dispatchService);

        await viewModel.HandleElevatorFocusedAsync(elevatorId);

        Assert.Equal(elevatorId, viewModel.SelectedElevatorId);
        Assert.Single(viewModel.SelectedElevatorTickets);
        Assert.Equal(1, dispatchService.LoadCallCount);
    }

    private static MapViewModel CreateViewModel(IMapDispatchService dispatchService)
    {
        return new MapViewModel(
            new StubMapDataService(),
            new MapSettings
            {
                DefaultCenterLatitude = 43.4516,
                DefaultCenterLongitude = -80.4925,
                DefaultZoom = 10,
                DefaultBaseLayer = "standard"
            },
            dispatchService,
            NullLogger<MapViewModel>.Instance);
    }

    private sealed class StubMapDispatchService : IMapDispatchService
    {
        public int AssignCallCount { get; private set; }
        public int LoadCallCount { get; private set; }
        public ElevatorTicketContext? ContextToReturn { get; set; }

        public Task<ElevatorTicketContext> LoadElevatorTicketContextAsync(Guid elevatorId, CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            return Task.FromResult(ContextToReturn ?? new ElevatorTicketContext(elevatorId, []));
        }

        public Task<MapAssignmentResult> AssignWorkerToTicketAsync(Guid ticketId, Guid workerId, CancellationToken cancellationToken = default)
        {
            AssignCallCount++;
            return Task.FromResult(new MapAssignmentResult(true, ticketId, workerId, "Worker assigned."));
        }
    }

    private sealed class StubMapDataService : IMapDataService
    {
        public Task<MapDataSnapshot> BuildSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MapDataSnapshot(
                CenterLatitude: 43.4516,
                CenterLongitude: -80.4925,
                Zoom: 10,
                Markers: [],
                StandardTiles: new TileProviderSnapshot("OpenStreetMap", "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", "© OpenStreetMap", 19, null),
                SatelliteTiles: new TileProviderSnapshot("ArcGIS", "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}", "© Esri", 19, null)));
        }
    }
}
