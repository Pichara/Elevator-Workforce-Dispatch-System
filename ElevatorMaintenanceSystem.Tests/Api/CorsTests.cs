using ElevatorMaintenanceSystem.Tests.Api.Support;
using System.Net;

namespace ElevatorMaintenanceSystem.Tests.Api;

public class CorsTests : ApiTestBase
{
    public CorsTests(ApiWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RequestFromLocalhost_Allowed()
    {
        var client = CreateClient(includeApiKey: false);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/elevators");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK,
            $"Expected preflight success but got {(int)response.StatusCode}.");
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("http://localhost:5173", values.Single());
    }

    [Fact]
    public async Task RequestFromNonLocalhost_Rejected()
    {
        var client = CreateClient(includeApiKey: false);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/elevators");
        request.Headers.Add("Origin", "http://malicious.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
