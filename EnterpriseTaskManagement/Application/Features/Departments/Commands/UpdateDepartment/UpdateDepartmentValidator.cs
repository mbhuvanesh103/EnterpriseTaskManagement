using EnterpriseTaskManagement.Domain.Entities.Departments;

using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.UpdateDepartment;

/// <summary>
/// Input-shape validation only. Code format and normalization stay in the Domain.
/// </summary>
public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Department public id is required.");

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
