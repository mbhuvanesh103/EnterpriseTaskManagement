using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Queries.GetAllDepartments;

/// <summary>Reads every department that is not soft-deleted.</summary>
public sealed record GetAllDepartmentsQuery : IRequest<GetAllDepartmentsResponse>;
