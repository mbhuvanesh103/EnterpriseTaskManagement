using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Input-shape validation only. Email syntax, name length and employee-code
/// format stay in the Domain value objects.
/// </summary>
public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("User public id is required.");
        RuleFor(command => command.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(command => command.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(command => command.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(command => command.DepartmentId).NotEmpty().WithMessage("Department is required.");
    }
}
