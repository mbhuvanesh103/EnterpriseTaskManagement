import { useQuery } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { toast } from "sonner";

import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { Can, useCapability } from "@/auth/RoleGate";
import { ConfirmDialog } from "@/components/common/ConfirmDialog";
import { DataTableShell, type ListState } from "@/components/common/DataTableShell";
import { RestrictedState } from "@/components/common/ErrorState";
import { FormDialog } from "@/components/common/FormDialog";
import { ListToolbar, matchesSearch } from "@/components/common/ListToolbar";
import { SelectField } from "@/components/common/SelectField";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { TableCell, TableRow } from "@/components/ui/table";
import {
  departmentsListQuery,
  useCreateDepartment,
  useDeleteDepartment,
  useUpdateDepartment,
} from "@/features/departments/departments";
import { describeApiError } from "@/lib/api/errors";
import { TextField } from "@/routes/users";
import type { DepartmentSummary } from "@/types/domain";

export const Route = createFileRoute("/departments")({
  head: () => ({
    meta: [
      { title: "Departments — Enterprise Task Management" },
      {
        name: "description",
        content: "Browse and manage the departments that own projects and people.",
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
  const canView = useCapability("viewDepartments");
  const canManage = useCapability("manageDepartments");

  const [search, setSearch] = useState("");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<DepartmentSummary | null>(null);

  const list = useQuery(departmentsListQuery({ enabled: canView }));

  const rows = useMemo(
    () =>
      (list.data?.departments ?? []).filter((department) =>
        matchesSearch(search, department.name, department.code, department.description),
      ),
    [list.data, search],
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
        title="Departments"
        description="Departments are read live from the API."
        actions={
          <Can do="manageDepartments">
            <Button size="sm" onClick={() => setCreating(true)}>
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
                caption="Departments in your organisation"
                columns={
                  canManage
                    ? ["Name", "Code", "Description", "Status", "Actions"]
                    : ["Name", "Code", "Description", "Status"]
                }
                state={state}
                emptyTitle={search ? "No matches" : "No departments to show"}
                emptyDescription={
                  search ? "Try a different search term." : "No departments have been created yet."
                }
                onRetry={() => void list.refetch()}
              >
                {rows.map((department) => (
                  <TableRow key={department.publicId}>
                    <TableCell className="font-medium">{department.name}</TableCell>
                    <TableCell>{department.code}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {department.description ?? "—"}
                    </TableCell>
                    <TableCell>
                      <Badge variant={department.isActive ? "default" : "secondary"}>
                        {department.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TableCell>
                    {canManage ? (
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => setEditing(department)}
                          >
                            Edit
                          </Button>
                          <DeleteDepartmentAction department={department} />
                        </div>
                      </TableCell>
                    ) : null}
                  </TableRow>
                ))}
              </DataTableShell>
            )}
          </ListPageShell>
        </Can>

        {creating ? <DepartmentDialog onClose={() => setCreating(false)} /> : null}
        {editing ? (
          <DepartmentDialog department={editing} onClose={() => setEditing(null)} />
        ) : null}
      </AppLayout>
    </ProtectedRoute>
  );
}

function DepartmentDialog({
  department,
  onClose,
}: {
  department?: DepartmentSummary;
  onClose: () => void;
}) {
  const isEdit = Boolean(department);
  const [form, setForm] = useState({
    name: department?.name ?? "",
    code: department?.code ?? "",
    description: department?.description ?? "",
    isActive: department?.isActive ?? true,
  });
  const [touched, setTouched] = useState(false);

  const create = useCreateDepartment();
  const update = useUpdateDepartment();
  const mutation = isEdit ? update : create;

  const missing = {
    name: !form.name.trim() ? "Name is required." : undefined,
    code: !form.code.trim() ? "Code is required." : undefined,
  };
  const hasMissing = Object.values(missing).some(Boolean);

  function submit() {
    setTouched(true);
    if (hasMissing) return;

    const payload = {
      name: form.name.trim(),
      code: form.code.trim(),
      description: form.description.trim() ? form.description.trim() : null,
      isActive: form.isActive,
    };

    if (department) {
      update.mutate(
        { publicId: department.publicId, ...payload },
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

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={isEdit ? `Edit ${department?.name}` : "Add a department"}
      description="The code must be unique across the organisation; the server enforces that."
      submitLabel={isEdit ? "Save changes" : "Add department"}
      onSubmit={submit}
      isPending={mutation.isPending}
      error={mutation.error}
    >
      <TextField
        id="department-name"
        label="Name"
        value={form.name}
        onChange={(value) => setForm((prev) => ({ ...prev, name: value }))}
        required
        error={touched ? missing.name : undefined}
      />
      <TextField
        id="department-code"
        label="Code"
        value={form.code}
        onChange={(value) => setForm((prev) => ({ ...prev, code: value }))}
        required
        error={touched ? missing.code : undefined}
      />
      <TextField
        id="department-description"
        label="Description"
        value={form.description}
        onChange={(value) => setForm((prev) => ({ ...prev, description: value }))}
        placeholder="Optional"
      />
      <SelectField
        id="department-status"
        label="Status"
        value={form.isActive ? "active" : "inactive"}
        onChange={(value) => setForm((prev) => ({ ...prev, isActive: value === "active" }))}
        options={[
          { value: "active", label: "Active" },
          { value: "inactive", label: "Inactive" },
        ]}
      />
    </FormDialog>
  );
}

function DeleteDepartmentAction({ department }: { department: DepartmentSummary }) {
  const remove = useDeleteDepartment();

  return (
    <ConfirmDialog
      trigger={
        <Button size="sm" variant="outline" disabled={remove.isPending}>
          Remove
        </Button>
      }
      title={`Remove ${department.name}?`}
      description="The department is marked as removed and deactivated on the server, and kept for audit purposes."
      confirmLabel="Remove"
      destructive
      onConfirm={() =>
        remove.mutate(department.publicId, {
          onSuccess: () => toast.success(`${department.name} was removed.`),
          onError: (error) => {
            const described = describeApiError(error);
            toast.error(described.title, { description: described.message });
          },
        })
      }
    />
  );
}
