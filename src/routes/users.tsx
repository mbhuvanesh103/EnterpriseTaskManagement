import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { Can } from "@/auth/RoleGate";
import { ConfirmDialog } from "@/components/common/ConfirmDialog";
import { DataTableShell, PaginationBar, type ListState } from "@/components/common/DataTableShell";
import { RestrictedState } from "@/components/common/ErrorState";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/users")({
  head: () => ({
    meta: [
      { title: "Users — Enterprise Task Management" },
      {
        name: "description",
        content: "Review and manage the people in your organisation, their roles and status.",
      },
      { property: "og:title", content: "Users — Enterprise Task Management" },
      {
        property: "og:description",
        content: "People directory for the Enterprise Task Management workspace.",
      },
    ],
  }),
  component: UsersPage,
});

function UsersPage() {
  const [state, setState] = useState<ListState>("empty");
  const [search, setSearch] = useState("");

  return (
    <ProtectedRoute>
      <AppLayout
        title="Users"
        description="People records are read from the API in a later step. Nothing is stored here."
        actions={
          <Can do="manageUsers">
            <Button size="sm" disabled>
              Add user
            </Button>
          </Can>
        }
      >
        <Can
          do="viewUsers"
          fallback={
            <RestrictedState description="Only managers and admins can view the people directory." />
          }
        >
          <ListPageShell>
            <ListToolbar
              searchLabel="Search people"
              search={search}
              onSearch={setSearch}
              state={state}
              onStateChange={setState}
            />
            <DataTableShell
              caption="People in your organisation"
              columns={["Name", "Email", "Employee code", "Status", "Actions"]}
              state={state}
              emptyTitle="No people to show"
              emptyDescription="Records will appear here once the directory is connected."
              onRetry={() => setState("empty")}
            />
            <PaginationBar page={1} pageSize={20} totalCount={0} />
          </ListPageShell>
        </Can>
      </AppLayout>
    </ProtectedRoute>
  );
}

/** Shared toolbar: search field plus a state switcher for reviewing the shell. */
export function ListToolbar({
  searchLabel,
  search,
  onSearch,
  state,
  onStateChange,
  extraActions,
}: {
  searchLabel: string;
  search: string;
  onSearch: (value: string) => void;
  state: ListState;
  onStateChange: (state: ListState) => void;
  extraActions?: React.ReactNode | undefined;
}) {
  return (
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div className="w-full max-w-xs">
        <Label htmlFor="list-search">{searchLabel}</Label>
        <Input
          id="list-search"
          type="search"
          value={search}
          placeholder="Type to filter…"
          onChange={(event) => onSearch(event.target.value)}
          className="mt-1.5"
        />
      </div>
      <div className="flex flex-wrap items-center gap-2">
        {extraActions}
        <div role="group" aria-label="Preview state" className="flex flex-wrap gap-2">
          {(["ready", "loading", "empty", "error"] as const).map((option) => (
            <Button
              key={option}
              size="sm"
              variant={state === option ? "default" : "outline"}
              onClick={() => onStateChange(option)}
            >
              {option}
            </Button>
          ))}
        </div>
        <ConfirmDialog
          trigger={
            <Button size="sm" variant="outline">
              Bulk delete
            </Button>
          }
          title="Delete the selected records?"
          description="This action is connected in a later step, so nothing is deleted yet."
          confirmLabel="Delete"
          destructive
        />
      </div>
    </div>
  );
}
