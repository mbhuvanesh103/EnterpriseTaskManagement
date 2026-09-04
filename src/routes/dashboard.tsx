import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { Can } from "@/auth/RoleGate";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState, RestrictedState } from "@/components/common/ErrorState";
import { LoadingCards } from "@/components/common/LoadingState";
import { AppLayout } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import {
  DEMO_DATA_NOTICE,
  DEMO_PEOPLE,
  DEMO_PROJECTS,
  DEMO_SUMMARY,
  DEMO_TASKS,
} from "@/features/dashboard/demo-data";
import { PROJECT_STATUS_LABELS, TASK_STATUS_LABELS } from "@/types/domain";
import type { DashboardCount } from "@/types/dashboard";

export const Route = createFileRoute("/dashboard")({
  head: () => ({
    meta: [
      { title: "Dashboard — Enterprise Task Management" },
      {
        name: "description",
        content:
          "Headline counts for tasks, projects, people and departments across your organisation.",
      },
      { property: "og:title", content: "Dashboard — Enterprise Task Management" },
      {
        property: "og:description",
        content: "Task, project and people counters for your organisation.",
      },
    ],
  }),
  component: DashboardPage,
});

/** The four states the real dashboard queries will produce in Phase 14. */
type ViewState = "normal" | "loading" | "empty" | "error";

function DashboardPage() {
  const [state, setState] = useState<ViewState>("normal");

  return (
    <ProtectedRoute>
      <AppLayout
        title="Dashboard"
        description="Counts only — the API never returns personal data to this screen."
        actions={
          <div className="flex flex-wrap gap-2" role="group" aria-label="Preview state">
            {(["normal", "loading", "empty", "error"] as const).map((option) => (
              <Button
                key={option}
                size="sm"
                variant={state === option ? "default" : "outline"}
                onClick={() => setState(option)}
              >
                {option}
              </Button>
            ))}
          </div>
        }
      >
        <p className="w-fit rounded-md bg-amber-500/10 px-3 py-1.5 text-xs font-medium text-amber-700 dark:text-amber-400">
          {DEMO_DATA_NOTICE}
        </p>

        {state === "loading" ? <LoadingCards /> : null}
        {state === "error" ? <ErrorState onRetry={() => setState("normal")} /> : null}
        {state === "empty" ? (
          <EmptyState
            title="No activity yet"
            description="Once projects and tasks exist, their counters appear here."
          />
        ) : null}

        {state === "normal" ? <DashboardContent /> : null}
      </AppLayout>
    </ProtectedRoute>
  );
}

function DashboardContent() {
  const { summary } = DEMO_SUMMARY;
  const completion =
    summary.totalTasks === 0 ? 0 : Math.round((summary.completedTasks / summary.totalTasks) * 100);

  return (
    <div className="flex flex-col gap-6">
      <section aria-labelledby="headline-heading" className="flex flex-col gap-3">
        <h2 id="headline-heading" className="text-sm font-medium text-muted-foreground">
          Headline counts
        </h2>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Tasks" value={summary.totalTasks} hint="In your scope" />
          <StatCard label="Completed" value={summary.completedTasks} hint={`${completion}% done`} />
          <StatCard label="Overdue" value={summary.overdueTasks} hint="Past due, not completed" />
          <StatCard label="Due today" value={summary.dueTodayTasks} hint="UTC calendar day" />
          <StatCard label="Projects" value={summary.totalProjects} hint="Total in scope" />
          <StatCard label="Active projects" value={summary.activeProjects} hint="Status: Active" />
          <StatCard label="Active people" value={summary.activeUsers} hint="Admin only" />
          <StatCard
            label="Active departments"
            value={summary.activeDepartments}
            hint="Admin only"
          />
        </div>
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Task completion</CardTitle>
            <CardDescription>Completed tasks as a share of all tasks in scope.</CardDescription>
          </CardHeader>
          <CardContent>
            <Progress value={completion} aria-label={`${completion}% of tasks completed`} />
            <p className="mt-2 text-sm text-muted-foreground">{completion}% completed</p>
          </CardContent>
        </Card>
      </section>

      <div className="grid gap-6 lg:grid-cols-2">
        <BreakdownCard
          title="Tasks by status"
          description="Todo, In progress, Review, Completed, Blocked."
          counts={DEMO_TASKS.tasks.byStatus}
          labels={TASK_STATUS_LABELS}
        />
        <BreakdownCard
          title="Tasks by priority"
          description="Low, Medium, High, Critical."
          counts={DEMO_TASKS.tasks.byPriority}
        />
        <Can
          do="viewProjectDashboard"
          fallback={
            <RestrictedState description="Project counters are available to managers and admins." />
          }
        >
          <BreakdownCard
            title="Projects by status"
            description="Planned, Active, On hold, Completed, Cancelled."
            counts={DEMO_PROJECTS.projects.byStatus}
            labels={PROJECT_STATUS_LABELS}
          />
        </Can>
        <Can
          do="viewPeopleDashboard"
          fallback={
            <RestrictedState description="Organisation-wide people counters are available to admins only." />
          }
        >
          <BreakdownCard
            title="People by role"
            description="Employee, Manager, Admin."
            counts={DEMO_PEOPLE.people.byRole}
          />
        </Can>
      </div>
    </div>
  );
}

function StatCard({ label, value, hint }: { label: string; value: number | null; hint: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardDescription>{label}</CardDescription>
        <CardTitle className="text-3xl tabular-nums">
          {value === null ? <span className="text-base font-normal">Not available</span> : value}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-xs text-muted-foreground">{hint}</p>
      </CardContent>
    </Card>
  );
}

function BreakdownCard({
  title,
  description,
  counts,
  labels,
}: {
  title: string;
  description: string;
  counts: DashboardCount[];
  labels?: Record<string, string> | undefined;
}) {
  const max = Math.max(1, ...counts.map((entry) => entry.count));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent>
        {counts.length === 0 ? (
          <EmptyState title="No breakdown yet" />
        ) : (
          <ul className="flex flex-col gap-3">
            {counts.map((entry) => (
              <li key={entry.key} className="flex items-center gap-3">
                <span className="w-28 shrink-0 text-sm text-foreground">
                  {labels?.[entry.key] ?? entry.key}
                </span>
                <span className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
                  <span
                    className="block h-full rounded-full bg-primary"
                    style={{ width: `${(entry.count / max) * 100}%` }}
                  />
                </span>
                <Badge variant="secondary" className="tabular-nums">
                  {entry.count}
                </Badge>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
