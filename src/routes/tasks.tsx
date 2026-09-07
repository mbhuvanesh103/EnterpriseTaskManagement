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
import { RelationSelect, SelectField } from "@/components/common/SelectField";
import { AppLayout, ListPageShell } from "@/components/layout/AppLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { TableCell, TableRow } from "@/components/ui/table";
import { projectsListQuery } from "@/features/projects/projects";
import {
  tasksListQuery,
  useAssignTask,
  useChangeTaskStatus,
  useCreateTask,
  useDeleteTask,
  useUpdateTask,
} from "@/features/tasks/tasks";
import { usersListQuery } from "@/features/users/users";
import { describeApiError } from "@/lib/api/errors";
import { TextField } from "@/routes/users";
import {
  TASK_PRIORITIES,
  TASK_STATUSES,
  TASK_STATUS_LABELS,
  type TaskPriority,
  type TaskStatus,
  type TaskSummary,
} from "@/types/domain";

export const Route = createFileRoute("/tasks")({
  head: () => ({
    meta: [
      { title: "Tasks — Enterprise Task Management" },
      {
        name: "description",
        content: "Work through tasks by project, priority and workflow status.",
      },
      { property: "og:title", content: "Tasks — Enterprise Task Management" },
      {
        property: "og:description",
        content: "Task board for the Enterprise Task Management workspace.",
      },
    ],
  }),
  component: TasksPage,
});

const STATUS_OPTIONS = TASK_STATUSES.map((status) => ({
  value: status,
  label: TASK_STATUS_LABELS[status],
}));

const PRIORITY_OPTIONS = TASK_PRIORITIES.map((priority) => ({
  value: priority,
  label: priority,
}));

function toDateInput(value: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "" : date.toISOString().slice(0, 10);
}

function fromDateInput(value: string): string | null {
  return value ? new Date(`${value}T00:00:00Z`).toISOString() : null;
}

function TasksPage() {
  const canView = useCapability("viewTasks");
  const canManage = useCapability("manageTasks");

  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [projectFilter, setProjectFilter] = useState("");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<TaskSummary | null>(null);
  const [assigning, setAssigning] = useState<TaskSummary | null>(null);

  const list = useQuery(
    tasksListQuery(projectFilter ? { projectId: projectFilter, enabled: canView } : { enabled: canView }),
  );
  const projects = useQuery(projectsListQuery({ enabled: canView }));

  const projectNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const project of projects.data?.projects ?? []) map.set(project.publicId, project.name);
    return map;
  }, [projects.data]);

  const rows = useMemo(
    () =>
      (list.data?.tasks ?? []).filter(
        (task) =>
          matchesSearch(search, task.title, task.description) &&
          (!statusFilter || task.status === statusFilter),
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
        title="Tasks"
        description="Tasks are read live from the API. You only see the ones the server allows."
        actions={
          <Can do="manageTasks">
            <Button size="sm" onClick={() => setCreating(true)}>
              Add task
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
              filters={
                <>
                  <div className="w-full max-w-[12rem]">
                    <SelectField
                      id="task-status-filter"
                      label="Status"
                      value={statusFilter}
                      onChange={setStatusFilter}
                      options={STATUS_OPTIONS}
                      placeholder="All statuses"
                    />
                  </div>
                  <div className="w-full max-w-[14rem]">
                    <SelectField
                      id="task-project-filter"
                      label="Project"
                      value={projectFilter}
                      onChange={setProjectFilter}
                      options={(projects.data?.projects ?? []).map((project) => ({
                        value: project.publicId,
                        label: project.name,
                      }))}
                      placeholder="All projects"
                      disabled={projects.isPending || projects.isError}
                    />
                  </div>
                </>
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
                caption="Tasks visible to you"
                columns={
                  canManage
                    ? ["Title", "Project", "Priority", "Status", "Due", "Actions"]
                    : ["Title", "Project", "Priority", "Status", "Due"]
                }
                state={state}
                emptyTitle={search || statusFilter || projectFilter ? "No matches" : "No tasks yet"}
                emptyDescription={
                  search || statusFilter || projectFilter
                    ? "Try a different search term or filter."
                    : "Nothing has been assigned to you yet."
                }
                onRetry={() => void list.refetch()}
              >
                {rows.map((task) => (
                  <TableRow key={task.publicId}>
                    <TableCell className="font-medium">{task.title}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {projectNames.get(task.projectId) ?? "—"}
                    </TableCell>
                    <TableCell>
                      <Badge variant={task.priority === "Critical" ? "destructive" : "secondary"}>
                        {task.priority}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <StatusChanger task={task} />
                    </TableCell>
                    <TableCell>{toDateInput(task.dueDateUtc) || "—"}</TableCell>
                    {canManage ? (
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          <Button size="sm" variant="outline" onClick={() => setEditing(task)}>
                            Edit
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => setAssigning(task)}>
                            Assign
                          </Button>
                          <DeleteTaskAction task={task} />
                        </div>
                      </TableCell>
                    ) : null}
                  </TableRow>
                ))}
              </DataTableShell>
            )}
          </ListPageShell>
        </Can>

        {creating ? <CreateTaskDialog onClose={() => setCreating(false)} /> : null}
        {editing ? <EditTaskDialog task={editing} onClose={() => setEditing(null)} /> : null}
        {assigning ? (
          <AssignTaskDialog task={assigning} onClose={() => setAssigning(null)} />
        ) : null}
      </AppLayout>
    </ProtectedRoute>
  );
}

