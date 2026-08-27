using EnterpriseTaskManagement.API.Contracts.Users;
using EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;
using EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;
using EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;
using EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;
using EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

/// <summary>
/// HTTP surface of the User aggregate. The controller is a thin transport
/// adapter: it depends only on <see cref="ISender"/> (MediatR), holds no
/// business logic, no validation logic and no persistence types.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    /// <summary>Registers a new user.</summary>
    /// <response code="201">User created; the Location header points at the new resource.</response>
    /// <response code="400">Input shape or domain invariant rejected the request.</response>
    /// <response code="409">Email or employee code already belongs to another user.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResponse>> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        CreateUserResponse response = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetByPublicId),
            new { publicId = response.PublicId },
            response);
    }

    /// <summary>Lists every user that is not soft-deleted, optionally filtered by department.</summary>
    /// <response code="200">The user collection (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllUsersResponse>> GetAll(
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAllUsersQuery(departmentId), cancellationToken));

    /// <summary>Reads a single user by its externally shareable identifier.</summary>
    /// <response code="200">The requested user.</response>
    /// <response code="404">No such user, or the user is soft-deleted.</response>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(GetUserByPublicIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetUserByPublicIdResponse>> GetByPublicId(
        Guid publicId,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetUserByPublicIdQuery(publicId), cancellationToken));

    /// <summary>Updates a user's name, email and department.</summary>
    /// <remarks>The identity is taken from the route only; the body carries no identifier.</remarks>
    /// <response code="200">The updated user.</response>
    /// <response code="400">Input shape or domain invariant rejected the request.</response>
    /// <response code="404">No such user, or the user is soft-deleted.</response>
    /// <response code="409">The new email already belongs to another user.</response>
    [HttpPut("{publicId:guid}")]
    [ProducesResponseType(typeof(UpdateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateUserResponse>> Update(
        Guid publicId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateUserCommand(
            publicId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.DepartmentId);

        return Ok(await _sender.Send(command, cancellationToken));
    }

    /// <summary>Soft-deletes a user. The row is never physically removed.</summary>
    /// <response code="204">The user is now marked deleted and deactivated.</response>
    /// <response code="400">The supplied identifier was rejected.</response>
    /// <response code="404">No such user, or the user is already soft-deleted.</response>
    [HttpDelete("{publicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid publicId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteUserCommand(publicId), cancellationToken);
        return NoContent();
    }
}
