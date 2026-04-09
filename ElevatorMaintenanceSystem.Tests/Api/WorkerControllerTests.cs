using ElevatorMaintenanceSystem.Api.Dtos.Workers;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Tests.Api.Support;
using System.Net;

namespace ElevatorMaintenanceSystem.Tests.Api;

public class WorkerControllerTests : ApiTestBase
{
    public WorkerControllerTests(ApiWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenWorkersExist()
    {
        var client = CreateClient();
        var created = await CreateWorkerAsync(client);

        var response = await client.GetAsync("/api/workers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadListAsync<WorkerDto>(response.Content);
        Assert.Contains(payload, worker => worker.Id == created.Id);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenWorkerExists()
    {
        var client = CreateClient();
        var created = await CreateWorkerAsync(client, "get-by-id");

        var response = await client.GetAsync($"/api/workers/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadAsync<WorkerDto>(response.Content);
        Assert.Equal(created.Id, payload.Id);
        Assert.Equal(created.Email, payload.Email);
    }

    [Fact]
    public async Task CreateWorker_ReturnsCreated_WithValidData()
    {
        var client = CreateClient();
        var request = new CreateWorkerDto
        {
            Name = "Create Worker",
            Email = "create.worker@example.com",
            Phone = "555-2222",
            Skills = ["Inspection"],
            Latitude = 43.47,
            Longitude = -80.47
        };

        var response = await client.PostAsync("/api/workers", ApiJson.Content(request));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await ApiJson.ReadAsync<WorkerDto>(response.Content);
        Assert.Equal("Create Worker", payload.Name);
    }

    [Fact]
    public async Task UpdateWorker_ReturnsNoContent_WhenValid()
    {
        var client = CreateClient();
        var created = await CreateWorkerAsync(client, "update");
        var request = new UpdateWorkerDto
        {
            Name = "Updated Worker",
            Email = "updated.worker@example.com",
            Phone = "555-3333",
            Skills = ["Repair", "Install"],
            AvailabilityStatus = WorkerAvailabilityStatus.Assigned,
            Latitude = 43.49,
            Longitude = -80.46
        };

        var updateResponse = await client.PutAsync($"/api/workers/{created.Id}", ApiJson.Content(request));
        var getResponse = await client.GetAsync($"/api/workers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var payload = await ApiJson.ReadAsync<WorkerDto>(getResponse.Content);
        Assert.Equal("Updated Worker", payload.Name);
        Assert.Equal(WorkerAvailabilityStatus.Assigned, payload.AvailabilityStatus);
    }

    [Fact]
    public async Task UpdateLocation_ReturnsNoContent_WhenValid()
    {
        var client = CreateClient();
        var created = await CreateWorkerAsync(client, "location");
        var request = new UpdateLocationDto
        {
            Latitude = 43.50,
            Longitude = -80.45
        };

        var updateResponse = await client.PatchAsync($"/api/workers/{created.Id}/location", ApiJson.Content(request));
        var getResponse = await client.GetAsync($"/api/workers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var payload = await ApiJson.ReadAsync<WorkerDto>(getResponse.Content);
        Assert.Equal(43.50, payload.Latitude, 2);
        Assert.Equal(-80.45, payload.Longitude, 2);
    }

    [Fact]
    public async Task DeleteWorker_ReturnsNoContent_WhenValid()
    {
        var client = CreateClient();
        var created = await CreateWorkerAsync(client, "delete");

        var deleteResponse = await client.DeleteAsync($"/api/workers/{created.Id}");
        var getResponse = await client.GetAsync($"/api/workers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task WithoutApiKey_ReturnsUnauthorized()
    {
        var client = CreateClient(includeApiKey: false);

        var response = await client.GetAsync("/api/workers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
