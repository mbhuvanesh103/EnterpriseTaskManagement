using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Use-case input for creating a project. Primitives only; <c>Status</c> is the
/// Domain enum name (Planned, Active, OnHold, Completed, Cancelled).
/// </summary>
public sealed record CreateProjectCommand(
    string Name,
    string Code,
    Guid DepartmentId,
    Guid ManagerId,
    string? Description = null,
    string? Status = null,
    DateTimeOffset? StartDateUtc = null,
    DateTimeOffset? TargetEndDateUtc = null) : IRequest<CreateProjectResponse>;
