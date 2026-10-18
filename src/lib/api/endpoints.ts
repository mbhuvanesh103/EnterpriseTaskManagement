/**
 * Single source of truth for API route paths. Paths are relative to
 * env.apiBaseUrl and must mirror the ASP.NET Core controllers/endpoints.
 * No absolute URLs, hosts, or secrets are allowed in this file.
 */
export const endpoints = {
  auth: {
    login: "/Auth/login",
    refresh: "/Auth/refresh",
    logout: "/Auth/logout",
    me: "/Auth/me",
  },
  users: {
    list: "/Users",
    byId: (publicId: string) => `/Users/${publicId}`,
  },
  departments: {
    list: "/Departments",
    byId: (publicId: string) => `/Departments/${publicId}`,
  },
  projects: {
    list: "/Projects",
    byId: (publicId: string) => `/Projects/${publicId}`,
  },
  tasks: {
    list: "/Tasks",
    byId: (publicId: string) => `/Tasks/${publicId}`,
    assignee: (publicId: string) => `/Tasks/${publicId}/assignee`,
    status: (publicId: string) => `/Tasks/${publicId}/status`,
  },
  dashboard: {
    summary: "/Dashboard/summary",
    tasks: "/Dashboard/tasks",
    projects: "/Dashboard/projects",
    users: "/Dashboard/users",
  },
} as const;
