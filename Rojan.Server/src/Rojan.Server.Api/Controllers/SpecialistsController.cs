using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rojan.Server.Application.Specialists;

namespace Rojan.Server.Api.Controllers;

/// <summary>
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. The second business
/// module endpoint set - <c>POST/GET api/v1/specialists</c>,
/// <c>GET/PUT api/v1/specialists/{id}</c>. Same shape as
/// <see cref="CustomersController"/>: all <see cref="AuthorizeAttribute"/>,
/// no organization id anywhere in a request/route - every action's tenant
/// comes from <c>Application.Tenancy.ITenantContext</c> inside
/// <see cref="ISpecialistService"/>, never from the client.
/// <see cref="SpecialistNotFoundException"/> is mapped to a plain 404
/// whether the specialist genuinely does not exist or belongs to a
/// different tenant - see that exception's own doc comment for why the
/// two must be indistinguishable from the outside.
/// </summary>
[ApiController]
[Route("api/v1/specialists")]
[Authorize]
public sealed class SpecialistsController : ControllerBase
{
    private readonly ISpecialistService _specialistService;

    public SpecialistsController(ISpecialistService specialistService)
    {
        _specialistService = specialistService;
    }

    [HttpPost]
    public async Task<ActionResult<SpecialistDto>> Create(CreateSpecialistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _specialistService.CreateSpecialistAsync(request, cancellationToken).ConfigureAwait(true);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidSpecialistBranchException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status400BadRequest });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpecialistDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _specialistService.GetSpecialistsAsync(cancellationToken).ConfigureAwait(true));

    [HttpGet("{id}")]
    public async Task<ActionResult<SpecialistDto>> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _specialistService.GetSpecialistAsync(id, cancellationToken).ConfigureAwait(true));
        }
        catch (SpecialistNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SpecialistDto>> Update(string id, UpdateSpecialistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _specialistService.UpdateSpecialistAsync(id, request, cancellationToken).ConfigureAwait(true));
        }
        catch (SpecialistNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidSpecialistBranchException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status400BadRequest });
        }
        catch (InvalidSpecialistStatusException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status400BadRequest });
        }
    }
}
