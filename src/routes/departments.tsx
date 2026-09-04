import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { Can } from "@/auth/RoleGate";
import { DataTableShell, PaginationBar, type ListState } from "@/components/common/DataTableShell";
import { RestrictedState } from "@/components/common/ErrorState";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Button } from "@/components/ui/button";
import { ListToolbar } from "@/routes/users";

export const Route = createFileRoute("/departments")({
  head: () => ({
    meta: [
      { title: "Departments — Enterprise Task Management" },
      {
        name: "description",
        content: "Browse the departments that own projects and people across the organisation.",
      },
      { property: "og:title", content: "Departments — Enterprise Task Management" },
      {
        property: "og:description",
        content: "Department structure for the Enterprise Task Management workspace.",
      },
    ],
  }),
  component: DepartmentsPage,
});

function DepartmentsPage() {
  const [state, setState] = useState<ListState>("empty");
  const [search, setSearch] = useState("");

  return (
    <ProtectedRoute>
      <AppLayout
        title="Departments"
        description="Department records are read from the API in a later step."
        actions={
          <Can do="manageDepartments">
            <Button size="sm" disabled>
              Add department
            </Button>
          </Can>
        }
      >
        <Can
          do="viewDepartments"
          fallback={<RestrictedState description="Your role doesn't include department access." />}
        >
          <ListPageShell>
            <ListToolbar
              searchLabel="Search departments"
              search={search}
              onSearch={setSearch}
              state={state}
              onStateChange={setState}
            />
            <DataTableShell
              caption="Departments in your organisation"
              columns={["Name", "Code", "Description", "Status", "Actions"]}
              state={state}
              emptyTitle="No departments to show"
              emptyDescription="Departments will appear here once the API is connected."
              onRetry={() => setState("empty")}
            />
            <PaginationBar page={1} pageSize={20} totalCount={0} />
          </ListPageShell>
        </Can>
      </AppLayout>
    </ProtectedRoute>
  );
}
