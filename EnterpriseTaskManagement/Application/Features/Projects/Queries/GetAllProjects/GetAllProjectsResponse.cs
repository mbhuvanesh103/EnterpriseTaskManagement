using EnterpriseTaskManagement.Application.Common.Models;

namespace EnterpriseTaskManagement.Application.Features.Projects.Queries.GetAllProjects;

/// <summary>Read model returned by <c>GetAllProjectsQuery</c>.</summary>
public sealed record GetAllProjectsResponse(IReadOnlyList<ProjectSummary> Projects, int Count);
