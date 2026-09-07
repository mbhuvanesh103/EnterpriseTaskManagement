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
import { RelationSelect } from "@/components/common/SelectField";
import { FieldError } from "@/components/common/ValidationSummary";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TableCell, TableRow } from "@/components/ui/table";
import { departmentsListQuery } from "@/features/departments/departments";
import { useCreateUser, useDeleteUser, useUpdateUser, usersListQuery } from "@/features/users/users";
import { describeApiError } from "@/lib/api/errors";
import type { UserSummary } from "@/types/domain";

export const Route = createFileRoute("/users")({
  head: () => ({
    meta: [
      { title: "Users — Enterprise Task Management" },
      {
        name: "description",
        content: "Review and manage the people in your organisation, their codes and status.",
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

interface UserFormState {
  firstName: string;
  lastName: string;
  email: string;
  employeeCode: string;
  departmentId: string;
}

const EMPTY_FORM: UserFormState = {
  firstName: "",
  lastName: "",
  email: "",
  employeeCode: "",
  departmentId: "",
};

function UsersPage() {
  const canView = useCapability("viewUsers");
  const canManage = useCapability("manageUsers");

  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<UserSummary | null>(null);
  const [creating, setCreating] = useState(false);

  const list = useQuery(usersListQuery({ enabled: canView }));

  const rows = useMemo(
    () =>
      (list.data?.users ?? []).filter((user) =>
        matchesSearch(search, user.fullName, user.email, user.employeeCode),
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
        title="Users"
        description="People are read live from the API. Roles and passwords are managed by the server."
        actions={
          <Can do="manageUsers">
            <Button size="sm" onClick={() => setCreating(true)}>
              Add person
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
                caption="People in your organisation"
                columns={
                  canManage
                    ? ["Name", "Email", "Employee code", "Status", "Actions"]
                    : ["Name", "Email", "Employee code", "Status"]
                }
                state={state}
                emptyTitle={search ? "No matches" : "No people to show"}
                emptyDescription={
                  search
                    ? "Try a different search term."
                    : "Nobody has been added to the directory yet."
                }
                onRetry={() => void list.refetch()}
              >
                {rows.map((user) => (
                  <TableRow key={user.publicId}>
                    <TableCell className="font-medium">{user.fullName}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>{user.employeeCode}</TableCell>
                    <TableCell>
                      <Badge variant={user.isActive ? "default" : "secondary"}>
                        {user.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TableCell>
                    {canManage ? (
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          <Button size="sm" variant="outline" onClick={() => setEditing(user)}>
                            Edit
                          </Button>
                          <DeleteUserAction user={user} />
                        </div>
                      </TableCell>
                    ) : null}
                  </TableRow>
                ))}
              </DataTableShell>
            )}
          </ListPageShell>
        </Can>

        {creating ? <CreateUserDialog onClose={() => setCreating(false)} /> : null}
        {editing ? <EditUserDialog user={editing} onClose={() => setEditing(null)} /> : null}
      </AppLayout>
    </ProtectedRoute>
  );
}

/** Department picker shared by both dialogs; options come from the real API. */
function DepartmentPicker({
  value,
  onChange,
  error,
}: {
  value: string;
  onChange: (value: string) => void;
  error?: string | undefined;
}) {
  const departments = useQuery(departmentsListQuery());
  const described = departments.isError ? describeApiError(departments.error) : null;

  return (
    <RelationSelect
      id="user-department"
      label="Department"
      value={value}
      onChange={onChange}
      required
      options={(departments.data?.departments ?? []).map((department) => ({
        value: department.publicId,
        label: `${department.name} (${department.code})`,
      }))}
      isLoading={departments.isPending}
      isError={departments.isError}
      isForbidden={described?.isForbidden ?? false}
      emptyMessage="No departments exist yet — create one first."
      error={error}
      onRetry={() => void departments.refetch()}
    />
  );
}

function CreateUserDialog({ onClose }: { onClose: () => void }) {
  const [form, setForm] = useState<UserFormState>(EMPTY_FORM);
  const [touched, setTouched] = useState(false);
  const create = useCreateUser();

  const missing = {
    firstName: !form.firstName.trim() ? "First name is required." : undefined,
    lastName: !form.lastName.trim() ? "Last name is required." : undefined,
    email: !form.email.trim() ? "Email is required." : undefined,
    employeeCode: !form.employeeCode.trim() ? "Employee code is required." : undefined,
    departmentId: !form.departmentId ? "Department is required." : undefined,
  };
  const hasMissing = Object.values(missing).some(Boolean);

  function submit() {
    setTouched(true);
    if (hasMissing) return;
    create.mutate(
      {
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        employeeCode: form.employeeCode.trim(),
        departmentId: form.departmentId,
      },
      {
        onSuccess: (user) => {
          toast.success(`${user.fullName} was added.`);
          onClose();
        },
      },
    );
  }

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title="Add a person"
      description="The server assigns the sign-in role and credentials — this form doesn't set a password."
      submitLabel="Add person"
      onSubmit={submit}
      isPending={create.isPending}
      error={create.error}
    >
      <TextField
        id="user-first-name"
        label="First name"
        value={form.firstName}
        onChange={(value) => setForm((prev) => ({ ...prev, firstName: value }))}
        required
        error={touched ? missing.firstName : undefined}
      />
      <TextField
        id="user-last-name"
        label="Last name"
        value={form.lastName}
        onChange={(value) => setForm((prev) => ({ ...prev, lastName: value }))}
        required
        error={touched ? missing.lastName : undefined}
      />
      <TextField
        id="user-email"
        label="Email"
        type="email"
        value={form.email}
        onChange={(value) => setForm((prev) => ({ ...prev, email: value }))}
        required
        error={touched ? missing.email : undefined}
      />
      <TextField
        id="user-employee-code"
        label="Employee code"
        value={form.employeeCode}
        onChange={(value) => setForm((prev) => ({ ...prev, employeeCode: value }))}
        required
        hint="The server checks the exact format and that it isn't already taken."
        error={touched ? missing.employeeCode : undefined}
      />
      <DepartmentPicker
        value={form.departmentId}
        onChange={(value) => setForm((prev) => ({ ...prev, departmentId: value }))}
        error={touched ? missing.departmentId : undefined}
      />
    </FormDialog>
  );
}

function EditUserDialog({ user, onClose }: { user: UserSummary; onClose: () => void }) {
  // The API's user read model returns a single full name and no department, so
  // the name is split for editing and the department must be re-selected. Both
  // are stated on screen rather than guessed silently.
  const [first = "", ...rest] = user.fullName.split(" ");
  const [form, setForm] = useState({
    firstName: first,
    lastName: rest.join(" "),
    email: user.email,
    departmentId: "",
  });
  const [touched, setTouched] = useState(false);
  const update = useUpdateUser();

  const missing = {
    firstName: !form.firstName.trim() ? "First name is required." : undefined,
    lastName: !form.lastName.trim() ? "Last name is required." : undefined,
    email: !form.email.trim() ? "Email is required." : undefined,
    departmentId: !form.departmentId ? "Please confirm the department." : undefined,
  };
  const hasMissing = Object.values(missing).some(Boolean);

  function submit() {
    setTouched(true);
    if (hasMissing) return;
    update.mutate(
      {
        publicId: user.publicId,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        departmentId: form.departmentId,
      },
      {
        onSuccess: (updated) => {
          toast.success(`${updated.fullName} was updated.`);
          onClose();
        },
      },
    );
  }

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={`Edit ${user.fullName}`}
      description="The employee code, role and password can't be changed through this screen."
      submitLabel="Save changes"
      onSubmit={submit}
      isPending={update.isPending}
      error={update.error}
    >
      <TextField
        id="user-first-name"
        label="First name"
        value={form.firstName}
        onChange={(value) => setForm((prev) => ({ ...prev, firstName: value }))}
        required
        error={touched ? missing.firstName : undefined}
      />
      <TextField
        id="user-last-name"
        label="Last name"
        value={form.lastName}
        onChange={(value) => setForm((prev) => ({ ...prev, lastName: value }))}
        required
        error={touched ? missing.lastName : undefined}
      />
      <TextField
        id="user-email"
        label="Email"
        type="email"
        value={form.email}
        onChange={(value) => setForm((prev) => ({ ...prev, email: value }))}
        required
        error={touched ? missing.email : undefined}
      />
      <DepartmentPicker
        value={form.departmentId}
        onChange={(value) => setForm((prev) => ({ ...prev, departmentId: value }))}
        error={touched ? missing.departmentId : undefined}
      />
      <p className="text-xs text-muted-foreground">
        The people list doesn&apos;t include the current department, so please confirm it before
        saving.
      </p>
    </FormDialog>
  );
}

function DeleteUserAction({ user }: { user: UserSummary }) {
  const remove = useDeleteUser();

  return (
    <ConfirmDialog
      trigger={
        <Button size="sm" variant="outline" disabled={remove.isPending}>
          Remove
        </Button>
      }
      title={`Remove ${user.fullName}?`}
      description="The record is marked as removed on the server and kept for audit purposes. It disappears from this list."
      confirmLabel="Remove"
      destructive
      onConfirm={() =>
        remove.mutate(user.publicId, {
          onSuccess: () => toast.success(`${user.fullName} was removed.`),
          onError: (error) => {
            const described = describeApiError(error);
            toast.error(described.title, { description: described.message });
          },
        })
      }
    />
  );
}

/** Small labelled text input with inline error wiring. */
export function TextField({
  id,
  label,
  value,
  onChange,
  type = "text",
  required = false,
  hint,
  error,
  placeholder,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
  hint?: string | undefined;
  error?: string | undefined;
  placeholder?: string | undefined;
}) {
  const describedBy = [error ? `${id}-error` : null, hint ? `${id}-hint` : null]
    .filter(Boolean)
    .join(" ");

  return (
    <div>
      <Label htmlFor={id}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </Label>
      <Input
        id={id}
        type={type}
        value={value}
        placeholder={placeholder}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy || undefined}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1.5"
      />
      {hint ? (
        <p id={`${id}-hint`} className="mt-1 text-xs text-muted-foreground">
          {hint}
        </p>
      ) : null}
      <FieldError id={`${id}-error`} message={error} />
    </div>
  );
}
