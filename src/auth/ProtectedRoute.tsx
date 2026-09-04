import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";

import { useAuth } from "@/auth/auth-context";
import { LoadingState } from "@/components/common/LoadingState";
import { Button } from "@/components/ui/button";

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

  if (status === "unauthenticated") {
    return (
      <>
        <div
          role="status"
          className="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-dashed border-border bg-muted/40 px-4 py-3 text-sm text-muted-foreground"
        >
          <span>
            You are not signed in. Sign-in is connected in a later step, so this page is shown as a
            preview of the interface.
          </span>
          <Button asChild size="sm" variant="outline">
            <Link to="/login">Go to sign in</Link>
          </Button>
        </div>
        {children}
      </>
    );
  }

  return <>{children}</>;
}
