import type { ReactNode } from "react";

import { useAuth } from "@/auth/auth-context";
import type { UserRole } from "@/types/domain";

/**
 * Reusable role-aware rendering helpers. UX only — the backend role policies
 * remain the security boundary. Pages must use these instead of scattering
 * hard-coded role comparisons.
 */

interface RoleGateProps {
  allow: readonly UserRole[];
  children: ReactNode;
  /** Rendered when the current role is not allowed. Defaults to nothing. */
  fallback?: ReactNode;
}

export function RoleGate({ allow, children, fallback = null }: RoleGateProps) {
  const { hasAnyRole } = useAuth();
  return <>{hasAnyRole(allow) ? children : fallback}</>;
}

/** Central capability map, mirroring the backend role policies. */
export const capabilities = {
  viewUsers: ["Admin", "Manager"],
  manageUsers: ["Admin"],
  viewDepartments: ["Admin", "Manager", "Employee"],
  manageDepartments: ["Admin"],
  viewProjects: ["Admin", "Manager", "Employee"],
  manageProjects: ["Admin", "Manager"],
  viewTasks: ["Admin", "Manager", "Employee"],
  manageTasks: ["Admin", "Manager"],
  viewPeopleDashboard: ["Admin"],
  viewProjectDashboard: ["Admin", "Manager"],

  // Navigation visibility (UX only):
  // Admin: dashboard, users, departments, projects, tasks
  // Manager: dashboard, projects, tasks
  // Employee: dashboard, tasks
  navDashboard: ["Admin", "Manager", "Employee"],
  navUsers: ["Admin"],
  navDepartments: ["Admin"],
  navProjects: ["Admin", "Manager"],
  navTasks: ["Admin", "Manager", "Employee"],
} as const satisfies Record<string, readonly UserRole[]>;

export type Capability = keyof typeof capabilities;

export function useCapability(capability: Capability): boolean {
  const { hasAnyRole } = useAuth();
  return hasAnyRole(capabilities[capability]);
}

/** Role list for a capability, for callers that filter collections. */
export function rolesFor(capability: Capability): readonly UserRole[] {
  return capabilities[capability];
}

export function Can({
  do: capability,
  children,
  fallback = null,
}: {
  do: Capability;
  children: ReactNode;
  fallback?: ReactNode;
}) {
  return (
    <RoleGate allow={capabilities[capability]} fallback={fallback}>
      {children}
    </RoleGate>
  );
}
