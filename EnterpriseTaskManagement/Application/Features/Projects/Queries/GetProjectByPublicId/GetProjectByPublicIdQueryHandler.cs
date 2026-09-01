using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Projects;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Queries.GetProjectByPublicId;

/// <summary>Query handler: repository read → DTO projection. No DbContext.</summary>
public sealed class GetProjectByPublicIdQueryHandler
    : IRequestHandler<GetProjectByPublicIdQuery, GetProjectByPublicIdResponse>
{
    private readonly IProjectRepository _projects;

    public GetProjectByPublicIdQueryHandler(IProjectRepository projects) => _projects = projects;

    public async Task<GetProjectByPublicIdResponse> Handle(
        GetProjectByPublicIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Project project = await _projects.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Project), request.PublicId, "public id");

        return new GetProjectByPublicIdResponse(
            project.PublicId,
            project.Name,
            project.Code,
            project.Description,
            project.DepartmentId,
            project.ManagerId,
            project.Status.ToString(),
            project.StartDateUtc,
            project.TargetEndDateUtc);
    }
}
