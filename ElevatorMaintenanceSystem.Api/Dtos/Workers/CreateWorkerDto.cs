using ElevatorMaintenanceSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ElevatorMaintenanceSystem.Api.Dtos.Workers;

public sealed class CreateWorkerDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [StringLength(50)]
    public string Phone { get; init; } = string.Empty;

    public List<string> Skills { get; init; } = [];

    public WorkerAvailabilityStatus AvailabilityStatus { get; init; } = WorkerAvailabilityStatus.Available;

    [Range(-90d, 90d)]
    public double Latitude { get; init; }

    [Range(-180d, 180d)]
    public double Longitude { get; init; }
}
