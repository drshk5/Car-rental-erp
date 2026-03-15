using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarRentalERP.Api.Tests.Infrastructure;
using CarRentalERP.Application.Auth;
using CarRentalERP.Shared.Contracts;
using Xunit;

namespace CarRentalERP.Api.Tests;

public sealed class AuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenPair()
    {
        var (email, password) = await _factory.EnsureAuthUserAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.RefreshToken));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var (email, _) = await _factory.EnsureAuthUserAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewTokens()
    {
        var (email, password) = await _factory.EnsureAuthUserAsync();
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(loginPayload?.Data);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(loginPayload!.Data!.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(refreshPayload);
        Assert.True(refreshPayload!.Success);
        Assert.NotNull(refreshPayload.Data);
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.Data!.AccessToken));
    }

    [Fact]
    public async Task Me_WithBearerToken_ReturnsAuthenticatedProfile()
    {
        var (email, password) = await _factory.EnsureAuthUserAsync();
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(loginPayload?.Data);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Data!.AccessToken);

        var meResponse = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var mePayload = await meResponse.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        Assert.NotNull(mePayload);
        Assert.True(mePayload!.Success);
        Assert.NotNull(mePayload.Data);
        Assert.Equal(email, mePayload.Data!.Email);
    }
}
