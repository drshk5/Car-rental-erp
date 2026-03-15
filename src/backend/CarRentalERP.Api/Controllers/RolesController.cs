using CarRentalERP.Application.Roles;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class RolesController : BaseApiController
{
    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleDto>>>> GetList(
        [FromServices] RoleService service,
        [FromQuery] RoleListRequest request,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetAllAsync(request, cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> GetById(
        [FromServices] RoleService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var role = await service.GetByIdAsync(id, cancellationToken);
        return role is null ? NotFound(ApiResponse<RoleDetailDto>.Fail("Role not found")) : OkResponse(role);
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PermissionCatalogDto>>>> GetPermissions(
        [FromServices] RoleService service,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetPermissionCatalogAsync(cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(
        [FromServices] RoleService service,
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Role created");

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(
        [FromServices] RoleService service,
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await service.UpdateAsync(id, request, cancellationToken);
        return role is null ? NotFound(ApiResponse<RoleDto>.Fail("Role not found")) : OkResponse(role, "Role updated");
    }
}
