using EnterpriseTaskManagement.Application.Common.Models;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetAllTasks;

/// <summary>Read model returned by <c>GetAllTasksQuery</c>.</summary>
public sealed record GetAllTasksResponse(IReadOnlyList<TaskSummary> Tasks, int Count);
