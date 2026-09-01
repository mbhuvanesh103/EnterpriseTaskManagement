using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Queries.GetAllProjects;

/// <summary>Query handler: repository read → DTO projection. No DbContext.</summary>
public sealed class GetAllProjectsQueryHandler
    : IRequestHandler<GetAllProjectsQuery, GetAllProjectsResponse>
{
    private readonly IProjectRepository _projects;

    public GetAllProjectsQueryHandler(IProjectRepository projects) => _projects = projects;

    public async Task<GetAllProjectsResponse> Handle(
        GetAllProjectsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var projects = request switch
        {
            { DepartmentId: { } department } =>
                await _projects.ListByDepartmentAsync(department, cancellationToken),
            { ManagerId: { } manager } =>
                await _projects.ListByManagerAsync(manager, cancellationToken),
            _ => await _projects.ListAllAsync(cancellationToken),
        };

        var summaries = projects.Select(ProjectSummary.FromProject).ToList();

        return new GetAllProjectsResponse(summaries, summaries.Count);
    }
}
