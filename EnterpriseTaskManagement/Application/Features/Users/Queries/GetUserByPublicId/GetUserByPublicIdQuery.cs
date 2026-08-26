using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

/// <summary>Reads a single user by its externally shareable identifier.</summary>
public sealed record GetUserByPublicIdQuery(Guid PublicId) : IRequest<GetUserByPublicIdResponse>;
