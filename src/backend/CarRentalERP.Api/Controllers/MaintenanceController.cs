using CarRentalERP.Application.Maintenance;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class MaintenanceController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MaintenanceDto>>>> GetList(
        [FromServices] MaintenanceService service,
        [FromQuery] MaintenanceListRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaintenanceDetailDto>>> GetById(
        [FromServices] MaintenanceService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await service.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            return NotFound(ApiResponse<MaintenanceDetailDto>.Fail("Maintenance record not found"));
        }

        return OkResponse(record);
    }

    [HttpGet("vehicle/{vehicleId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MaintenanceDto>>>> GetByVehicle(
        [FromServices] MaintenanceService service,
        Guid vehicleId,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetByVehicleAsync(vehicleId, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AddEditVehicles)]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Create(
        [FromServices] MaintenanceService service,
        [FromBody] CreateMaintenanceRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Maintenance created");

    [HttpPatch("{id:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.AddEditVehicles)]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Complete(
        [FromServices] MaintenanceService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await service.CompleteAsync(id, cancellationToken);
        if (record is null)
        {
            return NotFound(ApiResponse<MaintenanceDto>.Fail("Maintenance record not found"));
        }

        return OkResponse(record, "Maintenance completed");
    }
}
