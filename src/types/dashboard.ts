/**
 * Frontend mirrors of the Phase 12 Dashboard read models
 * (Application/Common/Models/Dashboard/*). Counts only — never entities,
 * internal ids, or personal data.
 */

export interface DashboardCount {
  key: string;
  count: number;
}

/** Mirrors DashboardSummary.cs — null means "not entitled", not zero. */
export interface DashboardSummary {
  activeUsers: number | null;
  activeDepartments: number | null;
  totalProjects: number | null;
  activeProjects: number | null;
  totalTasks: number;
  completedTasks: number;
  overdueTasks: number;
  dueTodayTasks: number;
}

/** Mirrors DashboardTaskMetrics.cs */
export interface DashboardTaskMetrics {
  total: number;
  completed: number;
  overdue: number;
  dueToday: number;
  unassigned: number;
  byStatus: DashboardCount[];
  byPriority: DashboardCount[];
}

/** Mirrors DashboardProjectMetrics.cs */
export interface DashboardProjectMetrics {
  total: number;
  active: number;
  byStatus: DashboardCount[];
}

/** Mirrors DashboardUserMetrics.cs */
export interface DashboardUserMetrics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  byRole: DashboardCount[];
  totalDepartments: number;
  activeDepartments: number;
}

export interface GetDashboardSummaryResponse {
  summary: DashboardSummary;
  scope: string;
  generatedAtUtc: string;
}

export interface GetTaskDashboardResponse {
  tasks: DashboardTaskMetrics;
  scope: string;
  generatedAtUtc: string;
}

export interface GetProjectDashboardResponse {
  projects: DashboardProjectMetrics;
  scope: string;
  generatedAtUtc: string;
}

export interface GetUserDashboardResponse {
  people: DashboardUserMetrics;
  scope: string;
  generatedAtUtc: string;
}
