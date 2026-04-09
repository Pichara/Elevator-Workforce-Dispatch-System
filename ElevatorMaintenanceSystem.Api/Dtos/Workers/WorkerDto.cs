using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Api.Dtos.Workers;

public sealed class WorkerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public IReadOnlyList<string> Skills { get; init; } = [];
    public WorkerAvailabilityStatus AvailabilityStatus { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
