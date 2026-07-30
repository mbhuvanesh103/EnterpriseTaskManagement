using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

public sealed record GetUserByPublicIdQuery(
    string PublicId
) : IRequest<GetUserByPublicIdResponse>;