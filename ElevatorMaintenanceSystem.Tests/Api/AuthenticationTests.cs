using ElevatorMaintenanceSystem.Tests.Api.Support;
using System.Net;

namespace ElevatorMaintenanceSystem.Tests.Api;

public class AuthenticationTests : ApiTestBase
{
    public AuthenticationTests(ApiWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RequestWithoutApiKey_ReturnsUnauthorized()
    {
        var client = CreateClient(includeApiKey: false);

        var response = await client.GetAsync("/api/elevators");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequestWithInvalidApiKey_ReturnsUnauthorized()
    {
        var client = CreateClient(includeApiKey: false);
        client.DefaultRequestHeaders.Add("X-API-Key", "wrong-key");

        var response = await client.GetAsync("/api/workers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
