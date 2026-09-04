/**
 * ⚠️ DEMO DATA — VISUALIZATION ONLY. NOT REAL DATA. NOT SEED DATA.
 *
 * This module exists solely so the Phase 13 dashboard layout can be reviewed
 * before Phase 14 connects GET /api/Dashboard/*. It is:
 * - the ONLY place in the frontend where fabricated numbers exist,
 * - never written to the backend or any database,
 * - shaped exactly like the real Phase 12 responses, so Phase 14 swaps the
 *   `DEMO_*` constants for query results and deletes this file.
 *
 * Every screen that renders these values shows a visible "demo data" badge.
 */

import type {
  GetDashboardSummaryResponse,
  GetProjectDashboardResponse,
  GetTaskDashboardResponse,
  GetUserDashboardResponse,
} from "@/types/dashboard";

export const DEMO_DATA_NOTICE =
  "Demo figures for layout preview only — real values arrive when the API is connected.";

export const DEMO_SUMMARY: GetDashboardSummaryResponse = {
  summary: {
    activeUsers: 48,
    activeDepartments: 6,
    totalProjects: 14,
    activeProjects: 9,
    totalTasks: 212,
    completedTasks: 128,
    overdueTasks: 11,
    dueTodayTasks: 7,
  },
  scope: "Demo",
  generatedAtUtc: "2026-01-01T00:00:00+00:00",
};

export const DEMO_TASKS: GetTaskDashboardResponse = {
  tasks: {
    total: 212,
    completed: 128,
    overdue: 11,
    dueToday: 7,
    unassigned: 15,
    byStatus: [
      { key: "Todo", count: 44 },
      { key: "InProgress", count: 27 },
      { key: "Review", count: 8 },
      { key: "Completed", count: 128 },
      { key: "Blocked", count: 5 },
    ],
    byPriority: [
      { key: "Low", count: 61 },
      { key: "Medium", count: 88 },
      { key: "High", count: 47 },
      { key: "Critical", count: 16 },
    ],
  },
  scope: "Demo",
  generatedAtUtc: "2026-01-01T00:00:00+00:00",
};

export const DEMO_PROJECTS: GetProjectDashboardResponse = {
  projects: {
    total: 14,
    active: 9,
    byStatus: [
      { key: "Planned", count: 3 },
      { key: "Active", count: 9 },
      { key: "OnHold", count: 1 },
      { key: "Completed", count: 1 },
      { key: "Cancelled", count: 0 },
    ],
  },
  scope: "Demo",
  generatedAtUtc: "2026-01-01T00:00:00+00:00",
};

export const DEMO_PEOPLE: GetUserDashboardResponse = {
  people: {
    totalUsers: 52,
    activeUsers: 48,
    inactiveUsers: 4,
    byRole: [
      { key: "Employee", count: 43 },
      { key: "Manager", count: 7 },
      { key: "Admin", count: 2 },
    ],
    totalDepartments: 7,
    activeDepartments: 6,
  },
  scope: "Demo",
  generatedAtUtc: "2026-01-01T00:00:00+00:00",
};
