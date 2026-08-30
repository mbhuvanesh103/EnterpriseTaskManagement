using EnterpriseTaskManagement.Application.Common.Models;

namespace EnterpriseTaskManagement.Application.Features.Departments.Queries.GetAllDepartments;

/// <summary>Read model returned by <c>GetAllDepartmentsQuery</c>.</summary>
public sealed record GetAllDepartmentsResponse(IReadOnlyList<DepartmentSummary> Departments, int Count);
