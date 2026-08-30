using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.DeleteDepartment;

/// <summary>
/// Soft-deletes a department: the aggregate's own <c>Delete()</c> sets the audit
/// deletion state and deactivates it. No row is ever physically removed, so the
/// required User → Department relationship can never dangle.
/// </summary>
public sealed class DeleteDepartmentCommandHandler
    : IRequestHandler<DeleteDepartmentCommand, DeleteDepartmentResponse>
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDepartmentCommandHandler(IDepartmentRepository departments, IUnitOfWork unitOfWork)
    {
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteDepartmentResponse> Handle(
        DeleteDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Department department = await _departments.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Department), request.PublicId, "public id");

        department.Delete();

        _departments.SoftDelete(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteDepartmentResponse(department.PublicId, department.IsDeleted, department.DeletedAtUtc);
    }
}
