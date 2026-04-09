using ElevatorMaintenanceSystem.Api.Dtos.Elevators;
using ElevatorMaintenanceSystem.Tests.Api.Support;
using System.Net;

namespace ElevatorMaintenanceSystem.Tests.Api;

public class ElevatorControllerTests : ApiTestBase
{
    public ElevatorControllerTests(ApiWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenElevatorsExist()
    {
        var client = CreateClient();
        var created = await CreateElevatorAsync(client);

        var response = await client.GetAsync("/api/elevators");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadListAsync<ElevatorDto>(response.Content);
        Assert.Contains(payload, elevator => elevator.Id == created.Id);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenElevatorExists()
    {
        var client = CreateClient();
        var created = await CreateElevatorAsync(client, "get-by-id");

        var response = await client.GetAsync($"/api/elevators/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadAsync<ElevatorDto>(response.Content);
        Assert.Equal(created.Id, payload.Id);
        Assert.Equal(created.Name, payload.Name);
    }

    [Fact]
    public async Task CreateElevator_ReturnsCreated_WithValidData()
    {
        var client = CreateClient();
        var request = new CreateElevatorDto
        {
            Name = "Tower Create",
            Address = "200 Main St",
            BuildingName = "Tower",
            FloorLabel = "L2",
            Manufacturer = "Kone",
            InstallationDate = new DateTime(2021, 1, 1),
            Latitude = 43.44,
            Longitude = -80.41
        };

        var response = await client.PostAsync("/api/elevators", ApiJson.Content(request));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var payload = await ApiJson.ReadAsync<ElevatorDto>(response.Content);
        Assert.Equal("Tower Create", payload.Name);
    }

    [Fact]
    public async Task UpdateElevator_ReturnsNoContent_WhenValid()
    {
        var client = CreateClient();
        var created = await CreateElevatorAsync(client, "update");
        var request = new UpdateElevatorDto
        {
            Name = "Updated Tower",
            Address = "300 Main St",
            BuildingName = "Updated Building",
            FloorLabel = "L3",
            Manufacturer = "Schindler",
            InstallationDate = new DateTime(2022, 1, 1),
            IsActive = true,
            Latitude = 43.40,
            Longitude = -80.40
        };

        var updateResponse = await client.PutAsync($"/api/elevators/{created.Id}", ApiJson.Content(request));
        var getResponse = await client.GetAsync($"/api/elevators/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var payload = await ApiJson.ReadAsync<ElevatorDto>(getResponse.Content);
        Assert.Equal("Updated Tower", payload.Name);
        Assert.Equal(43.40, payload.Latitude, 2);
    }

    [Fact]
    public async Task DeleteElevator_ReturnsNoContent_WhenValid()
    {
        var client = CreateClient();
        var created = await CreateElevatorAsync(client, "delete");
        var deactivateRequest = new UpdateElevatorDto
        {
            Name = created.Name,
            Address = created.Address,
            BuildingName = created.BuildingName,
            FloorLabel = created.FloorLabel,
            Manufacturer = created.Manufacturer,
            InstallationDate = created.InstallationDate,
            IsActive = false,
            Latitude = created.Latitude,
            Longitude = created.Longitude
        };

        var deactivateResponse = await client.PutAsync($"/api/elevators/{created.Id}", ApiJson.Content(deactivateRequest));
        var deleteResponse = await client.DeleteAsync($"/api/elevators/{created.Id}");
        var getResponse = await client.GetAsync($"/api/elevators/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task WithoutApiKey_ReturnsUnauthorized()
    {
        var client = CreateClient(includeApiKey: false);

        var response = await client.GetAsync("/api/elevators");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
