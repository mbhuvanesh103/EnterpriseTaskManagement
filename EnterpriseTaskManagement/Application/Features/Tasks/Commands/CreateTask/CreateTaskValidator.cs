using EnterpriseTaskManagement.Domain.Entities.Tasks;

using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Input-shape validation only (required fields, maximum lengths, known priority
/// name). Title normalization and workflow rules stay in the Domain.
/// </summary>
public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(TaskItem.MaxTitleLength);

        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Task project id is required.");

        RuleFor(command => command.RequesterPublicId)
            .NotEmpty().WithMessage("Requester public id is required.");

        RuleFor(command => command.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength)
            .When(command => command.Description is not null);

        RuleFor(command => command.AssignedUserId)
            .NotEmpty().WithMessage("Task assignee id must not be empty when supplied.")
            .When(command => command.AssignedUserId is not null);

        RuleFor(command => command.Priority)
            .Must(priority => Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out _))
            .WithMessage("Task priority must be one of: Low, Medium, High, Critical.")
            .When(command => command.Priority is not null);
    }
}
