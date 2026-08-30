using EnterpriseTaskManagement.Domain.Entities.Departments;

using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>
/// Input-shape validation only (required fields, maximum lengths). The code
/// format and normalization rules stay inside the Domain aggregate.
/// </summary>
public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(Department.MaxNameLength);

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Department code is required.")
            .MaximumLength(Department.MaxCodeLength);

        RuleFor(command => command.Description)
            .MaximumLength(Department.MaxDescriptionLength)
            .When(command => command.Description is not null);
    }
}
