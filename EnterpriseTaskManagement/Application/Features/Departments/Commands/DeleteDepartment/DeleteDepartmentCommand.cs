using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.DeleteDepartment;

/// <summary>Use-case input for soft-deleting a department, addressed by PublicId.</summary>
public sealed record DeleteDepartmentCommand(Guid PublicId) : IRequest<DeleteDepartmentResponse>;
