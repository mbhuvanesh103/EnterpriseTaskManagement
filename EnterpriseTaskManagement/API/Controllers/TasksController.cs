using System.Security.Claims;

using EnterpriseTaskManagement.API.Contracts.Tasks;
using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.AssignTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.CreateTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.DeleteTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.UpdateTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetAllTasks;
using EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetTaskByPublicId;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

/// <summary>
/// HTTP surface of the Task aggregate. The controller is a thin transport
/// adapter: it depends only on <see cref="ISender"/> (MediatR) and holds no
/// business logic. Role gates are the standard framework attribute over the
/// existing JWT role claim; the ownership rules (a Manager only inside projects
/// they lead, an Employee only their own assigned tasks) live in the Application
/// handlers and are always derived from the token, never from the request body.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private const string PublicIdClaimType = "publicId";

    private readonly ISender _sender;

    public TasksController(ISender sender) => _sender = sender;

    /// <summary>Creates a task. Admin anywhere; Manager only in projects they manage.</summary>
    /// <response code="201">Task created; the Location header points at the new resource.</response>
    /// <response code="403">The role or ownership rule denies the operation.</response>
    /// <response code="404">The referenced project or assignee does not exist.</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(TaskSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskSummary>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TaskSummary response = await _sender.Send(
            new CreateTaskCommand(
                request.Title,
                request.ProjectId,
                RequesterPublicId,
                RequesterRole,
                request.Description,
                request.AssignedUserId,
                request.Priority,
                request.DueDateUtc),
            cancellationToken);

        return CreatedAtAction(nameof(GetByPublicId), new { publicId = response.PublicId }, response);
    }

    /// <summary>
    /// Lists tasks visible to the caller: an Admin sees all, a Manager only tasks
    /// in projects they manage, an Employee only tasks assigned to themselves.
    /// </summary>
    /// <response code="200">The task collection (possibly empty).</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetAllTasksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetAllTasksResponse>> GetAll(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assignedUserId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetAllTasksQuery(RequesterPublicId, RequesterRole, projectId, assignedUserId),
            cancellationToken));

    /// <summary>Reads a single task by its externally shareable identifier.</summary>
    /// <response code="200">The requested task.</response>
    /// <response code="403">The caller may not read this task.</response>
    /// <response code="404">No such task, or it is soft-deleted.</response>
    [HttpGet("{publicId:guid}")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(TaskSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskSummary>> GetByPublicId(
        Guid publicId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetTaskByPublicIdQuery(publicId, RequesterPublicId, RequesterRole),
            cancellationToken));

    /// <summary>Updates a task's title, description, priority and due date.</summary>
    /// <response code="200">The updated task.</response>
    /// <response code="409">The row changed concurrently.</response>
    [HttpPut("{publicId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(TaskSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskSummary>> Update(
        Guid publicId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(
            new UpdateTaskCommand(
                publicId,
                request.Title,
                RequesterPublicId,
                RequesterRole,
                request.Description,
                request.Priority,
                request.DueDateUtc),
            cancellationToken));
    }

    /// <summary>Assigns or unassigns a task. Admin anywhere; Manager in their own projects.</summary>
    /// <response code="200">The reassigned task.</response>
    [HttpPut("{publicId:guid}/assignee")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(TaskSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskSummary>> Assign(
        Guid publicId,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(
            new AssignTaskCommand(publicId, request.AssignedUserId, RequesterPublicId, RequesterRole),
            cancellationToken));
    }

    /// <summary>
    /// Moves a task to another workflow state. An Employee may only do this for a
    /// task assigned to themselves.
    /// </summary>
    /// <response code="200">The task in its new state.</response>
    /// <response code="400">The transition is not allowed by the domain.</response>
    [HttpPut("{publicId:guid}/status")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(TaskSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskSummary>> ChangeStatus(
        Guid publicId,
        [FromBody] ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await _sender.Send(
            new ChangeTaskStatusCommand(publicId, request.Status, RequesterPublicId, RequesterRole),
            cancellationToken));
    }

    /// <summary>Soft-deletes a task. The row is never physically removed.</summary>
    /// <response code="204">The task is now marked deleted.</response>
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
            new DeleteTaskCommand(publicId, RequesterPublicId, RequesterRole),
            cancellationToken);

        return NoContent();
    }

    /// <summary>The caller's role, taken from the validated JWT role claim only.</summary>
    private TaskRequesterRole RequesterRole =>
        User.IsInRole("Admin") ? TaskRequesterRole.Admin
        : User.IsInRole("Manager") ? TaskRequesterRole.Manager
        : TaskRequesterRole.Employee;

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
