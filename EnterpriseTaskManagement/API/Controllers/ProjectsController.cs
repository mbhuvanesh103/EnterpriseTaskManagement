using System.Security.Claims;

using EnterpriseTaskManagement.API.Contracts.Projects;
using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Features.Projects.Commands.CreateProject;
using EnterpriseTaskManagement.Application.Features.Projects.Commands.DeleteProject;
using EnterpriseTaskManagement.Application.Features.Projects.Commands.UpdateProject;
using EnterpriseTaskManagement.Application.Features.Projects.Queries.GetAllProjects;
using EnterpriseTaskManagement.Application.Features.Projects.Queries.GetProjectByPublicId;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

/// <summary>
/// HTTP surface of the Project aggregate. The controller is a thin transport
/// adapter: it depends only on <see cref="ISender"/> (MediatR) and holds no
/// business logic. Role gates are the standard framework attribute over the
/// existing JWT role claim (Admin/Manager write, all organizational roles read);
/// the per-project ownership rule (a Manager may only change projects they lead)
/// is enforced inside the Application handlers using the token's public id.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ProjectsController : ControllerBase
{
    private const string PublicIdClaimType = "publicId";

    private readonly ISender _sender;

    public ProjectsController(ISender sender) => _sender = sender;

    /// <summary>Creates a new project. Admin and Manager may create.</summary>
    /// <response code="201">Project created; the Location header points at the new resource.</response>
    /// <response code="400">Input shape or domain invariant rejected the request.</response>
    /// <response code="403">Authenticated, but the role or ownership rule denies the operation.</response>
    /// <response code="404">The referenced department or manager does not exist.</response>
    /// <response code="409">The code already belongs to another project.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(CreateProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A Manager always creates projects they lead themselves; only an Admin
        // may nominate a different manager.
        Guid managerId = IsAdmin ? request.ManagerId : RequesterPublicId;

        var command = new CreateProjectCommand(
            request.Name,
            request.Code,
            request.DepartmentId,
            managerId,
            request.Description,
            request.Status,
            request.StartDateUtc,
            request.TargetEndDateUtc);

        CreateProjectResponse response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetByPublicId),
            new { publicId = response.PublicId },
            response);
    }

    /// <summary>Lists every project that is not soft-deleted, optionally filtered.</summary>
    /// <response code="200">The project collection (possibly empty).</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetAllProjectsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetAllProjectsResponse>> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? managerId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAllProjectsQuery(departmentId, managerId), cancellationToken));

    /// <summary>Reads a single project by its externally shareable identifier.</summary>
    /// <response code="200">The requested project.</response>
    /// <response code="404">No such project, or it is soft-deleted.</response>
    [HttpGet("{publicId:guid}")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetProjectByPublicIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetProjectByPublicIdResponse>> GetByPublicId(
        Guid publicId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProjectByPublicIdQuery(publicId), cancellationToken));

    /// <summary>
    /// Updates a project. An Admin may update any project; a Manager only projects
    /// whose ManagerId equals their own public id.
    /// </summary>
    /// <response code="200">The updated project.</response>
    /// <response code="403">The ownership rule denies the operation.</response>
    /// <response code="409">Duplicate code, or the row changed concurrently.</response>
    [HttpPut("{publicId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(UpdateProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateProjectResponse>> Update(
        Guid publicId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateProjectCommand(
            publicId,
            request.Name,
            request.Code,
            request.DepartmentId,
            request.ManagerId,
            RequesterPublicId,
            IsAdmin,
            request.Description,
            request.Status,
            request.StartDateUtc,
            request.TargetEndDateUtc);

        return Ok(await _sender.Send(command, cancellationToken));
    }

    /// <summary>
    /// Soft-deletes a project. An Admin may delete any project; a Manager only
    /// projects they lead. The row is never physically removed.
    /// </summary>
    /// <response code="204">The project is now marked deleted and cancelled.</response>
    /// <response code="403">The ownership rule denies the operation.</response>
    [HttpDelete("{publicId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid publicId, CancellationToken cancellationToken)
    {
        await _sender.Send(
            new DeleteProjectCommand(publicId, RequesterPublicId, IsAdmin),
            cancellationToken);

        return NoContent();
    }

    /// <summary>True when the authenticated caller holds the Admin role claim.</summary>
    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>
    /// Public id of the authenticated caller, read from the token's <c>publicId</c>
    /// claim (falling back to <c>sub</c>). It is never taken from the request body.
    /// </summary>
    private Guid RequesterPublicId
    {
        get
        {
            string? value = User.FindFirstValue(PublicIdClaimType)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var parsed)
                ? parsed
                : throw new ForbiddenAccessException(
                    "The access token does not carry a usable user identifier.");
        }
    }
}
