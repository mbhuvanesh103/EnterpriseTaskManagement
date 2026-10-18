using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Input-shape validation only (presence of required fields, a department was
/// supplied). Format rules — email syntax, name lengths, the EMP###### pattern —
/// belong to the Domain value objects and are NOT duplicated here.
/// </summary>
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(command => command.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(command => command.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(command => command.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(command => command.EmployeeCode).NotEmpty().WithMessage("Employee code is required.");

        // DepartmentId is a Guid in the Domain (User references Department.PublicId),
        // so "greater than zero" is expressed as "must be a real, non-empty id".
        RuleFor(command => command.DepartmentId).NotEmpty().WithMessage("Department is required.");
    }
}
