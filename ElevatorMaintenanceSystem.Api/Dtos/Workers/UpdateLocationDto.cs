using System.ComponentModel.DataAnnotations;

namespace ElevatorMaintenanceSystem.Api.Dtos.Workers;

public sealed class UpdateLocationDto
{
    [Range(-90d, 90d)]
    public double Latitude { get; init; }

    [Range(-180d, 180d)]
    public double Longitude { get; init; }
}
