using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Auth.Commands.Login;

/// <summary>
/// Input-shape validation only. Email syntax and normalization remain the
/// Domain <c>Email</c> value object's job; this simply refuses obviously
/// unusable input before any repository or hashing work happens.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Shortest password accepted at the transport boundary.</summary>
    public const int MinimumPasswordLength = 8;

    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinimumPasswordLength)
            .WithMessage($"Password must be at least {MinimumPasswordLength} characters.");
    }
}
