using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.AssignTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.CreateTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.DeleteTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetAllTasks;
using EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetTaskByPublicId;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Tests.TestDoubles;

namespace EnterpriseTaskManagement.Tests.Application;

/// <summary>Role / ownership rules for every task use case (unit, test doubles).</summary>
public sealed class TaskHandlerAuthorizationTests
{
    private readonly FakeProjectRepository _projects = new();
    private readonly FakeTaskRepository _tasks;
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _otherManager = Guid.NewGuid();
    private readonly Guid _employee = Guid.NewGuid();
    private readonly Guid _projectId;
    private readonly Guid _foreignProjectId;

    public TaskHandlerAuthorizationTests()
    {
        _tasks = new FakeTaskRepository(_projects.ManagedBy);
        var project = Build.Project(_manager, "OWN");
        var foreign = Build.Project(_otherManager, "FOREIGN");
        _projects.Seed(project, foreign);
        _projectId = project.PublicId;
        _foreignProjectId = foreign.PublicId;
    }

    private CreateTaskCommandHandler Create() => new(_tasks, _projects, _users, _uow);

    [Fact]
    public async Task Admin_can_create_task_in_any_project()
    {
        var result = await Create().Handle(
            new CreateTaskCommand("T", _foreignProjectId, Guid.NewGuid(), TaskRequesterRole.Admin), default);

        Assert.Equal("Todo", result.Status);
        Assert.Equal(_foreignProjectId, result.ProjectId);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Manager_can_create_task_in_own_project() =>
        Assert.NotNull(await Create().Handle(
            new CreateTaskCommand("T", _projectId, _manager, TaskRequesterRole.Manager), default));

    [Fact]
    public async Task Manager_cannot_create_task_in_foreign_project()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Create().Handle(
            new CreateTaskCommand("T", _foreignProjectId, _manager, TaskRequesterRole.Manager), default));
        Assert.Equal(0, _uow.SaveCount);
    }

    [Fact]
    public async Task Employee_cannot_create_tasks() =>
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Create().Handle(
            new CreateTaskCommand("T", _projectId, _employee, TaskRequesterRole.Employee), default));

    [Fact]
    public async Task Create_in_missing_project_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Create().Handle(
            new CreateTaskCommand("T", Guid.NewGuid(), Guid.NewGuid(), TaskRequesterRole.Admin), default));

    [Fact]
    public async Task Create_with_unknown_assignee_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Create().Handle(
            new CreateTaskCommand("T", _projectId, _manager, TaskRequesterRole.Manager, AssignedUserId: Guid.NewGuid()), default));

    [Fact]
    public async Task Employee_can_change_status_of_own_task()
    {
        var task = Build.Task(_projectId, _employee);
        _tasks.Seed(task);

        var result = await new ChangeTaskStatusCommandHandler(_tasks, _projects, _uow).Handle(
            new ChangeTaskStatusCommand(task.PublicId, "InProgress", _employee, TaskRequesterRole.Employee), default);

        Assert.Equal("InProgress", result.Status);
    }

    [Fact]
    public async Task Employee_cannot_change_status_of_someone_elses_task()
    {
        var task = Build.Task(_projectId, Guid.NewGuid());
        _tasks.Seed(task);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new ChangeTaskStatusCommandHandler(_tasks, _projects, _uow).Handle(
                new ChangeTaskStatusCommand(task.PublicId, "InProgress", _employee, TaskRequesterRole.Employee), default));
        Assert.Equal(TaskItemStatus.Todo, task.Status);
    }

    [Fact]
    public async Task Manager_cannot_change_status_in_foreign_project()
    {
        var task = Build.Task(_foreignProjectId, _employee);
        _tasks.Seed(task);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new ChangeTaskStatusCommandHandler(_tasks, _projects, _uow).Handle(
                new ChangeTaskStatusCommand(task.PublicId, "InProgress", _manager, TaskRequesterRole.Manager), default));
    }

    [Fact]
    public async Task Change_status_rejects_unknown_status_and_completed_tasks()
    {
        var open = Build.Task(_projectId, _employee);
        var done = Build.Task(_projectId, _employee, TaskItemStatus.Completed);
        _tasks.Seed(open, done);
        var handler = new ChangeTaskStatusCommandHandler(_tasks, _projects, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new ChangeTaskStatusCommand(open.PublicId, "Nope", _manager, TaskRequesterRole.Manager), default));
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new ChangeTaskStatusCommand(done.PublicId, "InProgress", _manager, TaskRequesterRole.Manager), default));
    }

    [Fact]
    public async Task Employee_cannot_assign_tasks_even_their_own()
    {
        var task = Build.Task(_projectId, _employee);
        _tasks.Seed(task);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new AssignTaskCommandHandler(_tasks, _projects, _users, _uow).Handle(
                new AssignTaskCommand(task.PublicId, Guid.NewGuid(), _employee, TaskRequesterRole.Employee), default));
    }

    [Fact]
    public async Task Manager_assigns_active_user_and_cannot_assign_inactive_user()
    {
        var active = Build.User(seq: 2);
        var inactive = Build.User(active: false, seq: 3);
        _users.Seed(active, inactive);
        var task = Build.Task(_projectId);
        _tasks.Seed(task);
        var handler = new AssignTaskCommandHandler(_tasks, _projects, _users, _uow);

        var result = await handler.Handle(new AssignTaskCommand(task.PublicId, active.PublicId, _manager, TaskRequesterRole.Manager), default);
        Assert.Equal(active.PublicId, result.AssignedUserId);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new AssignTaskCommand(task.PublicId, inactive.PublicId, _manager, TaskRequesterRole.Manager), default));
    }

    [Fact]
    public async Task Null_assignee_unassigns()
    {
        var task = Build.Task(_projectId, _employee);
        _tasks.Seed(task);

        var result = await new AssignTaskCommandHandler(_tasks, _projects, _users, _uow).Handle(
            new AssignTaskCommand(task.PublicId, null, Guid.NewGuid(), TaskRequesterRole.Admin), default);

        Assert.Null(result.AssignedUserId);
    }

    [Fact]
    public async Task Employee_reads_only_own_tasks()
    {
        var mine = Build.Task(_projectId, _employee);
        var theirs = Build.Task(_projectId, Guid.NewGuid());
        _tasks.Seed(mine, theirs);
        var getOne = new GetTaskByPublicIdQueryHandler(_tasks, _projects);

        var list = await new GetAllTasksQueryHandler(_tasks, _projects).Handle(
            new GetAllTasksQuery(_employee, TaskRequesterRole.Employee), default);

        Assert.Equal([mine.PublicId], list.Tasks.Select(t => t.PublicId));
        Assert.NotNull(await getOne.Handle(new GetTaskByPublicIdQuery(mine.PublicId, _employee, TaskRequesterRole.Employee), default));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            getOne.Handle(new GetTaskByPublicIdQuery(theirs.PublicId, _employee, TaskRequesterRole.Employee), default));
    }

    [Fact]
    public async Task Manager_lists_only_tasks_in_projects_they_lead()
    {
        var own = Build.Task(_projectId);
        var foreign = Build.Task(_foreignProjectId);
        _tasks.Seed(own, foreign);

        var list = await new GetAllTasksQueryHandler(_tasks, _projects).Handle(
            new GetAllTasksQuery(_manager, TaskRequesterRole.Manager), default);
        var forced = await new GetAllTasksQueryHandler(_tasks, _projects).Handle(
            new GetAllTasksQuery(_manager, TaskRequesterRole.Manager, ProjectId: _foreignProjectId), default);

        Assert.Equal([own.PublicId], list.Tasks.Select(t => t.PublicId));
        Assert.Empty(forced.Tasks);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => new GetTaskByPublicIdQueryHandler(_tasks, _projects)
            .Handle(new GetTaskByPublicIdQuery(foreign.PublicId, _manager, TaskRequesterRole.Manager), default));
    }

    [Fact]
    public async Task Admin_lists_everything()
    {
        _tasks.Seed(Build.Task(_projectId), Build.Task(_foreignProjectId));

        var list = await new GetAllTasksQueryHandler(_tasks, _projects).Handle(
            new GetAllTasksQuery(Guid.NewGuid(), TaskRequesterRole.Admin), default);

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Delete_soft_deletes_for_owner_and_forbids_employee()
    {
        var task = Build.Task(_projectId, _employee);
        _tasks.Seed(task);
        var handler = new DeleteTaskCommandHandler(_tasks, _projects, _uow);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new DeleteTaskCommand(task.PublicId, _employee, TaskRequesterRole.Employee), default));

        await handler.Handle(new DeleteTaskCommand(task.PublicId, _manager, TaskRequesterRole.Manager), default);

        Assert.True(task.IsDeleted);
        Assert.Contains(task, _tasks.SoftDeleted);
    }
}
