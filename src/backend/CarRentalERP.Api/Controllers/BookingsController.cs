using CarRentalERP.Application.Bookings;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class BookingsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> GetList(
        [FromServices] BookingService service,
        [FromQuery] BookingListRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BookingDetailDto>>> GetById(
        [FromServices] BookingService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var booking = await service.GetByIdAsync(id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<BookingDetailDto>.Fail("Booking not found"));
        }

        return OkResponse(booking);
    }

    [HttpGet("quote")]
    public async Task<ActionResult<ApiResponse<BookingQuoteDto>>> Quote(
        [FromServices] BookingService service,
        [FromQuery] BookingQuoteRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.QuoteAsync(request, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CreateEditBooking)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create(
        [FromServices] BookingService service,
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Booking created");

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = AuthorizationPolicies.CreateEditBooking)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Confirm(
        [FromServices] BookingService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var booking = await service.ConfirmAsync(id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<BookingDto>.Fail("Booking not found"));
        }

        return OkResponse(booking, "Booking confirmed");
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.CancelBooking)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(
        [FromServices] BookingService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var booking = await service.CancelAsync(id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<BookingDto>.Fail("Booking not found"));
        }

        return OkResponse(booking, "Booking cancelled");
    }
}
