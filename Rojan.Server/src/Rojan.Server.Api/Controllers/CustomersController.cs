using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rojan.Server.Application.Customers;

namespace Rojan.Server.Api.Controllers;

/// <summary>
/// Sprint 8 Commit 4: Tenant-Aware Customer API. The first business
/// module endpoint set - <c>POST/GET api/v1/customers</c>,
/// <c>GET/PUT api/v1/customers/{id}</c>. All <see cref="AuthorizeAttribute"/>,
/// same as <see cref="TenantController"/> - a caller with no valid bearer
/// token has no tenant's customers to act on. No organization id anywhere
/// in a request/route here - every action's tenant comes from
/// <c>Application.Tenancy.ITenantContext</c> inside
/// <c>ICustomerService</c>, never from the client (see
/// <see cref="ICustomerService"/>'s own doc comment).
/// <see cref="CustomerNotFoundException"/> is mapped to a plain 404
/// whether the customer genuinely does not exist or belongs to a
/// different tenant - see that exception's own doc comment for why the
/// two must be indistinguishable from the outside.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _customerService.CreateCustomerAsync(request, cancellationToken).ConfigureAwait(true);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidCustomerBranchException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status400BadRequest });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _customerService.GetCustomersAsync(cancellationToken).ConfigureAwait(true));

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _customerService.GetCustomerAsync(id, cancellationToken).ConfigureAwait(true));
        }
        catch (CustomerNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> Update(string id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _customerService.UpdateCustomerAsync(id, request, cancellationToken).ConfigureAwait(true));
        }
        catch (CustomerNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidCustomerBranchException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status400BadRequest });
        }
        catch (InvalidCustomerStatusException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status400BadRequest });
        }
    }
}
