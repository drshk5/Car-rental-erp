using System.Net;
using System.Net.Http.Json;
using CarRentalERP.Api.Tests.Infrastructure;
using CarRentalERP.Application.Health;
using CarRentalERP.Shared.Contracts;
using Xunit;

namespace CarRentalERP.Api.Tests;

public sealed class SystemHealthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SystemHealthTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOkResponse()
    {
        var response = await _client.GetAsync("/api/v1/system/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SystemHealthDto>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
    }
}
