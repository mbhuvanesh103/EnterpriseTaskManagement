using EnterpriseTaskManagement.Application.Features.Auth.Commands.Login;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTaskManagement.API.Controllers;

/// <summary>
/// Authentication surface. Thin transport adapter: it depends only on
/// <see cref="ISender"/> and contains no credential, hashing or token logic.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>Exchanges email and password for a signed JWT access token.</summary>
    /// <response code="200">Authentication succeeded; the access token is returned.</response>
    /// <response code="400">The submitted input shape was rejected.</response>
    /// <response code="401">Authentication failed. The reason is deliberately generic.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(command, cancellationToken));
}
