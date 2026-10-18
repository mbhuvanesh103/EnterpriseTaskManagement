using EnterpriseTaskManagement.API.Contracts.Departments;
using EnterpriseTaskManagement.Application.Features.Departments.Commands.CreateDepartment;
using EnterpriseTaskManagement.Application.Features.Departments.Commands.DeleteDepartment;
using EnterpriseTaskManagement.Application.Features.Departments.Commands.UpdateDepartment;
using EnterpriseTaskManagement.Application.Features.Departments.Queries.GetAllDepartments;
using EnterpriseTaskManagement.Application.Features.Departments.Queries.GetDepartmentByPublicId;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

/// <summary>
/// HTTP surface of the Department aggregate. The controller is a thin transport
/// adapter: it depends only on <see cref="ISender"/> (MediatR), holds no business
/// logic, no validation logic and no persistence types. Authorization is the
/// standard framework attribute over the existing JWT role claim: writes are
/// Admin-only, reads are open to every authenticated organizational role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ISender _sender;

    public DepartmentsController(ISender sender) => _sender = sender;

    /// <summary>Creates a new department.</summary>
    /// <response code="201">Department created; the Location header points at the new resource.</response>
    /// <response code="400">Input shape or domain invariant rejected the request.</response>
    /// <response code="401">Missing, expired or invalid bearer token.</response>
    /// <response code="403">Authenticated, but the role is not Admin.</response>
    /// <response code="409">The code already belongs to another department.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateDepartmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateDepartmentResponse>> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateDepartmentCommand(
            request.Name,
            request.Code,
            request.Description,
            request.IsActive);

        CreateDepartmentResponse response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetByPublicId),
            new { publicId = response.PublicId },
            response);
    }

    /// <summary>Lists every department that is not soft-deleted.</summary>
    /// <response code="200">The department collection (possibly empty).</response>
    /// <response code="401">Missing, expired or invalid bearer token.</response>
    /// <response code="403">Authenticated, but the role is not an organizational role.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetAllDepartmentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetAllDepartmentsResponse>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAllDepartmentsQuery(), cancellationToken));

    /// <summary>Reads a single department by its externally shareable identifier.</summary>
    /// <response code="200">The requested department.</response>
    /// <response code="404">No such department, or it is soft-deleted.</response>
    [HttpGet("{publicId:guid}")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetDepartmentByPublicIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetDepartmentByPublicIdResponse>> GetByPublicId(
        Guid publicId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetDepartmentByPublicIdQuery(publicId), cancellationToken));

    /// <summary>Updates a department's name, code, description and active state.</summary>
    /// <remarks>The identity is taken from the route only; the body carries no identifier.</remarks>
    /// <response code="200">The updated department.</response>
    /// <response code="400">Input shape or domain invariant rejected the request.</response>
    /// <response code="404">No such department, or it is soft-deleted.</response>
    /// <response code="409">The code already belongs to another department, or the row changed concurrently.</response>
    [HttpPut("{publicId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateDepartmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateDepartmentResponse>> Update(
        Guid publicId,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateDepartmentCommand(
            publicId,
            request.Name,
            request.Code,
            request.Description,
            request.IsActive);

        return Ok(await _sender.Send(command, cancellationToken));
    }

    /// <summary>Soft-deletes a department. The row is never physically removed.</summary>
    /// <response code="204">The department is now marked deleted and deactivated.</response>
    /// <response code="404">No such department, or it is already soft-deleted.</response>
    [HttpDelete("{publicId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid publicId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteDepartmentCommand(publicId), cancellationToken);
        return NoContent();
    }
}
