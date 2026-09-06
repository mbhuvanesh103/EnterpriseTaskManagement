import { useNavigate, useRouterState } from "@tanstack/react-router";
import { useEffect, type ReactNode } from "react";

import { useAuth } from "@/auth/auth-context";
import { LoadingState } from "@/components/common/LoadingState";

/**
 * Real route protection (Phase 14): an unauthenticated visitor is sent to
 * /login, carrying the blocked URL so sign-in can return them there. The
 * redirect runs once per unauthenticated render and /login is never protected,
 * so no loop is possible.
 */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const navigate = useNavigate();
  const href = useRouterState({ select: (state) => state.location.href });

  useEffect(() => {
    if (status === "unauthenticated") {
      void navigate({ to: "/login", search: { redirect: href }, replace: true });
    }
  }, [href, navigate, status]);

  if (status !== "authenticated") {
    return <LoadingState label="Checking your session" />;
  }

  return <>{children}</>;
}
