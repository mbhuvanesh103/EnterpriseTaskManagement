using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.DeleteProject;

/// <summary>
/// Use-case input for soft-deleting a project, addressed by PublicId. The
/// requester identity comes from the authenticated token so a Manager may only
/// delete projects they lead.
/// </summary>
public sealed record DeleteProjectCommand(
    Guid PublicId,
    Guid RequesterPublicId,
    bool RequesterIsAdmin) : IRequest<DeleteProjectResponse>;
