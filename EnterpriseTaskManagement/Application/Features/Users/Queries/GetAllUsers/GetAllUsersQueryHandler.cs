using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;

/// <summary>Query handler: repository read → DTO projection. No DbContext.</summary>
public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, GetAllUsersResponse>
{
    private readonly IUserRepository _users;

    public GetAllUsersQueryHandler(IUserRepository users) => _users = users;

    public async Task<GetAllUsersResponse> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = request.DepartmentId is { } departmentId && departmentId != Guid.Empty
            ? await _users.ListByDepartmentAsync(departmentId, cancellationToken)
            : await _users.ListAllAsync(cancellationToken);

        var summaries = users.Select(UserSummary.FromUser).ToList();

        return new GetAllUsersResponse(summaries, summaries.Count);
    }
}
