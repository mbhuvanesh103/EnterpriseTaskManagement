import { queryOptions, useMutation, useQueryClient } from "@tanstack/react-query";

import { invalidateEntities, queryKeys, sharedQueryOptions } from "@/features/shared/query-keys";
import { apiClient } from "@/lib/api/client";
import { endpoints } from "@/lib/api/endpoints";
import type { ProjectStatus, ProjectSummary, PublicId } from "@/types/domain";

/**
 * Projects feature service module.
 *
 * Backend contracts mirrored exactly:
 * - POST   /api/Projects                              → CreateProjectRequest(Name, Code, DepartmentId, ManagerId, Description, Status, StartDateUtc, TargetEndDateUtc)
 * - GET    /api/Projects?departmentId=&managerId=     → GetAllProjectsResponse(Projects, Count)
 * - GET    /api/Projects/{publicId}
 * - PUT    /api/Projects/{publicId}                   → UpdateProjectRequest(same shape, no id in body)
 * - DELETE /api/Projects/{publicId}                   → 204, server-side soft delete
 *
 * Status is one of the real backend names: Planned, Active, OnHold, Completed,
 * Cancelled. Ownership is decided by the API from the token: a Manager creating
 * a project always becomes its manager regardless of what the body says, and a
 * Manager may only change projects they lead. The UI never asserts ownership.
 */

export interface GetAllProjectsResponse {
  projects: ProjectSummary[];
  count: number;
}

export interface ProjectPayload {
  name: string;
  code: string;
  departmentId: PublicId;
  managerId: PublicId;
  description: string | null;
  status: ProjectStatus;
  startDateUtc: string | null;
  targetEndDateUtc: string | null;
}

export const projectsListQuery = (options?: {
  departmentId?: PublicId;
  managerId?: PublicId;
  enabled?: boolean;
}) =>
  queryOptions({
    queryKey: [
      ...queryKeys.projects,
      "list",
      options?.departmentId ?? null,
      options?.managerId ?? null,
    ] as const,
    queryFn: async () => {
      const params: Record<string, string> = {};
      if (options?.departmentId) params["departmentId"] = options.departmentId;
      if (options?.managerId) params["managerId"] = options.managerId;
      const { data } = await apiClient.get<GetAllProjectsResponse>(endpoints.projects.list, {
        params,
      });
      return data;
    },
    enabled: options?.enabled ?? true,
    ...sharedQueryOptions,
  });

export const projectDetailQuery = (publicId: PublicId) =>
  queryOptions({
    queryKey: [...queryKeys.projects, "detail", publicId] as const,
    queryFn: async () => {
      const { data } = await apiClient.get<ProjectSummary>(endpoints.projects.byId(publicId));
      return data;
    },
    ...sharedQueryOptions,
  });

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: ProjectPayload) => {
      const { data } = await apiClient.post<ProjectSummary>(endpoints.projects.list, payload);
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["projects", "dashboard"]),
  });
}

export function useUpdateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ publicId, ...payload }: ProjectPayload & { publicId: PublicId }) => {
      const { data } = await apiClient.put<ProjectSummary>(
        endpoints.projects.byId(publicId),
        payload,
      );
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["projects", "dashboard"]),
  });
}

/** Soft delete: the server marks the row deleted and cancels the project. */
export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (publicId: PublicId) => {
      await apiClient.delete(endpoints.projects.byId(publicId));
    },
    onSuccess: () => invalidateEntities(queryClient, ["projects", "tasks", "dashboard"]),
  });
}
