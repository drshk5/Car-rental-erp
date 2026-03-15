using CarRentalERP.Application.Customers;
using CarRentalERP.Api.Auth;
using CarRentalERP.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Controllers;

public sealed class CustomersController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerDto>>>> GetList(
        [FromServices] CustomerService service,
        [FromQuery] CustomerListRequest request,
        CancellationToken cancellationToken)
    {
        return OkResponse(await service.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDetailDto>>> GetById(
        [FromServices] CustomerService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var customer = await service.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound(ApiResponse<CustomerDetailDto>.Fail("Customer not found"));
        }

        return OkResponse(customer);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create(
        [FromServices] CustomerService service,
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
        => OkResponse(await service.CreateAsync(request, cancellationToken), "Customer created");

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(
        [FromServices] CustomerService service,
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await service.UpdateAsync(id, request, cancellationToken);
        if (customer is null)
        {
            return NotFound(ApiResponse<CustomerDto>.Fail("Customer not found"));
        }

        return OkResponse(customer, "Customer updated");
    }

    [HttpPatch("{id:guid}/verification")]
    [Authorize(Policy = AuthorizationPolicies.VerifyCustomer)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> SetVerification(
        [FromServices] CustomerService service,
        Guid id,
        [FromBody] SetCustomerVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await service.SetVerificationAsync(id, request, cancellationToken);
        if (customer is null)
        {
            return NotFound(ApiResponse<CustomerDto>.Fail("Customer not found"));
        }

        return OkResponse(customer, "Customer verification updated");
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> SetStatus(
        [FromServices] CustomerService service,
        Guid id,
        [FromBody] SetCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await service.SetStatusAsync(id, request, cancellationToken);
        if (customer is null)
        {
            return NotFound(ApiResponse<CustomerDto>.Fail("Customer not found"));
        }

        return OkResponse(customer, request.IsActive ? "Customer reactivated" : "Customer archived");
    }
}
