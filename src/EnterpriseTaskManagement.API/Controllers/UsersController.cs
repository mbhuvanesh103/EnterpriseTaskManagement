using EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;
using EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(CreateUser),
            new { publicId = result.PublicId },
            result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllUsersQuery(),
            cancellationToken);

        return Ok(result);
    }
}