using CarRentalERP.Application.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class AuthController : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromServices] AuthService service,
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid credentials"));
        }

        return OkResponse(response, "Login successful");
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromServices] AuthService service,
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.RefreshAsync(request.RefreshToken, cancellationToken);
        if (response is null)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid refresh token"));
        }

        return OkResponse(response, "Token refreshed");
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Me(
        [FromServices] AuthService service,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Authenticated user context is missing."));
        }

        var profile = await service.GetProfileAsync(userId, cancellationToken);
        if (profile is null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Fail("User not found"));
        }

        return OkResponse(profile);
    }
}
