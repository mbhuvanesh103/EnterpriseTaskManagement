using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Queries.GetProjectByPublicId;

/// <summary>Reads a single project by its externally shareable identifier.</summary>
public sealed record GetProjectByPublicIdQuery(Guid PublicId) : IRequest<GetProjectByPublicIdResponse>;
