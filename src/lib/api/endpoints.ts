/**
 * Single source of truth for API route paths. Paths are relative to
 * env.apiBaseUrl and must mirror the ASP.NET Core controllers/endpoints.
 */
export const endpoints = {
  auth: {
    login: "/auth/login",
    refresh: "/auth/refresh",
    logout: "/auth/logout",
    me: "/auth/me",
  },
  tasks: {
    list: "/tasks",
    byId: (id: string) => `/tasks/${id}`,
  },
  projects: {
    list: "/projects",
    byId: (id: string) => `/projects/${id}`,
  },
  users: {
    list: "/users",
    byId: (id: string) => `/users/${id}`,
  },
} as const;
