import { queryOptions, useMutation, useQueryClient } from "@tanstack/react-query";

import { invalidateEntities, queryKeys, sharedQueryOptions } from "@/features/shared/query-keys";
import { apiClient } from "@/lib/api/client";
import { endpoints } from "@/lib/api/endpoints";
import type { DepartmentSummary, PublicId } from "@/types/domain";

/**
 * Departments feature service module.
 *
 * Backend contracts mirrored exactly:
 * - POST   /api/Departments               → CreateDepartmentRequest(Name, Code, Description, IsActive)
 * - GET    /api/Departments               → GetAllDepartmentsResponse(Departments, Count)
 * - GET    /api/Departments/{publicId}
 * - PUT    /api/Departments/{publicId}    → UpdateDepartmentRequest(Name, Code, Description, IsActive)
 * - DELETE /api/Departments/{publicId}    → 204, server-side soft delete
 *
 * Reads are open to every organizational role, writes are Admin-only; the API
 * enforces that and the UI only mirrors it.
 */

export interface GetAllDepartmentsResponse {
  departments: DepartmentSummary[];
  count: number;
}

export interface DepartmentPayload {
  name: string;
  code: string;
  description: string | null;
  isActive: boolean;
}

export const departmentsListQuery = (options?: { enabled?: boolean }) =>
  queryOptions({
    queryKey: [...queryKeys.departments, "list"] as const,
    queryFn: async () => {
      const { data } = await apiClient.get<GetAllDepartmentsResponse>(endpoints.departments.list);
      return data;
    },
    enabled: options?.enabled ?? true,
    ...sharedQueryOptions,
  });

export const departmentDetailQuery = (publicId: PublicId) =>
  queryOptions({
    queryKey: [...queryKeys.departments, "detail", publicId] as const,
    queryFn: async () => {
      const { data } = await apiClient.get<DepartmentSummary>(endpoints.departments.byId(publicId));
      return data;
    },
    ...sharedQueryOptions,
  });

export function useCreateDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: DepartmentPayload) => {
      const { data } = await apiClient.post<DepartmentSummary>(endpoints.departments.list, payload);
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["departments", "dashboard"]),
  });
}

export function useUpdateDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ publicId, ...payload }: DepartmentPayload & { publicId: PublicId }) => {
      const { data } = await apiClient.put<DepartmentSummary>(
        endpoints.departments.byId(publicId),
        payload,
      );
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["departments", "dashboard"]),
  });
}

/** Soft delete: the server marks the row deleted and deactivates it. */
export function useDeleteDepartment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (publicId: PublicId) => {
      await apiClient.delete(endpoints.departments.byId(publicId));
    },
    onSuccess: () =>
      invalidateEntities(queryClient, ["departments", "projects", "users", "dashboard"]),
  });
}
