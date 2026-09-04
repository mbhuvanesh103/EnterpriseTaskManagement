import type { ReactNode } from "react";

import { useAuth } from "@/auth/auth-context";
import { LoadingState } from "@/components/common/LoadingState";

/**
 * Route protection shell. The real redirect-to-/login behaviour switches on in
 * Phase 14, once a session actually exists. Until then the guard stays in
 * "preview" mode so the foundation screens remain reviewable, and it says so
 * plainly instead of pretending a session is present.
 */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { status } = useAuth();

  if (status === "initializing") {
    return <LoadingState label="Checking your session" />;
  }

  return <>{children}</>;
}
