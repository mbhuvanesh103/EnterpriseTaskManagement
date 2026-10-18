using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>Use-case input for creating a department. Primitives only.</summary>
public sealed record CreateDepartmentCommand(
    string Name,
    string Code,
    string? Description = null,
    bool IsActive = true) : IRequest<CreateDepartmentResponse>;
