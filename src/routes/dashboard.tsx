import { useQuery } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";

import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { useCapability } from "@/auth/RoleGate";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState, RestrictedState } from "@/components/common/ErrorState";
import { LoadingCards } from "@/components/common/LoadingState";
import { AppLayout } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Skeleton } from "@/components/ui/skeleton";
import {
  dashboardPeopleQuery,
  dashboardProjectsQuery,
  dashboardSummaryQuery,
  dashboardTasksQuery,
} from "@/features/dashboard/queries";
import { ApiError } from "@/lib/api/client";
import { PROJECT_STATUS_LABELS, TASK_STATUS_LABELS } from "@/types/domain";
import type { DashboardCount, DashboardSummary } from "@/types/dashboard";

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

function DashboardPage() {
  return (
    <ProtectedRoute>
      <AppLayout
        title="Dashboard"
        description="Counts only — the API never returns personal data to this screen."
      >
        <DashboardContent />
      </AppLayout>
    </ProtectedRoute>
  );
}

function isForbidden(error: unknown): boolean {
  return error instanceof ApiError && error.isForbidden;
}

function DashboardContent() {
  const canSeeProjects = useCapability("viewProjectDashboard");
  const canSeePeople = useCapability("viewPeopleDashboard");

  const summary = useQuery(dashboardSummaryQuery());
  const tasks = useQuery(dashboardTasksQuery());
  const projects = useQuery(dashboardProjectsQuery(canSeeProjects));
  const people = useQuery(dashboardPeopleQuery(canSeePeople));

  return (
    <div className="flex flex-col gap-6">
      <section aria-labelledby="headline-heading" className="flex flex-col gap-3">
        <h2 id="headline-heading" className="text-sm font-medium text-muted-foreground">
          Headline counts
        </h2>

        {summary.isPending ? <LoadingCards /> : null}

        {summary.isError ? (
          isForbidden(summary.error) ? (
            <RestrictedState description="Your role doesn't include the dashboard summary." />
          ) : (
            <ErrorState
              description={
                summary.error instanceof ApiError
                  ? summary.error.message
                  : "We couldn't load the summary."
              }
              onRetry={() => void summary.refetch()}
            />
          )
        ) : null}

        {summary.data ? <SummarySection summary={summary.data.summary} /> : null}
      </section>

      <div className="grid gap-6 lg:grid-cols-2">
        <BreakdownPanel
          title="Tasks by status"
          description="Todo, In progress, Review, Completed, Blocked."
          counts={tasks.data?.tasks.byStatus}
          labels={TASK_STATUS_LABELS}
          isPending={tasks.isPending}
          error={tasks.error}
          onRetry={() => void tasks.refetch()}
        />
        <BreakdownPanel
          title="Tasks by priority"
          description="Low, Medium, High, Critical."
          counts={tasks.data?.tasks.byPriority}
          isPending={tasks.isPending}
          error={tasks.error}
          onRetry={() => void tasks.refetch()}
        />

        {canSeeProjects ? (
          <BreakdownPanel
            title="Projects by status"
            description="Planned, Active, On hold, Completed, Cancelled."
            counts={projects.data?.projects.byStatus}
            labels={PROJECT_STATUS_LABELS}
            isPending={projects.isPending}
            error={projects.error}
            onRetry={() => void projects.refetch()}
          />
        ) : (
          <RestrictedState description="Project counters are available to managers and admins." />
        )}

        {canSeePeople ? (
          <BreakdownPanel
            title="People by role"
            description="Employee, Manager, Admin."
            counts={people.data?.people.byRole}
            isPending={people.isPending}
            error={people.error}
            onRetry={() => void people.refetch()}
          />
        ) : (
          <RestrictedState description="People counters are available to admins only." />
        )}
      </div>
    </div>
  );
}

function SummarySection({ summary }: { summary: DashboardSummary }) {
  const completion =
    summary.totalTasks === 0 ? 0 : Math.round((summary.completedTasks / summary.totalTasks) * 100);

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Tasks" value={summary.totalTasks} hint="In your scope" />
        <StatCard label="Completed" value={summary.completedTasks} hint={`${completion}% done`} />
        <StatCard label="Overdue" value={summary.overdueTasks} hint="Past due, not completed" />
        <StatCard label="Due today" value={summary.dueTodayTasks} hint="UTC calendar day" />
        <StatCard label="Projects" value={summary.totalProjects} hint="Total in scope" />
        <StatCard label="Active projects" value={summary.activeProjects} hint="Status: Active" />
        <StatCard label="Active people" value={summary.activeUsers} hint="Admin only" />
        <StatCard label="Active departments" value={summary.activeDepartments} hint="Admin only" />
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
    </>
  );
}

/** `null` from the API means "not available for your role", never zero. */
function StatCard({ label, value, hint }: { label: string; value: number | null; hint?: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardDescription>{label}</CardDescription>
        <CardTitle className="text-3xl tabular-nums">
          {value === null ? (
            <span className="text-base text-muted-foreground">Not available</span>
          ) : (
            value
          )}
        </CardTitle>
      </CardHeader>
      {hint ? (
        <CardContent>
          <p className="text-xs text-muted-foreground">{hint}</p>
        </CardContent>
      ) : null}
    </Card>
  );
}

function BreakdownPanel({
  title,
  description,
  counts,
  labels,
  isPending,
  error,
  onRetry,
}: {
  title: string;
  description: string;
  counts: DashboardCount[] | undefined;
  labels?: Record<string, string> | undefined;
  isPending: boolean;
  error: unknown;
  onRetry: () => void;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent>
        {isPending ? (
          <div role="status" aria-live="polite" aria-label="Loading" className="space-y-3">
            {Array.from({ length: 4 }, (_, index) => (
              <Skeleton key={index} className="h-6 w-full rounded-md" />
            ))}
          </div>
        ) : error ? (
          isForbidden(error) ? (
            <RestrictedState description="Your role doesn't include these figures." />
          ) : (
            <ErrorState
              description={error instanceof ApiError ? error.message : "We couldn't load this."}
              onRetry={onRetry}
            />
          )
        ) : !counts || counts.length === 0 ? (
          <EmptyState title="No activity yet" description="Counters appear as work is recorded." />
        ) : (
          <CountList counts={counts} labels={labels} />
        )}
      </CardContent>
    </Card>
  );
}

function CountList({
  counts,
  labels,
}: {
  counts: DashboardCount[];
  labels?: Record<string, string> | undefined;
}) {
  const max = Math.max(1, ...counts.map((entry) => entry.count));

  return (
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
  );
}
