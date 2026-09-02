using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetTaskByPublicId;

/// <summary>Reads a single task by its externally shareable identifier.</summary>
public sealed record GetTaskByPublicIdQuery(
    Guid PublicId,
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<TaskSummary>;
