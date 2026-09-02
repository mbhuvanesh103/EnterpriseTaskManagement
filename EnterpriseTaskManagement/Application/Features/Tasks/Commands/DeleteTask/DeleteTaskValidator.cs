using FluentValidation;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.DeleteTask;

/// <summary>Input-shape validation: identifiers must be supplied.</summary>
public sealed class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskValidator()
    {
        RuleFor(command => command.PublicId).NotEmpty().WithMessage("Task public id is required.");
        RuleFor(command => command.RequesterPublicId).NotEmpty().WithMessage("Requester public id is required.");
    }
}
