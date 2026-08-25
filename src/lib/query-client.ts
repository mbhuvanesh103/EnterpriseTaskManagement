import { QueryClient } from "@tanstack/react-query";

import { ApiError } from "@/lib/api/client";

/**
 * Shared TanStack Query configuration. Created per request/router instance so
 * SSR never leaks cache between users.
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: (failureCount, error) => {
          const status = error instanceof ApiError ? error.status : undefined;
          if (status && status >= 400 && status < 500) return false;
          return failureCount < 2;
        },
        refetchOnWindowFocus: false,
      },
      mutations: { retry: 0 },
    },
  });
}

/** Central query-key registry to keep invalidation predictable. */
export const queryKeys = {
  auth: { me: ["auth", "me"] as const },
  tasks: {
    all: ["tasks"] as const,
    list: (params: unknown) => ["tasks", "list", params] as const,
    detail: (id: string) => ["tasks", "detail", id] as const,
  },
  projects: {
    all: ["projects"] as const,
    detail: (id: string) => ["projects", "detail", id] as const,
  },
} as const;
