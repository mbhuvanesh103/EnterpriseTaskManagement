using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Departments.Queries.GetDepartmentByPublicId;

/// <summary>Reads a single department by its externally shareable identifier.</summary>
public sealed record GetDepartmentByPublicIdQuery(Guid PublicId) : IRequest<GetDepartmentByPublicIdResponse>;
