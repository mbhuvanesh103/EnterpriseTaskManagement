import type { QueryClient } from "@tanstack/react-query";

import { ApiError } from "@/lib/api/client";

/**
 * Central TanStack Query key registry. Every feature module reads and
 * invalidates through these keys, so no page has to guess a key shape and
 * cache invalidation stays predictable. There is no full page reload anywhere:
 * a successful mutation invalidates the affected lists plus the live dashboard.
 */
export const queryKeys = {
  users: ["users"] as const,
  departments: ["departments"] as const,
  projects: ["projects"] as const,
  tasks: ["tasks"] as const,
  dashboard: ["dashboard"] as const,
};

export type EntityKey = keyof typeof queryKeys;

/** Invalidate the listed entity caches (dashboard included where relevant). */
export async function invalidateEntities(
  queryClient: QueryClient,
  entities: readonly EntityKey[],
): Promise<void> {
  await Promise.all(
    entities.map((entity) => queryClient.invalidateQueries({ queryKey: queryKeys[entity] })),
  );
}

/**
 * Shared retry policy: an auth or permission failure is never retried, and a
 * missing record is never retried either.
 */
export function retryPolicy(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError) {
    if (error.isUnauthorized || error.isForbidden || error.status === 404) return false;
  }
  return failureCount < 1;
}

export const sharedQueryOptions = { staleTime: 30_000, retry: retryPolicy } as const;
