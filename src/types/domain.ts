/**
 * Strict frontend mirrors of the backend Domain vocabulary.
 *
 * Rules:
 * - Only PublicId (Guid string) identifiers ever appear here. Internal database
 *   ids never leave the backend and must never be modelled in the frontend.
 * - Enum values are string literals that match the backend enum NAMES exactly
 *   (the API serializes them via `.ToString()`), so no arbitrary strings can be
 *   introduced by the UI.
 */

export type PublicId = string;

/** Backend: Domain/Entities/Users/UserRole.cs */
export const USER_ROLES = ["Employee", "Manager", "Admin"] as const;
export type UserRole = (typeof USER_ROLES)[number];

/** Backend: Domain/Entities/Tasks/TaskItemStatus.cs */
export const TASK_STATUSES = ["Todo", "InProgress", "Review", "Completed", "Blocked"] as const;
export type TaskStatus = (typeof TASK_STATUSES)[number];

/** Backend: Domain/Entities/Tasks/TaskPriority.cs */
export const TASK_PRIORITIES = ["Low", "Medium", "High", "Critical"] as const;
export type TaskPriority = (typeof TASK_PRIORITIES)[number];

/** Backend: Domain/Entities/Projects/ProjectStatus.cs */
export const PROJECT_STATUSES = ["Planned", "Active", "OnHold", "Completed", "Cancelled"] as const;
export type ProjectStatus = (typeof PROJECT_STATUSES)[number];

/** Mirrors Application/Common/Models/UserSummary.cs */
export interface UserSummary {
  publicId: PublicId;
  fullName: string;
  email: string;
  employeeCode: string;
  isActive: boolean;
}

/** Mirrors Application/Common/Models/DepartmentSummary.cs */
export interface DepartmentSummary {
  publicId: PublicId;
  name: string;
  code: string;
  description: string | null;
  isActive: boolean;
}

/** Mirrors Application/Common/Models/ProjectSummary.cs */
export interface ProjectSummary {
  publicId: PublicId;
  name: string;
  code: string;
  description: string | null;
  departmentId: PublicId;
  managerId: PublicId;
  status: ProjectStatus;
  startDateUtc: string;
  targetEndDateUtc: string | null;
}

/** Mirrors Application/Common/Models/TaskSummary.cs */
export interface TaskSummary {
  publicId: PublicId;
  title: string;
  description: string | null;
  projectId: PublicId;
  assignedUserId: PublicId | null;
  priority: TaskPriority;
  status: TaskStatus;
  dueDateUtc: string | null;
  completedAtUtc: string | null;
}

/** Human-readable labels for enum names that are not display-friendly. */
export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: "To do",
  InProgress: "In progress",
  Review: "Review",
  Completed: "Completed",
  Blocked: "Blocked",
};

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  Planned: "Planned",
  Active: "Active",
  OnHold: "On hold",
  Completed: "Completed",
  Cancelled: "Cancelled",
};
