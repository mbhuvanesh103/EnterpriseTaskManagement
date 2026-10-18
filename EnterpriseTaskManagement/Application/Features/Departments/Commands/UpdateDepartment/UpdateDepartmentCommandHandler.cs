using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.UpdateDepartment;

/// <summary>
/// Loads the aggregate by PublicId, applies the changes through its behaviour
/// (<c>Update</c>) and commits. Concurrent writes are rejected by the rowversion
/// token configured in Infrastructure and surface as HTTP 409.
/// </summary>
public sealed class UpdateDepartmentCommandHandler
    : IRequestHandler<UpdateDepartmentCommand, UpdateDepartmentResponse>
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDepartmentCommandHandler(IDepartmentRepository departments, IUnitOfWork unitOfWork)
    {
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateDepartmentResponse> Handle(
        UpdateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Department department = await _departments.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Department), request.PublicId, "public id");

        department.Update(request.Name, request.Code, request.Description, request.IsActive);

        if (await _departments.CodeExistsAsync(department.Code, department.PublicId, cancellationToken))
        {
            throw ConflictException.Duplicate(nameof(Department), "code", department.Code);
        }

        _departments.Update(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateDepartmentResponse(
            department.PublicId,
            department.Name,
            department.Code,
            department.Description,
            department.IsActive);
    }
}
