import { queryOptions, useMutation, useQueryClient } from "@tanstack/react-query";

import { invalidateEntities, queryKeys, sharedQueryOptions } from "@/features/shared/query-keys";
import { apiClient } from "@/lib/api/client";
import { endpoints } from "@/lib/api/endpoints";
import type { PublicId, UserSummary } from "@/types/domain";

/**
 * Users feature service module. Every HTTP call for the User aggregate lives
 * here and goes through the shared apiClient — no component ever calls axios or
 * fetch directly. Identifiers are always the backend's PublicId (Guid string);
 * internal database ids are never modelled.
 *
 * Backend contracts mirrored exactly:
 * - POST   /api/Users                → CreateUserCommand(FirstName, LastName, Email, EmployeeCode, DepartmentId)
 * - GET    /api/Users?departmentId=  → GetAllUsersResponse(Users, Count)
 * - GET    /api/Users/{publicId}     → UserSummary-shaped response
 * - PUT    /api/Users/{publicId}     → UpdateUserRequest(FirstName, LastName, Email, DepartmentId)
 * - DELETE /api/Users/{publicId}     → 204, server-side soft delete
 *
 * PASSWORD SEMANTICS: the real create/update contracts carry NO password field
 * and NO role field — the API exposes no way to set either. The UI therefore
 * never collects, generates, transmits or displays a password, and never lets a
 * client choose a role. Nothing is invented to fill those gaps.
 */

export interface GetAllUsersResponse {
  users: UserSummary[];
  count: number;
}

export interface CreateUserPayload {
  firstName: string;
  lastName: string;
  email: string;
  employeeCode: string;
  departmentId: PublicId;
}

export interface UpdateUserPayload {
  firstName: string;
  lastName: string;
  email: string;
  departmentId: PublicId;
}

export const usersListQuery = (options?: { departmentId?: PublicId; enabled?: boolean }) =>
  queryOptions({
    queryKey: [...queryKeys.users, "list", options?.departmentId ?? null] as const,
    queryFn: async () => {
      const { data } = await apiClient.get<GetAllUsersResponse>(endpoints.users.list, {
        params: options?.departmentId ? { departmentId: options.departmentId } : undefined,
      });
      return data;
    },
    enabled: options?.enabled ?? true,
    ...sharedQueryOptions,
  });

export const userDetailQuery = (publicId: PublicId) =>
  queryOptions({
    queryKey: [...queryKeys.users, "detail", publicId] as const,
    queryFn: async () => {
      const { data } = await apiClient.get<UserSummary>(endpoints.users.byId(publicId));
      return data;
    },
    ...sharedQueryOptions,
  });

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateUserPayload) => {
      const { data } = await apiClient.post<UserSummary>(endpoints.users.list, payload);
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["users", "dashboard"]),
  });
}

export function useUpdateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ publicId, ...payload }: UpdateUserPayload & { publicId: PublicId }) => {
      const { data } = await apiClient.put<UserSummary>(endpoints.users.byId(publicId), payload);
      return data;
    },
    onSuccess: () => invalidateEntities(queryClient, ["users", "dashboard"]),
  });
}

/** Soft delete: the server marks the row deleted; the row is never removed. */
export function useDeleteUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (publicId: PublicId) => {
      await apiClient.delete(endpoints.users.byId(publicId));
    },
    onSuccess: () => invalidateEntities(queryClient, ["users", "dashboard"]),
  });
}
