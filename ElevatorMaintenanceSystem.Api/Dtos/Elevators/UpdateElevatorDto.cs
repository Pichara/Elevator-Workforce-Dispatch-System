using System.ComponentModel.DataAnnotations;

namespace ElevatorMaintenanceSystem.Api.Dtos.Elevators;

public sealed class UpdateElevatorDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; init; } = string.Empty;

    [StringLength(200)]
    public string BuildingName { get; init; } = string.Empty;

    [StringLength(50)]
    public string FloorLabel { get; init; } = string.Empty;

    [StringLength(200)]
    public string Manufacturer { get; init; } = string.Empty;

    public DateTime InstallationDate { get; init; }

    public bool IsActive { get; init; } = true;

    [Range(-90d, 90d)]
    public double Latitude { get; init; }

    [Range(-180d, 180d)]
    public double Longitude { get; init; }
}
