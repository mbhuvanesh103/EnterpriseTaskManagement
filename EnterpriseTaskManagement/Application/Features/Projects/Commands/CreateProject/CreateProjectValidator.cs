using EnterpriseTaskManagement.Domain.Entities.Projects;

using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Input-shape validation only (required fields, maximum lengths, known status
/// name). Code format, normalization and schedule rules stay in the Domain.
/// </summary>
public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(Project.MaxNameLength);

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Project code is required.")
            .MaximumLength(Project.MaxCodeLength);

        RuleFor(command => command.DepartmentId)
            .NotEmpty().WithMessage("Project department id is required.");

        RuleFor(command => command.ManagerId)
            .NotEmpty().WithMessage("Project manager id is required.");

        RuleFor(command => command.Description)
            .MaximumLength(Project.MaxDescriptionLength)
            .When(command => command.Description is not null);

        RuleFor(command => command.Status)
            .Must(status => Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out _))
            .WithMessage("Project status must be one of: Planned, Active, OnHold, Completed, Cancelled.")
            .When(command => command.Status is not null);
    }
}
