using System.Security.Claims;

using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetDashboardSummary;
using EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetProjectDashboard;
using EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetTaskDashboard;
using EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetUserDashboard;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

/// <summary>
/// Read-only reporting surface. The controller is a thin transport adapter: it
/// depends only on <see cref="ISender"/> (MediatR), holds no business logic, and
/// performs no data access. Role gates are the standard framework attribute over
/// the existing JWT role claim; the finer scoping rules (a Manager sees only the
/// projects they lead, an Employee only their own assigned tasks) live in the
/// Application handlers and are always derived from the validated token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private const string PublicIdClaimType = "publicId";

    private readonly ISender _sender;

    public DashboardController(ISender sender) => _sender = sender;

    /// <summary>
    /// Headline counters for the caller. Sections outside the caller's
    /// entitlement come back as <c>null</c> (an Employee receives task figures
    /// only, a Manager task and project figures for the projects they lead).
    /// </summary>
    /// <response code="200">The dashboard summary for the caller's scope.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    /// <response code="403">The role does not permit dashboard access.</response>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetDashboardSummaryResponse>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetDashboardSummaryQuery(RequesterPublicId, RequesterRole),
            cancellationToken));

    /// <summary>Task metrics, scoped to the caller (all / managed projects / own assignments).</summary>
    /// <response code="200">Task counts, plus the status and priority breakdowns.</response>
    [HttpGet("tasks")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    [ProducesResponseType(typeof(GetTaskDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetTaskDashboardResponse>> GetTasks(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetTaskDashboardQuery(RequesterPublicId, RequesterRole),
            cancellationToken));

    /// <summary>Project metrics. Admin sees every project; a Manager only the ones they lead.</summary>
    /// <response code="200">Project counts and the status breakdown.</response>
    [HttpGet("projects")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(GetProjectDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetProjectDashboardResponse>> GetProjects(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetProjectDashboardQuery(RequesterPublicId, RequesterRole),
            cancellationToken));

    /// <summary>
    /// Organization-wide people and department counters. Admin only; the response
    /// carries counts and role names only — no personal data.
    /// </summary>
    /// <response code="200">User and department counts.</response>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetUserDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetUserDashboardResponse>> GetUsers(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GetUserDashboardQuery(RequesterPublicId, RequesterRole),
            cancellationToken));

    /// <summary>The caller's role, taken from the validated JWT role claim only.</summary>
    private TaskRequesterRole RequesterRole =>
        User.IsInRole("Admin") ? TaskRequesterRole.Admin
        : User.IsInRole("Manager") ? TaskRequesterRole.Manager
        : TaskRequesterRole.Employee;

    /// <summary>
    /// Public id of the authenticated caller, read from the token's <c>publicId</c>
    /// claim (falling back to <c>sub</c>). It is never taken from client input.
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
