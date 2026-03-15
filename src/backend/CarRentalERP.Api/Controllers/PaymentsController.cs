using CarRentalERP.Application.Payments;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class PaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentDto>>>> GetList(
        [FromServices] PaymentService service,
        [FromQuery] PaymentListRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentDetailDto>>> GetById(
        [FromServices] PaymentService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await service.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return NotFound(ApiResponse<PaymentDetailDto>.Fail("Payment not found"));
        }

        return OkResponse(payment);
    }

    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentDto>>>> GetByBooking(
        [FromServices] PaymentService service,
        Guid bookingId,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetByBookingAsync(bookingId, cancellationToken));

    [HttpGet("summary/{bookingId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentSummaryDto>>> GetSummary(
        [FromServices] PaymentService service,
        Guid bookingId,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetSummaryAsync(bookingId, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RecordPayment)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Record(
        [FromServices] PaymentService service,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.RecordAsync(request, cancellationToken), "Payment recorded");

    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = AuthorizationPolicies.RefundPayment)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Refund(
        [FromServices] PaymentService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await service.RefundAsync(id, cancellationToken);
        if (payment is null)
        {
            return NotFound(ApiResponse<PaymentDto>.Fail("Payment not found or already refunded"));
        }

        return OkResponse(payment, "Payment refunded");
    }
}
