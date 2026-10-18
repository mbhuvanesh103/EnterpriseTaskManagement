import { createFileRoute, redirect } from "@tanstack/react-router";

/**
 * The workspace entry point. Phase 14 will branch on the real session; for now
 * the dashboard is the landing surface and /login is reachable from it.
 */
export const Route = createFileRoute("/")({
  beforeLoad: () => {
    throw redirect({ to: "/dashboard" });
  },
  component: () => null,
});
