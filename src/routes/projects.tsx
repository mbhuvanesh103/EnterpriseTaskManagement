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
import { PROJECT_STATUSES, PROJECT_STATUS_LABELS } from "@/types/domain";

export const Route = createFileRoute("/projects")({
  head: () => ({
    meta: [
      { title: "Projects — Enterprise Task Management" },
      {
        name: "description",
        content: "Track projects by status, owning department and responsible manager.",
      },
      { property: "og:title", content: "Projects — Enterprise Task Management" },
      {
        property: "og:description",
        content: "Project portfolio for the Enterprise Task Management workspace.",
      },
    ],
  }),
  component: ProjectsPage,
});

function ProjectsPage() {
  const [state, setState] = useState<ListState>("empty");
  const [search, setSearch] = useState("");

  return (
    <ProtectedRoute>
      <AppLayout
        title="Projects"
        description="Project records are read from the API in a later step."
        actions={
          <Can do="manageProjects">
            <Button size="sm" disabled>
              New project
            </Button>
          </Can>
        }
      >
        <Can
          do="viewProjects"
          fallback={<RestrictedState description="Your role doesn't include project access." />}
        >
          <ListPageShell>
            <ListToolbar
              searchLabel="Search projects"
              search={search}
              onSearch={setSearch}
              state={state}
              onStateChange={setState}
            />
            <div className="flex flex-wrap gap-2" aria-label="Project statuses">
              {PROJECT_STATUSES.map((status) => (
                <Badge key={status} variant="outline">
                  {PROJECT_STATUS_LABELS[status]}
                </Badge>
              ))}
            </div>
            <DataTableShell
              caption="Projects in your scope"
              columns={["Name", "Code", "Status", "Start", "Target end", "Actions"]}
              state={state}
              emptyTitle="No projects to show"
              emptyDescription="Projects will appear here once the API is connected."
              onRetry={() => setState("empty")}
            />
            <PaginationBar page={1} pageSize={20} totalCount={0} />
          </ListPageShell>
        </Can>
      </AppLayout>
    </ProtectedRoute>
  );
}
