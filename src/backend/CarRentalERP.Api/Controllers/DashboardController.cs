using CarRentalERP.Application.Dashboard;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class DashboardController : BaseApiController
{
    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.ViewReports)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(
        [FromServices] DashboardService service,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetSummaryAsync(cancellationToken));
    }
}
