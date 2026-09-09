import { useQuery } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/auth/auth-context";
import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { Can, useCapability } from "@/auth/RoleGate";
import { ConfirmDialog } from "@/components/common/ConfirmDialog";
import { DataTableShell, type ListState } from "@/components/common/DataTableShell";
import { RestrictedState } from "@/components/common/ErrorState";
import { FormDialog } from "@/components/common/FormDialog";
import { ListToolbar, matchesSearch } from "@/components/common/ListToolbar";
import { RelationSelect, SelectField } from "@/components/common/SelectField";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { TableCell, TableRow } from "@/components/ui/table";
import { departmentsListQuery } from "@/features/departments/departments";
import {
  projectsListQuery,
  useCreateProject,
  useDeleteProject,
  useUpdateProject,
} from "@/features/projects/projects";
import { usersListQuery } from "@/features/users/users";
import { describeApiError } from "@/lib/api/errors";
import { TextField } from "@/routes/users";
import {
  PROJECT_STATUSES,
  PROJECT_STATUS_LABELS,
  type ProjectStatus,
  type ProjectSummary,
} from "@/types/domain";

export const Route = createFileRoute("/projects")({
  head: () => ({
    meta: [
      { title: "Projects — Enterprise Task Management" },
      {
        name: "description",
        content: "Track and manage projects by status, owning department and responsible manager.",
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

const STATUS_OPTIONS = PROJECT_STATUSES.map((status) => ({
  value: status,
  label: PROJECT_STATUS_LABELS[status],
}));

function toDateInput(value: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "" : date.toISOString().slice(0, 10);
}

function fromDateInput(value: string): string | null {
  return value ? new Date(`${value}T00:00:00Z`).toISOString() : null;
}

function ProjectsPage() {
  const canView = useCapability("viewProjects");
  const canManage = useCapability("manageProjects");

  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<ProjectSummary | null>(null);

  const list = useQuery(projectsListQuery({ enabled: canView }));
  const departments = useQuery(departmentsListQuery({ enabled: canView }));

  const departmentNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const department of departments.data?.departments ?? []) {
      map.set(department.publicId, department.name);
    }
    return map;
  }, [departments.data]);

  const rows = useMemo(
    () =>
      (list.data?.projects ?? []).filter(
        (project) =>
          matchesSearch(search, project.name, project.code, project.description) &&
          (!statusFilter || project.status === statusFilter),
      ),
    [list.data, search, statusFilter],
  );

  const state: ListState = list.isPending
    ? "loading"
    : list.isError
      ? "error"
      : rows.length === 0
        ? "empty"
        : "ready";

  const described = list.isError ? describeApiError(list.error) : null;

  return (
    <ProtectedRoute>
      <AppLayout
        title="Projects"
        description="Projects are read live from the API. The server decides which ones you may change."
        actions={
          <Can do="manageProjects">
            <Button size="sm" onClick={() => setCreating(true)}>
              Add project
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
              filters={
                <div className="w-full max-w-[12rem]">
                  <SelectField
                    id="project-status-filter"
                    label="Status"
                    value={statusFilter}
                    onChange={setStatusFilter}
                    options={STATUS_OPTIONS}
                    placeholder="All statuses"
                  />
                </div>
              }
              actions={
                <p className="text-sm text-muted-foreground" aria-live="polite">
                  {list.data ? `${list.data.count} in total` : null}
                </p>
              }
            />

            {described?.isForbidden ? (
              <RestrictedState description={described.message} />
            ) : (
              <DataTableShell
                caption="Projects in your organisation"
                columns={
                  canManage
                    ? ["Name", "Code", "Department", "Status", "Starts", "Target end", "Actions"]
                    : ["Name", "Code", "Department", "Status", "Starts", "Target end"]
                }
                state={state}
                emptyTitle={search || statusFilter ? "No matches" : "No projects to show"}
                emptyDescription={
                  search || statusFilter
                    ? "Try a different search term or status."
                    : "No projects have been created yet."
                }
                onRetry={() => void list.refetch()}
              >
                {rows.map((project) => (
                  <TableRow key={project.publicId}>
                    <TableCell className="font-medium">{project.name}</TableCell>
                    <TableCell>{project.code}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {departmentNames.get(project.departmentId) ?? "—"}
                    </TableCell>
                    <TableCell>
                      <Badge variant={project.status === "Active" ? "default" : "secondary"}>
                        {PROJECT_STATUS_LABELS[project.status]}
                      </Badge>
                    </TableCell>
                    <TableCell>{toDateInput(project.startDateUtc) || "—"}</TableCell>
                    <TableCell>{toDateInput(project.targetEndDateUtc) || "—"}</TableCell>
                    {canManage ? (
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          <Button size="sm" variant="outline" onClick={() => setEditing(project)}>
                            Edit
                          </Button>
                          <DeleteProjectAction project={project} />
                        </div>
                      </TableCell>
                    ) : null}
                  </TableRow>
                ))}
              </DataTableShell>
            )}
          </ListPageShell>
        </Can>

        {creating ? <ProjectDialog onClose={() => setCreating(false)} /> : null}
        {editing ? <ProjectDialog project={editing} onClose={() => setEditing(null)} /> : null}
      </AppLayout>
    </ProtectedRoute>
  );
}

function ProjectDialog({ project, onClose }: { project?: ProjectSummary; onClose: () => void }) {
  const isEdit = Boolean(project);
  const { user, hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [form, setForm] = useState({
    name: project?.name ?? "",
    code: project?.code ?? "",
    description: project?.description ?? "",
    departmentId: project?.departmentId ?? "",
    managerId: project?.managerId ?? (isAdmin ? "" : (user?.publicId ?? "")),
    status: (project?.status ?? "Planned") as ProjectStatus,
    startDateUtc: toDateInput(project?.startDateUtc ?? null),
    targetEndDateUtc: toDateInput(project?.targetEndDateUtc ?? null),
  });
  const [touched, setTouched] = useState(false);

  const departments = useQuery(departmentsListQuery());
  // Only Admins and Managers may read the people list, and only an Admin may
  // nominate a different manager — the API forces a Manager's own id otherwise.
  const managers = useQuery(usersListQuery({ enabled: isAdmin }));

  const create = useCreateProject();
  const update = useUpdateProject();
  const mutation = isEdit ? update : create;

  const missing = {
    name: !form.name.trim() ? "Name is required." : undefined,
    code: !form.code.trim() ? "Code is required." : undefined,
    departmentId: !form.departmentId ? "Department is required." : undefined,
    managerId: isAdmin && !form.managerId ? "Manager is required." : undefined,
  };
  const hasMissing = Object.values(missing).some(Boolean);

  function submit() {
    setTouched(true);
    if (hasMissing) return;

    const payload = {
      name: form.name.trim(),
      code: form.code.trim(),
      departmentId: form.departmentId,
      managerId: form.managerId || (user?.publicId ?? ""),
      description: form.description.trim() ? form.description.trim() : null,
      status: form.status,
      startDateUtc: fromDateInput(form.startDateUtc),
      targetEndDateUtc: fromDateInput(form.targetEndDateUtc),
    };

    if (project) {
      update.mutate(
        { publicId: project.publicId, ...payload },
        {
          onSuccess: () => {
            toast.success(`${payload.name} was updated.`);
            onClose();
          },
        },
      );
      return;
    }

    create.mutate(payload, {
      onSuccess: () => {
        toast.success(`${payload.name} was created.`);
        onClose();
      },
    });
  }

  const departmentsError = departments.isError ? describeApiError(departments.error) : null;
  const managersError = managers.isError ? describeApiError(managers.error) : null;

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={isEdit ? `Edit ${project?.name}` : "Add a project"}
      description={
        isAdmin
          ? "The code must be unique; the server enforces that."
          : "Projects you create are led by you — the server sets that automatically."
      }
      submitLabel={isEdit ? "Save changes" : "Add project"}
      onSubmit={submit}
      isPending={mutation.isPending}
      error={mutation.error}
    >
      <TextField
        id="project-name"
        label="Name"
        value={form.name}
        onChange={(value) => setForm((prev) => ({ ...prev, name: value }))}
        required
        error={touched ? missing.name : undefined}
      />
      <TextField
        id="project-code"
        label="Code"
        value={form.code}
        onChange={(value) => setForm((prev) => ({ ...prev, code: value }))}
        required
        error={touched ? missing.code : undefined}
      />
      <TextField
        id="project-description"
        label="Description"
        value={form.description}
        onChange={(value) => setForm((prev) => ({ ...prev, description: value }))}
        placeholder="Optional"
      />
      <RelationSelect
        id="project-department"
        label="Department"
        value={form.departmentId}
        onChange={(value) => setForm((prev) => ({ ...prev, departmentId: value }))}
        required
        options={(departments.data?.departments ?? []).map((department) => ({
          value: department.publicId,
          label: `${department.name} (${department.code})`,
        }))}
        isLoading={departments.isPending}
        isError={departments.isError}
        isForbidden={departmentsError?.isForbidden ?? false}
        emptyMessage="No departments exist yet — create one first."
        error={touched ? missing.departmentId : undefined}
        onRetry={() => void departments.refetch()}
      />
      {isAdmin ? (
        <RelationSelect
          id="project-manager"
          label="Manager"
          value={form.managerId}
          onChange={(value) => setForm((prev) => ({ ...prev, managerId: value }))}
          required
          options={(managers.data?.users ?? []).map((person) => ({
            value: person.publicId,
            label: `${person.fullName} (${person.employeeCode})`,
          }))}
          isLoading={managers.isPending}
          isError={managers.isError}
          isForbidden={managersError?.isForbidden ?? false}
          emptyMessage="No people exist yet — add someone first."
          error={touched ? missing.managerId : undefined}
          onRetry={() => void managers.refetch()}
        />
      ) : null}
      <SelectField
        id="project-status"
        label="Status"
        value={form.status}
        onChange={(value) => setForm((prev) => ({ ...prev, status: value as ProjectStatus }))}
        options={STATUS_OPTIONS}
      />
      <TextField
        id="project-start"
        label="Start date"
        type="date"
        value={form.startDateUtc}
        onChange={(value) => setForm((prev) => ({ ...prev, startDateUtc: value }))}
      />
      <TextField
        id="project-target-end"
        label="Target end date"
        type="date"
        value={form.targetEndDateUtc}
        onChange={(value) => setForm((prev) => ({ ...prev, targetEndDateUtc: value }))}
      />
    </FormDialog>
  );
}

function DeleteProjectAction({ project }: { project: ProjectSummary }) {
  const remove = useDeleteProject();

  return (
    <ConfirmDialog
      trigger={
        <Button size="sm" variant="outline" disabled={remove.isPending}>
          Remove
        </Button>
      }
      title={`Remove ${project.name}?`}
      description="The project is marked as removed and cancelled on the server, and kept for audit purposes."
      confirmLabel="Remove"
      destructive
      onConfirm={() =>
        remove.mutate(project.publicId, {
          onSuccess: () => toast.success(`${project.name} was removed.`),
          onError: (error) => {
            const described = describeApiError(error);
            toast.error(described.title, { description: described.message });
          },
        })
      }
    />
  );
}