/**
 * Status changes always go through the dedicated status endpoint. Employees may
 * use it for their own tasks; the server rejects anything else and refuses
 * transitions the domain doesn't allow.
 */
function StatusChanger({ task }: { task: TaskSummary }) {
  const changeStatus = useChangeTaskStatus();

  return (
    <SelectField
      id={`task-status-${task.publicId}`}
      label="Status"
      value={task.status}
      disabled={changeStatus.isPending}
      onChange={(value) =>
        changeStatus.mutate(
          { publicId: task.publicId, status: value as TaskStatus },
          {
            onSuccess: (updated) =>
              toast.success(`Moved to ${TASK_STATUS_LABELS[updated.status]}.`),
            onError: (error) => {
              const described = describeApiError(error);
              toast.error(described.title, { description: described.message });
            },
          },
        )
      }
      options={STATUS_OPTIONS}
    />
  );
}

function CreateTaskDialog({ onClose }: { onClose: () => void }) {
  const canAssign = useCapability("manageTasks");
  const [form, setForm] = useState({
    title: "",
    projectId: "",
    description: "",
    assignedUserId: "",
    priority: "Medium" as TaskPriority,
    dueDateUtc: "",
  });
  const [touched, setTouched] = useState(false);

  const projects = useQuery(projectsListQuery());
  const people = useQuery(usersListQuery({ enabled: canAssign }));
  const create = useCreateTask();

  const missing = {
    title: !form.title.trim() ? "Title is required." : undefined,
    projectId: !form.projectId ? "Project is required." : undefined,
  };
  const hasMissing = Object.values(missing).some(Boolean);

  const projectsError = projects.isError ? describeApiError(projects.error) : null;
  const peopleError = people.isError ? describeApiError(people.error) : null;

  function submit() {
    setTouched(true);
    if (hasMissing) return;
    create.mutate(
      {
        title: form.title.trim(),
        projectId: form.projectId,
        description: form.description.trim() ? form.description.trim() : null,
        assignedUserId: form.assignedUserId || null,
        priority: form.priority,
        dueDateUtc: fromDateInput(form.dueDateUtc),
      },
      {
        onSuccess: (task) => {
          toast.success(`“${task.title}” was created.`);
          onClose();
        },
      },
    );
  }

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title="Add a task"
      description="You can only add tasks to projects the server lets you manage."
      submitLabel="Add task"
      onSubmit={submit}
      isPending={create.isPending}
      error={create.error}
    >
      <TextField
        id="task-title"
        label="Title"
        value={form.title}
        onChange={(value) => setForm((prev) => ({ ...prev, title: value }))}
        required
        error={touched ? missing.title : undefined}
      />
      <RelationSelect
        id="task-project"
        label="Project"
        value={form.projectId}
        onChange={(value) => setForm((prev) => ({ ...prev, projectId: value }))}
        required
        options={(projects.data?.projects ?? []).map((project) => ({
          value: project.publicId,
          label: `${project.name} (${project.code})`,
        }))}
        isLoading={projects.isPending}
        isError={projects.isError}
        isForbidden={projectsError?.isForbidden ?? false}
        emptyMessage="No projects exist yet — create one first."
        error={touched ? missing.projectId : undefined}
        onRetry={() => void projects.refetch()}
      />
      <TextField
        id="task-description"
        label="Description"
        value={form.description}
        onChange={(value) => setForm((prev) => ({ ...prev, description: value }))}
        placeholder="Optional"
      />
      <RelationSelect
        id="task-assignee"
        label="Assign to"
        value={form.assignedUserId}
        onChange={(value) => setForm((prev) => ({ ...prev, assignedUserId: value }))}
        options={(people.data?.users ?? []).map((person) => ({
          value: person.publicId,
          label: `${person.fullName} (${person.employeeCode})`,
        }))}
        isLoading={people.isPending}
        isError={people.isError}
        isForbidden={peopleError?.isForbidden ?? false}
        permitted={canAssign}
        placeholder="Leave unassigned"
        emptyMessage="No people exist yet."
        onRetry={() => void people.refetch()}
      />
      <SelectField
        id="task-priority"
        label="Priority"
        value={form.priority}
        onChange={(value) => setForm((prev) => ({ ...prev, priority: value as TaskPriority }))}
        options={PRIORITY_OPTIONS}
      />
      <TextField
        id="task-due"
        label="Due date"
        type="date"
        value={form.dueDateUtc}
        onChange={(value) => setForm((prev) => ({ ...prev, dueDateUtc: value }))}
      />
    </FormDialog>
  );
}

