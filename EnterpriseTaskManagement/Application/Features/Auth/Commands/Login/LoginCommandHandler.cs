using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Interfaces.Authentication;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Auth.Commands.Login;

/// <summary>
/// Login use case: normalize the email through the Domain value object, load the
/// aggregate through <see cref="IUserRepository"/>, verify the stored hash with
/// <see cref="IPasswordHasher"/>, then issue a token via
/// <see cref="IJwtTokenService"/>. No DbContext, no SQL, no hashing algorithm
/// and no token library are visible here. Every rejection path throws the same
/// <see cref="AuthenticationFailedException"/>.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (Exception exception) when (exception is ArgumentException or Domain.Exceptions.DomainException)
        {
            // A malformed email must not be distinguishable from a wrong password.
            throw new AuthenticationFailedException();
        }

        User? user = await _users.GetByEmailAsync(email, cancellationToken);

        // Unknown account, provisioned-without-password account, wrong password,
        // deactivated account and soft-deleted account are all the same failure.
        if (user is null
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !_passwordHasher.Verify(request.Password, user.PasswordHash)
            || !user.IsActive
            || user.IsDeleted)
        {
            throw new AuthenticationFailedException();
        }

        AccessToken token = _tokenService.CreateAccessToken(user);

        return new LoginResponse(
            token.Value,
            token.ExpiresAtUtc,
            user.PublicId,
            user.Name.Value,
            user.Email.Value,
            user.Role.ToString());
    }
}
