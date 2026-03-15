using CarRentalERP.Application.Vehicles;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class VehiclesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleDto>>>> GetList(
        [FromServices] VehicleService service,
        [FromQuery] VehicleListRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.GetPagedAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VehicleDetailDto>>> GetById(
        [FromServices] VehicleService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var vehicle = await service.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
        {
            return NotFound(ApiResponse<VehicleDetailDto>.Fail("Vehicle not found"));
        }

        return OkResponse(vehicle);
    }

    [HttpGet("availability")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<VehicleDto>>>> Availability(
        [FromServices] VehicleService service,
        [FromQuery] VehicleAvailabilityRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.SearchAvailableAsync(request, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AddEditVehicles)]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> Create(
        [FromServices] VehicleService service,
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Vehicle created");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AddEditVehicles)]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> Update(
        [FromServices] VehicleService service,
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await service.UpdateAsync(id, request, cancellationToken);
        if (vehicle is null)
        {
            return NotFound(ApiResponse<VehicleDto>.Fail("Vehicle not found"));
        }

        return OkResponse(vehicle, "Vehicle updated");
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.DeactivateVehicle)]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> SetStatus(
        [FromServices] VehicleService service,
        Guid id,
        [FromBody] SetVehicleStatusRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await service.SetStatusAsync(id, request, cancellationToken);
        if (vehicle is null)
        {
            return NotFound(ApiResponse<VehicleDto>.Fail("Vehicle not found"));
        }

        return OkResponse(vehicle, "Vehicle status updated");
    }
}