/** The general update endpoint carries descriptive fields only, by design. */
function EditTaskDialog({ task, onClose }: { task: TaskSummary; onClose: () => void }) {
  const [form, setForm] = useState({
    title: task.title,
    description: task.description ?? "",
    priority: task.priority,
    dueDateUtc: toDateInput(task.dueDateUtc),
  });
  const [touched, setTouched] = useState(false);
  const update = useUpdateTask();

  const titleError = !form.title.trim() ? "Title is required." : undefined;

  function submit() {
    setTouched(true);
    if (titleError) return;
    update.mutate(
      {
        publicId: task.publicId,
        title: form.title.trim(),
        description: form.description.trim() ? form.description.trim() : null,
        priority: form.priority,
        dueDateUtc: fromDateInput(form.dueDateUtc),
      },
      {
        onSuccess: () => {
          toast.success("Task updated.");
          onClose();
        },
      },
    );
  }

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={`Edit “${task.title}”`}
      description="Who it's assigned to and its status are changed with the Assign button and the status picker."
      submitLabel="Save changes"
      onSubmit={submit}
      isPending={update.isPending}
      error={update.error}
    >
      <TextField
        id="task-title"
        label="Title"
        value={form.title}
        onChange={(value) => setForm((prev) => ({ ...prev, title: value }))}
        required
        error={touched ? titleError : undefined}
      />
      <TextField
        id="task-description"
        label="Description"
        value={form.description}
        onChange={(value) => setForm((prev) => ({ ...prev, description: value }))}
        placeholder="Optional"
      />
      <SelectField
        id="task-priority"
        label="Priority"
        value={form.priority}
        onChange={(value) => setForm((prev) => ({ ...prev, priority: value as TaskPriority }))}
        options={PRIORITY_OPTIONS}
      />
      <TextField
        id="task-due"
        label="Due date"
        type="date"
        value={form.dueDateUtc}
        onChange={(value) => setForm((prev) => ({ ...prev, dueDateUtc: value }))}
      />
    </FormDialog>
  );
}

/** Uses the dedicated assignee endpoint; an empty choice unassigns the task. */
function AssignTaskDialog({ task, onClose }: { task: TaskSummary; onClose: () => void }) {
  const [assignedUserId, setAssignedUserId] = useState(task.assignedUserId ?? "");
  const people = useQuery(usersListQuery());
  const assign = useAssignTask();
  const peopleError = people.isError ? describeApiError(people.error) : null;

  return (
    <FormDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={`Assign “${task.title}”`}
      description="Leave the choice empty to take the task off everyone's list."
      submitLabel="Save assignment"
      onSubmit={() =>
        assign.mutate(
          { publicId: task.publicId, assignedUserId: assignedUserId || null },
          {
            onSuccess: () => {
              toast.success("Assignment updated.");
              onClose();
            },
          },
        )
      }
      isPending={assign.isPending}
      error={assign.error}
    >
      <RelationSelect
        id="task-assignee"
        label="Assign to"
        value={assignedUserId}
        onChange={setAssignedUserId}
        options={(people.data?.users ?? []).map((person) => ({
          value: person.publicId,
          label: `${person.fullName} (${person.employeeCode})`,
        }))}
        isLoading={people.isPending}
        isError={people.isError}
        isForbidden={peopleError?.isForbidden ?? false}
        placeholder="Nobody (unassigned)"
        emptyMessage="No people exist yet."
        onRetry={() => void people.refetch()}
      />
    </FormDialog>
  );
}

function DeleteTaskAction({ task }: { task: TaskSummary }) {
  const remove = useDeleteTask();

  return (
    <ConfirmDialog
      trigger={
        <Button size="sm" variant="outline" disabled={remove.isPending}>
          Remove
        </Button>
      }
      title={`Remove “${task.title}”?`}
      description="The task is marked as removed on the server and kept for audit purposes."
      confirmLabel="Remove"
      destructive
      onConfirm={() =>
        remove.mutate(task.publicId, {
          onSuccess: () => toast.success("Task removed."),
          onError: (error) => {
            const described = describeApiError(error);
            toast.error(described.title, { description: described.message });
          },
        })
      }
    />
  );
}
