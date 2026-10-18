import { queryOptions, useMutation, useQueryClient } from "@tanstack/react-query";

import { invalidateEntities, queryKeys, sharedQueryOptions } from "@/features/shared/query-keys";
import { apiClient } from "@/lib/api/client";
import { endpoints } from "@/lib/api/endpoints";
import type { PublicId, TaskPriority, TaskStatus, TaskSummary } from "@/types/domain";

/**
 * Tasks feature service module.
 *
 * Backend contracts mirrored exactly:
 * - POST   /api/Tasks                                   → CreateTaskRequest(Title, ProjectId, Description, AssignedUserId, Priority, DueDateUtc)
 * - GET    /api/Tasks?projectId=&assignedUserId=         → GetAllTasksResponse(Tasks, Count)
 * - GET    /api/Tasks/{publicId}
 * - PUT    /api/Tasks/{publicId}                        → UpdateTaskRequest(Title, Description, Priority, DueDateUtc) — descriptive fields ONLY
 * - PUT    /api/Tasks/{publicId}/assignee               → AssignTaskRequest(AssignedUserId) — null unassigns
 * - PUT    /api/Tasks/{publicId}/status                 → ChangeTaskStatusRequest(Status)
 * - DELETE /api/Tasks/{publicId}                        → 204, server-side soft delete
 *
 * IMPORTANT: assignment and status changes go through their dedicated endpoints
 * and are never folded into the general update call — the general update body
 * carries no assignee and no status by design.
 *
 * Visibility is decided by the API from the token: an Admin sees everything, a
 * Manager only tasks in projects they lead, an Employee only tasks assigned to
 * themselves. The UI never filters for ownership itself.
 */

export interface GetAllTasksResponse {
  tasks: TaskSummary[];
  count: number;
}

export interface CreateTaskPayload {
  title: string;
  projectId: PublicId;
  description: string | null;
  assignedUserId: PublicId | null;
  priority: TaskPriority;
  dueDateUtc: string | null;
}

export interface UpdateTaskPayload {
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueDateUtc: string | null;
}

export const tasksListQuery = (options?: {
  projectId?: PublicId;
  assignedUserId?: PublicId;
  enabled?: boolean;
}) =>
  queryOptions({
    queryKey: [
      ...queryKeys.tasks,
      "list",
      options?.projectId ?? null,
      options?.assignedUserId ?? null,
    ] as const,
    queryFn: async () => {
      const params: Record<string, string> = {};
      if (options?.projectId) params["projectId"] = options.projectId;
      if (options?.assignedUserId) params["assignedUserId"] = options.assignedUserId;
      const { data } = await apiClient.get<GetAllTasksResponse>(endpoints.tasks.list, { params });
      return data;
    },
    enabled: options?.enabled ?? true,
    ...sharedQueryOptions,
  });

export const taskDetailQuery = (publicId: PublicId) =>
  queryOptions({
    queryKey: [...queryKeys.tasks, "detail", publicId] as const,
    queryFn: async () => {
      const { data } = await apiClient.get<TaskSummary>(endpoints.tasks.byId(publicId));
      return data;
    },
    ...sharedQueryOptions,
  });

export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateTaskPayload) => {
      const { data } = await apiClient.post<TaskSummary>(endpoints.tasks.list, payload);
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["tasks", "dashboard"]),
  });
}

export function useUpdateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ publicId, ...payload }: UpdateTaskPayload & { publicId: PublicId }) => {
      const { data } = await apiClient.put<TaskSummary>(endpoints.tasks.byId(publicId), payload);
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["tasks", "dashboard"]),
  });
}

/** Dedicated assignment endpoint. `assignedUserId: null` unassigns the task. */
export function useAssignTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      publicId,
      assignedUserId,
    }: {
      publicId: PublicId;
      assignedUserId: PublicId | null;
    }) => {
      const { data } = await apiClient.put<TaskSummary>(endpoints.tasks.assignee(publicId), {
        assignedUserId,
      });
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["tasks", "dashboard"]),
  });
}

/** Dedicated status endpoint; the domain decides whether a transition is legal. */
export function useChangeTaskStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ publicId, status }: { publicId: PublicId; status: TaskStatus }) => {
      const { data } = await apiClient.put<TaskSummary>(endpoints.tasks.status(publicId), {
        status,
      });
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["tasks", "dashboard"]),
  });
}

/** Soft delete: the server marks the row deleted; the row is never removed. */
export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (publicId: PublicId) => {
      await apiClient.delete(endpoints.tasks.byId(publicId));
    },
    onSuccess: () => invalidateEntities(queryClient, ["tasks", "dashboard"]),
  });
}
