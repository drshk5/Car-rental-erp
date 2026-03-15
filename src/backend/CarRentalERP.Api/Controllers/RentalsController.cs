using CarRentalERP.Application.Rentals;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class RentalsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<RentalListResponse>>> GetList(
        [FromServices] RentalService service,
        [FromQuery] RentalListRequest request,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RentalDto>>>> GetActive(
        [FromServices] RentalService service,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetActiveAsync(cancellationToken));
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RentalDto>>>> GetOverdue(
        [FromServices] RentalService service,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetOverdueAsync(cancellationToken));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<RentalStatsDto>>> GetStats(
        [FromServices] RentalService service,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetStatsAsync(cancellationToken));
    }

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<ApiResponse<RentalDashboardSummaryDto>>> GetDashboardSummary(
        [FromServices] RentalService service,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetDashboardSummaryAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RentalDetailDto>>> GetById(
        [FromServices] RentalService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var rental = await service.GetByIdAsync(id, cancellationToken);
        if (rental is null)
        {
            return NotFound(ApiResponse<RentalDetailDto>.Fail("Rental not found"));
        }

        return OkResponse(rental);
    }

    [HttpPost("checkout")]
    [Authorize(Policy = AuthorizationPolicies.CheckoutCheckin)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Checkout(
        [FromServices] RentalService service,
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CheckoutAsync(request, cancellationToken), "Checkout completed");

    [HttpPost("{id:guid}/checkin")]
    [Authorize(Policy = AuthorizationPolicies.CheckoutCheckin)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Checkin(
        [FromServices] RentalService service,
        Guid id,
        [FromBody] CheckinRequest request,
        CancellationToken cancellationToken)
    {
        var rental = await service.CheckinAsync(id, request, cancellationToken);
        if (rental is null)
        {
            return NotFound(ApiResponse<RentalDto>.Fail("Rental not found"));
        }

        return OkResponse(rental, "Check-in completed");
    }

    [HttpPatch("{id:guid}/damage")]
    [Authorize(Policy = AuthorizationPolicies.CheckoutCheckin)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> UpdateDamageNotes(
        [FromServices] RentalService service,
        Guid id,
        [FromBody] UpdateRentalDamageRequest request,
        CancellationToken cancellationToken)
    {
        var rental = await service.UpdateDamageNotesAsync(id, request, cancellationToken);
        if (rental is null)
        {
            return NotFound(ApiResponse<RentalDto>.Fail("Rental not found"));
        }

        return OkResponse(rental, "Damage notes updated");
    }
}
