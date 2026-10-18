using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Queries.GetAllProjects;

/// <summary>
/// Reads every project that is not soft-deleted, optionally narrowed to one
/// department or one manager (both by public id).
/// </summary>
public sealed record GetAllProjectsQuery(
    Guid? DepartmentId = null,
    Guid? ManagerId = null) : IRequest<GetAllProjectsResponse>;
