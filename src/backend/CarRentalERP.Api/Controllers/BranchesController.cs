using CarRentalERP.Application.Branches;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class BranchesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BranchDto>>>> GetList(
        [FromServices] BranchService service,
        [FromQuery] BranchListRequest request,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDetailDto>>> GetById(
        [FromServices] BranchService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var branch = await service.GetByIdAsync(id, cancellationToken);
        if (branch is null)
        {
            return NotFound(ApiResponse<BranchDetailDto>.Fail("Branch not found"));
        }

        return OkResponse(branch);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageBranches)]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Create(
        [FromServices] BranchService service,
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Branch created");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageBranches)]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Update(
        [FromServices] BranchService service,
        Guid id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await service.UpdateAsync(id, request, cancellationToken);
        if (branch is null)
        {
            return NotFound(ApiResponse<BranchDto>.Fail("Branch not found"));
        }

        return OkResponse(branch, "Branch updated");
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.ManageBranches)]
    public async Task<ActionResult<ApiResponse<BranchDto>>> SetStatus(
        [FromServices] BranchService service,
        Guid id,
        [FromBody] SetBranchStatusRequest request,
        CancellationToken cancellationToken)
    {
        var branch = await service.SetStatusAsync(id, request, cancellationToken);
        if (branch is null)
        {
            return NotFound(ApiResponse<BranchDto>.Fail("Branch not found"));
        }

        return OkResponse(branch, request.IsActive ? "Branch activated" : "Branch deactivated");
    }
}
