using CarRentalERP.Application.Owners;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class OwnersController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OwnerDto>>>> GetList(
        [FromServices] OwnerService service,
        [FromQuery] OwnerListRequest request,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OwnerDetailDto>>> GetById(
        [FromServices] OwnerService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var owner = await service.GetByIdAsync(id, cancellationToken);
        return owner is null ? NotFound(ApiResponse<OwnerDetailDto>.Fail("Owner not found")) : OkResponse(owner);
    }

    [HttpGet("revenue")]
    [Authorize(Policy = AuthorizationPolicies.ViewReports)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OwnerRevenueDto>>>> GetRevenue(
        [FromServices] OwnerService service,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetRevenueSummaryAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OwnerDto>>> Create(
        [FromServices] OwnerService service,
        [FromBody] CreateOwnerRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Owner created");

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OwnerDto>>> Update(
        [FromServices] OwnerService service,
        Guid id,
        [FromBody] UpdateOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await service.UpdateAsync(id, request, cancellationToken);
        return owner is null ? NotFound(ApiResponse<OwnerDto>.Fail("Owner not found")) : OkResponse(owner, "Owner updated");
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<OwnerDto>>> SetStatus(
        [FromServices] OwnerService service,
        Guid id,
        [FromBody] SetOwnerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await service.SetStatusAsync(id, request, cancellationToken);
        return owner is null ? NotFound(ApiResponse<OwnerDto>.Fail("Owner not found")) : OkResponse(owner, request.IsActive ? "Owner activated" : "Owner deactivated");
    }
}
