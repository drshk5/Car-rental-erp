using CarRentalERP.Application.Users;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class UsersController : BaseApiController
{
    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetList(
        [FromServices] UserService service,
        [FromQuery] UserListRequest request,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetPagedAsync(request, cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetById(
        [FromServices] UserService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await service.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UserDetailDto>.Fail("User not found"));
        }

        return OkResponse(user);
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(
        [FromServices] UserService service,
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "User created");

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        [FromServices] UserService service,
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await service.UpdateAsync(id, request, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        }

        return OkResponse(user, "User updated");
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<UserDto>>> SetStatus(
        [FromServices] UserService service,
        Guid id,
        [FromBody] SetUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await service.SetStatusAsync(id, request, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        }

        return OkResponse(user, request.IsActive ? "User activated" : "User deactivated");
    }

    [Authorize(Policy = AuthorizationPolicies.ManageUsersAndRoles)]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ApiResponse<UserDto>>> ResetPassword(
        [FromServices] UserService service,
        Guid id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await service.ResetPasswordAsync(id, request, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        }

        return OkResponse(user, "User password reset");
    }
}
