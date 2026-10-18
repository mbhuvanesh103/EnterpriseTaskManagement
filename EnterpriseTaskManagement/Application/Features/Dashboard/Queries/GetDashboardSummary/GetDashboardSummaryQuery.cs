using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Reads the headline dashboard counters for the authenticated caller. Both
/// parameters are supplied by the API from the validated JWT only.
/// </summary>
public sealed record GetDashboardSummaryQuery(
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<GetDashboardSummaryResponse>;
