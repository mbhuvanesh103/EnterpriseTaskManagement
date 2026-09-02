using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.AssignTask;

/// <summary>Input-shape validation: identifiers must be supplied.</summary>
public sealed class AssignTaskValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Task public id is required.");
        RuleFor(command => command.RequesterPublicId).NotEmpty().WithMessage("Requester public id is required.");

        RuleFor(command => command.AssignedUserId)
            .NotEmpty().WithMessage("Task assignee id must not be empty when supplied.")
            .When(command => command.AssignedUserId is not null);
    }
}
