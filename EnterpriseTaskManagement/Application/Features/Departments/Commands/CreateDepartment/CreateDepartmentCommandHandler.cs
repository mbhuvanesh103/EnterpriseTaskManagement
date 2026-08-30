using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>
/// Orchestrates department creation: aggregate factory → duplicate-code check →
/// repository → unit of work. No EF Core, no SQL, no DbContext here.
/// </summary>
public sealed class CreateDepartmentCommandHandler
    : IRequestHandler<CreateDepartmentCommand, CreateDepartmentResponse>
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDepartmentCommandHandler(IDepartmentRepository departments, IUnitOfWork unitOfWork)
    {
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateDepartmentResponse> Handle(
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The aggregate validates and normalizes the code before it is compared.
        var department = Department.Create(request.Name, request.Code, request.Description, request.IsActive);

        if (await _departments.CodeExistsAsync(department.Code, null, cancellationToken))
        {
            throw ConflictException.Duplicate(nameof(Department), "code", department.Code);
        }

        await _departments.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateDepartmentResponse(
            department.PublicId,
            department.Name,
            department.Code,
            department.Description,
            department.IsActive);
    }
}
