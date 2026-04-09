using ElevatorMaintenanceSystem.Api.Dtos.Elevators;
using ElevatorMaintenanceSystem.Api.Dtos.Tickets;
using ElevatorMaintenanceSystem.Api.Dtos.Workers;
using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Tests.Api.Support;

public abstract class ApiTestBase : IClassFixture<ApiWebApplicationFactory>
{
    protected ApiTestBase(ApiWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected ApiWebApplicationFactory Factory { get; }

    protected HttpClient CreateClient(bool includeApiKey = true)
    {
        return Factory.CreateApiClient(includeApiKey);
    }

    protected Guid SeedElevatorId => Factory.SeedElevatorId;
    protected Guid SeedWorkerId => Factory.SeedWorkerId;

    protected static async Task<ElevatorDto> CreateElevatorAsync(HttpClient client, string suffix = "alpha")
    {
        var request = new CreateElevatorDto
        {
            Name = $"Tower {suffix}",
            Address = $"{suffix} Main St",
            BuildingName = $"Building {suffix}",
            FloorLabel = "L1",
            Manufacturer = "Otis",
            InstallationDate = new DateTime(2020, 1, 1),
            Latitude = 43.45,
            Longitude = -80.49
        };

        return await CreateAsync<ElevatorDto>(client, "/api/elevators", request);
    }

    protected static async Task<WorkerDto> CreateWorkerAsync(HttpClient client, string suffix = "alpha")
    {
        var request = new CreateWorkerDto
        {
            Name = $"Worker {suffix}",
            Email = $"{suffix}@example.com",
            Phone = "555-1000",
            Skills = ["Repair", "Inspection"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available,
            Latitude = 43.46,
            Longitude = -80.48
        };

        return await CreateAsync<WorkerDto>(client, "/api/workers", request);
    }

    protected static async Task<TicketDto> CreateTicketAsync(HttpClient client, Guid elevatorId, string description = "Door stuck")
    {
        var request = new CreateTicketDto
        {
            ElevatorId = elevatorId,
            Description = description,
            IssueType = TicketIssueType.Mechanical,
            Priority = TicketPriority.High,
            RequestedDate = new DateTime(2026, 4, 9, 13, 0, 0, DateTimeKind.Utc)
        };

        return await CreateAsync<TicketDto>(client, "/api/tickets", request);
    }

    private static async Task<T> CreateAsync<T>(HttpClient client, string path, object payload)
    {
        var response = await client.PostAsync(path, ApiJson.Content(payload));
        response.EnsureSuccessStatusCode();
        return await ApiJson.ReadAsync<T>(response.Content);
    }
}
