using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarRentalERP.Api.Tests.Infrastructure;
using CarRentalERP.Application.Auth;
using CarRentalERP.Application.Bookings;
using CarRentalERP.Application.Payments;
using CarRentalERP.Application.Rentals;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;
using Xunit;

namespace CarRentalERP.Api.Tests;

public sealed class RentalPaymentTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RentalPaymentTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Checkout_WithConfirmedBooking_ReturnsActiveRental()
    {
        var client = await CreateAuthorizedClientAsync();
        var booking = await CreateAndConfirmBookingAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/rentals/checkout", new CheckoutRequest(
            booking.Id,
            12500,
            "Full",
            "Ready for pickup"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RentalDto>>();
        Assert.NotNull(payload?.Data);
        Assert.True(payload!.Success);
        Assert.Equal(RentalStatuses.Active, payload.Data!.Status);
        Assert.Equal(booking.Id, payload.Data.BookingId);
    }

    [Fact]
    public async Task Checkin_WithActiveRental_CompletesRental()
    {
        var client = await CreateAuthorizedClientAsync();
        var booking = await CreateAndConfirmBookingAsync(client);

        var checkoutResponse = await client.PostAsJsonAsync("/api/v1/rentals/checkout", new CheckoutRequest(
            booking.Id,
            15000,
            "Full",
            "Checkout test"));
        var checkoutPayload = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<RentalDto>>();
        Assert.NotNull(checkoutPayload?.Data);

        var checkinResponse = await client.PostAsJsonAsync(
            $"/api/v1/rentals/{checkoutPayload!.Data!.Id}/checkin",
            new CheckinRequest(15240, "Half", 35, "Minor scratch"));

        Assert.Equal(HttpStatusCode.OK, checkinResponse.StatusCode);

        var checkinPayload = await checkinResponse.Content.ReadFromJsonAsync<ApiResponse<RentalDto>>();
        Assert.NotNull(checkinPayload?.Data);
        Assert.Equal(RentalStatuses.Completed, checkinPayload!.Data!.Status);
        Assert.Equal(240, checkinPayload.Data.DistanceTravelled);
        Assert.Equal(35, checkinPayload.Data.ExtraCharges);
    }

    [Fact]
    public async Task RecordPayment_ForBooking_ReturnsPaidPayment()
    {
        var client = await CreateAuthorizedClientAsync();
        var booking = await CreateAndConfirmBookingAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new RecordPaymentRequest(
            booking.Id,
            50,
            PaymentMethod.Cash,
            string.Empty,
            DateTime.UtcNow,
            "Advance collected"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>();
        Assert.NotNull(payload?.Data);
        Assert.True(payload!.Success);
        Assert.Equal(PaymentStatus.Paid, payload.Data!.PaymentStatus);
        Assert.Equal(booking.Id, payload.Data.BookingId);
        Assert.Equal(50, payload.Data.Amount);
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

    private async Task<BookingDto> CreateAndConfirmBookingAsync(HttpClient client)
    {
        var scenario = await _factory.CreateBookingScenarioAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            scenario.CustomerId,
            scenario.VehicleId,
            scenario.PickupBranchId,
            scenario.ReturnBranchId,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(4),
            PricingPlan.Daily,
            10,
            25));

        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(createPayload?.Data);

        var confirmResponse = await client.PostAsync($"/api/v1/bookings/{createPayload!.Data!.Id}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>();
        Assert.NotNull(confirmPayload?.Data);
        Assert.Equal(BookingStatus.Confirmed, confirmPayload!.Data!.Status);

        return confirmPayload.Data;
    }
}
