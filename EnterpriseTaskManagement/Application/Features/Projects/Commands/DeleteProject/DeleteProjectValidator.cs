using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.DeleteProject;

/// <summary>Input-shape validation: identifiers must be supplied.</summary>
public sealed class DeleteProjectValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Project public id is required.");
        RuleFor(command => command.RequesterPublicId).NotEmpty().WithMessage("Requester public id is required.");
    }
}
