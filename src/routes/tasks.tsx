import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { Can } from "@/auth/RoleGate";
import { DataTableShell, PaginationBar, type ListState } from "@/components/common/DataTableShell";
import { RestrictedState } from "@/components/common/ErrorState";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ListToolbar } from "@/routes/users";
import { TASK_PRIORITIES, TASK_STATUSES, TASK_STATUS_LABELS } from "@/types/domain";

export const Route = createFileRoute("/tasks")({
  head: () => ({
    meta: [
      { title: "Tasks — Enterprise Task Management" },
      {
        name: "description",
        content: "Follow tasks by status, priority, assignee and due date across your projects.",
      },
      { property: "og:title", content: "Tasks — Enterprise Task Management" },
      {
        property: "og:description",
        content: "Task workload for the Enterprise Task Management workspace.",
      },
    ],
  }),
  component: TasksPage,
});

function TasksPage() {
  const [state, setState] = useState<ListState>("empty");
  const [search, setSearch] = useState("");

  return (
    <ProtectedRoute>
      <AppLayout
        title="Tasks"
        description="Task records are read from the API in a later step."
        actions={
          <Can do="manageTasks">
            <Button size="sm" disabled>
              New task
            </Button>
          </Can>
        }
      >
        <Can
          do="viewTasks"
          fallback={<RestrictedState description="Your role doesn't include task access." />}
        >
          <ListPageShell>
            <ListToolbar
              searchLabel="Search tasks"
              search={search}
              onSearch={setSearch}
              state={state}
              onStateChange={setState}
            />
            <div className="flex flex-wrap gap-4">
              <div className="flex flex-wrap gap-2" aria-label="Task statuses">
                {TASK_STATUSES.map((status) => (
                  <Badge key={status} variant="outline">
                    {TASK_STATUS_LABELS[status]}
                  </Badge>
                ))}
              </div>
              <div className="flex flex-wrap gap-2" aria-label="Task priorities">
                {TASK_PRIORITIES.map((priority) => (
                  <Badge key={priority} variant="secondary">
                    {priority}
                  </Badge>
                ))}
              </div>
            </div>
            <DataTableShell
              caption="Tasks in your scope"
              columns={["Title", "Priority", "Status", "Assignee", "Due", "Actions"]}
              state={state}
              emptyTitle="No tasks to show"
              emptyDescription="Tasks will appear here once the API is connected."
              onRetry={() => setState("empty")}
            />
            <PaginationBar page={1} pageSize={20} totalCount={0} />
          </ListPageShell>
        </Can>
      </AppLayout>
    </ProtectedRoute>
  );
}
