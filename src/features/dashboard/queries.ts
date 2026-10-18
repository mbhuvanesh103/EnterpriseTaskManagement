import { queryOptions } from "@tanstack/react-query";

import { apiClient, ApiError } from "@/lib/api/client";
import { endpoints } from "@/lib/api/endpoints";
import type {
  GetDashboardSummaryResponse,
  GetProjectDashboardResponse,
  GetTaskDashboardResponse,
  GetUserDashboardResponse,
} from "@/types/dashboard";

/**
 * Live dashboard reads against the Phase 12 endpoints. There is no fallback
 * data: a failed request surfaces an error state, and a role-restricted section
 * surfaces a restricted state. Nothing is fabricated.
 */

async function get<T>(path: string): Promise<T> {
  const { data } = await apiClient.get<T>(path);
  return data;
}

/** Never retry an auth/permission failure — retrying cannot change the answer. */
function retryPolicy(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && (error.isUnauthorized || error.isForbidden)) return false;
  return failureCount < 1;
}

const shared = { staleTime: 30_000, retry: retryPolicy } as const;

export const dashboardSummaryQuery = () =>
  queryOptions({
    queryKey: ["dashboard", "summary"] as const,
    queryFn: () => get<GetDashboardSummaryResponse>(endpoints.dashboard.summary),
    ...shared,
  });

export const dashboardTasksQuery = () =>
  queryOptions({
    queryKey: ["dashboard", "tasks"] as const,
    queryFn: () => get<GetTaskDashboardResponse>(endpoints.dashboard.tasks),
    ...shared,
  });

export const dashboardProjectsQuery = (enabled: boolean) =>
  queryOptions({
    queryKey: ["dashboard", "projects"] as const,
    queryFn: () => get<GetProjectDashboardResponse>(endpoints.dashboard.projects),
    enabled,
    ...shared,
  });

export const dashboardPeopleQuery = (enabled: boolean) =>
  queryOptions({
    queryKey: ["dashboard", "users"] as const,
    queryFn: () => get<GetUserDashboardResponse>(endpoints.dashboard.users),
    enabled,
    ...shared,
  });
