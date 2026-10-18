using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Auth.Commands.Login;

/// <summary>Credentials submitted to <c>POST /api/Auth/login</c>.</summary>
/// <param name="Email">The account email; normalized by the Domain Email value object.</param>
/// <param name="Password">The plaintext candidate password. Never persisted or echoed back.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
