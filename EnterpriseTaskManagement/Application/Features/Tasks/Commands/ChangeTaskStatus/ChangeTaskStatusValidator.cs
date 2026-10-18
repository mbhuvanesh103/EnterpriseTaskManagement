using EnterpriseTaskManagement.Domain.Entities.Tasks;

using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// Input-shape validation only: a known status name must be supplied. Which
/// transitions are legal is a Domain invariant.
/// </summary>
public sealed class ChangeTaskStatusValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Task public id is required.");
        RuleFor(command => command.RequesterPublicId).NotEmpty().WithMessage("Requester public id is required.");

        RuleFor(command => command.Status)
            .NotEmpty().WithMessage("Task status is required.")
            .Must(status => Enum.TryParse<TaskItemStatus>(status, ignoreCase: true, out _))
            .WithMessage("Task status must be one of: Todo, InProgress, Review, Completed, Blocked.");
    }
}
