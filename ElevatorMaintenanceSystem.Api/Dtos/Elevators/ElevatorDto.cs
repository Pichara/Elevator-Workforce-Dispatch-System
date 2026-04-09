namespace ElevatorMaintenanceSystem.Api.Dtos.Elevators;

public sealed class ElevatorDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string BuildingName { get; init; } = string.Empty;
    public string FloorLabel { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public DateTime InstallationDate { get; init; }
    public bool IsActive { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
