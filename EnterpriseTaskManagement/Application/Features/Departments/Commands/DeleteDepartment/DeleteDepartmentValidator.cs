using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.DeleteDepartment;

/// <summary>Input-shape validation: the public id must be supplied.</summary>
public sealed class DeleteDepartmentValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentValidator() =>
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Department public id is required.");
}
