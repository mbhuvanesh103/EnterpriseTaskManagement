using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Queries.GetAllDepartments;

/// <summary>Query handler: repository read → DTO projection. No DbContext.</summary>
public sealed class GetAllDepartmentsQueryHandler
    : IRequestHandler<GetAllDepartmentsQuery, GetAllDepartmentsResponse>
{
    private readonly IDepartmentRepository _departments;

    public GetAllDepartmentsQueryHandler(IDepartmentRepository departments) => _departments = departments;

    public async Task<GetAllDepartmentsResponse> Handle(
        GetAllDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _departments.ListAllAsync(cancellationToken);
        var summaries = departments.Select(DepartmentSummary.FromDepartment).ToList();

        return new GetAllDepartmentsResponse(summaries, summaries.Count);
    }
}
