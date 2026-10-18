using EnterpriseTaskManagement.Domain.Entities.Tasks;

using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.UpdateTask;

/// <summary>Input-shape validation only; the Domain owns the invariants.</summary>
public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Task public id is required.");
        RuleFor(command => command.RequesterPublicId).NotEmpty().WithMessage("Requester public id is required.");

        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(TaskItem.MaxTitleLength);

        RuleFor(command => command.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength)
            .When(command => command.Description is not null);

        RuleFor(command => command.Priority)
            .Must(priority => Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out _))
            .WithMessage("Task priority must be one of: Low, Medium, High, Critical.")
            .When(command => command.Priority is not null);
    }
}
