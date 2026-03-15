using CarRentalERP.Application.Health;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class SystemController : BaseApiController
{
    [AllowAnonymous]
    [HttpGet("health")]
    public ActionResult<ApiResponse<SystemHealthDto>> Health([FromServices] SystemHealthService service)
    {
        return OkResponse(service.GetSummary(HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName));
    }
}
