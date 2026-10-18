using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Queries.GetDepartmentByPublicId;

/// <summary>Reads one department, or throws <see cref="NotFoundException"/> when absent.</summary>
public sealed class GetDepartmentByPublicIdQueryHandler
    : IRequestHandler<GetDepartmentByPublicIdQuery, GetDepartmentByPublicIdResponse>
{
    private readonly IDepartmentRepository _departments;

    public GetDepartmentByPublicIdQueryHandler(IDepartmentRepository departments) => _departments = departments;

    public async Task<GetDepartmentByPublicIdResponse> Handle(
        GetDepartmentByPublicIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Department department = await _departments.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Department), request.PublicId, "public id");

        return new GetDepartmentByPublicIdResponse(
            department.PublicId,
            department.Name,
            department.Code,
            department.Description,
            department.IsActive);
    }
}
