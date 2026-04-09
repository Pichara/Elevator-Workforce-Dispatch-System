using ElevatorMaintenanceSystem.Api.Dtos.Tickets;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Tests.Api.Support;
using System.Net;

namespace ElevatorMaintenanceSystem.Tests.Api;

public class TicketControllerTests : ApiTestBase
{
    public TicketControllerTests(ApiWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenTicketsExist()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId);

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadListAsync<TicketDto>(response.Content);
        Assert.Contains(payload, item => item.Id == ticket.Id);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenTicketExists()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId);

        var response = await client.GetAsync($"/api/tickets/{ticket.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadAsync<TicketDto>(response.Content);
        Assert.Equal(ticket.Id, payload.Id);
    }

    [Fact]
    public async Task CreateTicket_ReturnsCreated_WithValidData()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/tickets", ApiJson.Content(new CreateTicketDto
        {
            ElevatorId = SeedElevatorId,
            Description = "Motor issue",
            IssueType = TicketIssueType.Mechanical,
            Priority = TicketPriority.High,
            RequestedDate = new DateTime(2026, 4, 9, 14, 0, 0, DateTimeKind.Utc)
        }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await ApiJson.ReadAsync<TicketDto>(response.Content);
        Assert.Equal(TicketStatus.Pending, payload.Status);
    }

    [Fact]
    public async Task CreateTicket_ReturnsBadRequest_WithInvalidData()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/tickets", ApiJson.Content(new CreateTicketDto
        {
            ElevatorId = SeedElevatorId,
            Description = string.Empty,
            IssueType = TicketIssueType.Mechanical,
            Priority = TicketPriority.High,
            RequestedDate = DateTime.UtcNow
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignWorker_ReturnsOk_WhenValid()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId);

        var response = await client.PostAsync(
            $"/api/tickets/{ticket.Id}/assign",
            ApiJson.Content(new AssignWorkerDto { WorkerId = SeedWorkerId }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadAsync<TicketDto>(response.Content);
        Assert.Equal(SeedWorkerId, payload.AssignedWorkerId);
    }

    [Fact]
    public async Task UnassignWorker_ReturnsOk_WhenValid()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId);
        await client.PostAsync(
            $"/api/tickets/{ticket.Id}/assign",
            ApiJson.Content(new AssignWorkerDto { WorkerId = SeedWorkerId }));

        var response = await client.PostAsync($"/api/tickets/{ticket.Id}/unassign", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadAsync<TicketDto>(response.Content);
        Assert.Null(payload.AssignedWorkerId);
        Assert.Equal(TicketStatus.Pending, payload.Status);
    }

    [Fact]
    public async Task ChangeStatus_ReturnsOk_WhenValid()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId);
        await client.PostAsync(
            $"/api/tickets/{ticket.Id}/assign",
            ApiJson.Content(new AssignWorkerDto { WorkerId = SeedWorkerId }));

        var response = await client.PatchAsync(
            $"/api/tickets/{ticket.Id}/status",
            ApiJson.Content(new ChangeStatusDto { Status = TicketStatus.InProgress }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadAsync<TicketDto>(response.Content);
        Assert.Equal(TicketStatus.InProgress, payload.Status);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsMatchingTickets()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId, "Filter me");

        var response = await client.GetAsync("/api/tickets?status=Pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ApiJson.ReadListAsync<TicketDto>(response.Content);
        Assert.Contains(payload, item => item.Id == ticket.Id);
    }

    [Fact]
    public async Task DeleteCanceledTicket_ReturnsNoContent()
    {
        var client = CreateClient();
        var ticket = await CreateTicketAsync(client, SeedElevatorId, "Cancel me");

        await client.PatchAsync(
            $"/api/tickets/{ticket.Id}/status",
            ApiJson.Content(new ChangeStatusDto { Status = TicketStatus.Canceled }));

        var deleteResponse = await client.DeleteAsync($"/api/tickets/{ticket.Id}");
        var getResponse = await client.GetAsync($"/api/tickets/{ticket.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task WithoutApiKey_ReturnsUnauthorized()
    {
        var client = CreateClient(includeApiKey: false);

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
