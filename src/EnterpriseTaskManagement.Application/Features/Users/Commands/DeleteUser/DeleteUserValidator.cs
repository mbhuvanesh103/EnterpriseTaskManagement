using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserValidator
    : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty();

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .MaximumLength(100);
    }
}