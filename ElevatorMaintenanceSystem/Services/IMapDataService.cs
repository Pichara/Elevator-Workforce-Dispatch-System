namespace ElevatorMaintenanceSystem.Services;

/// <summary>
/// Service for building map data snapshots containing elevator and worker markers
/// </summary>
public interface IMapDataService
{
    /// <summary>
    /// Build a snapshot of all active elevators and workers for map visualization
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Map data snapshot with markers and tile settings</returns>
    Task<MapDataSnapshot> BuildSnapshotAsync(CancellationToken cancellationToken = default);
}
