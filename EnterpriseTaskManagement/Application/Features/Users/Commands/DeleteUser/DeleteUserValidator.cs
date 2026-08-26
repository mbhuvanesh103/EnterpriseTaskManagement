using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>Input-shape validation: the public id must be supplied.</summary>
public sealed class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator() =>
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("User public id is required.");
}
