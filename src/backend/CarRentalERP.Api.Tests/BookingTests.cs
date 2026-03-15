using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarRentalERP.Api.Tests.Infrastructure;
using CarRentalERP.Application.Auth;
using CarRentalERP.Application.Bookings;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;
using Xunit;

namespace CarRentalERP.Api.Tests;

public sealed class BookingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BookingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBooking_ReturnsDraftBooking()
    {
        var client = await CreateAuthorizedClientAsync();
        var scenario = await _factory.CreateBookingScenarioAsync();
        var request = BuildBookingRequest(scenario);

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(payload?.Data);
        Assert.True(payload!.Success);
        Assert.Equal(BookingStatus.Draft, payload.Data!.Status);
    }

    [Fact]
    public async Task ConfirmBooking_TransitionsDraftToConfirmed()
    {
        var client = await CreateAuthorizedClientAsync();
        var scenario = await _factory.CreateBookingScenarioAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/bookings", BuildBookingRequest(scenario));
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(createPayload?.Data);

        var confirmResponse = await client.PostAsync($"/api/v1/bookings/{createPayload!.Data!.Id}/confirm", content: null);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(confirmPayload?.Data);
        Assert.Equal(BookingStatus.Confirmed, confirmPayload!.Data!.Status);
    }

    [Fact]
    public async Task CancelBooking_TransitionsConfirmedToCancelled()
    {
        var client = await CreateAuthorizedClientAsync();
        var scenario = await _factory.CreateBookingScenarioAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/bookings", BuildBookingRequest(scenario));
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(createPayload?.Data);

        var confirmResponse = await client.PostAsync($"/api/v1/bookings/{createPayload!.Data!.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var cancelResponse = await client.PostAsync($"/api/v1/bookings/{createPayload.Data.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var cancelPayload = await cancelResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(cancelPayload?.Data);
        Assert.Equal(BookingStatus.Cancelled, cancelPayload!.Data!.Status);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var (email, password) = await _factory.EnsureAuthUserAsync();
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(loginPayload?.Data);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Data!.AccessToken);
        return client;
    }

    private static CreateBookingRequest BuildBookingRequest(TestWebApplicationFactory.BookingScenario scenario)
    {
        var startAtUtc = DateTime.UtcNow.AddDays(2);
        var endAtUtc = startAtUtc.AddDays(2);

        return new CreateBookingRequest(
            scenario.CustomerId,
            scenario.VehicleId,
            scenario.PickupBranchId,
            scenario.ReturnBranchId,
            startAtUtc,
            endAtUtc,
            PricingPlan.Daily,
            10,
            25);
    }
}
