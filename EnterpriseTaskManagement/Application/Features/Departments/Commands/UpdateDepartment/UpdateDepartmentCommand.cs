using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.UpdateDepartment;

/// <summary>Use-case input for updating a department, addressed by PublicId.</summary>
public sealed record UpdateDepartmentCommand(
    Guid PublicId,
    string Name,
    string Code,
    string? Description = null,
    bool IsActive = true) : IRequest<UpdateDepartmentResponse>;
