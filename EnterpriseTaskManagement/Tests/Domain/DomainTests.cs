using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Domain.ValueObjects;
using EnterpriseTaskManagement.Tests.TestDoubles;

namespace EnterpriseTaskManagement.Tests.Domain;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("  John.Doe@Example.COM ", "john.doe@example.com")]
    [InlineData("a@b.io", "a@b.io")]
    public void Email_is_trimmed_and_lower_cased(string input, string expected) =>
        Assert.Equal(expected, Email.Create(input).Value);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("a@b")]
    [InlineData("a b@c.com")]
    public void Email_rejects_invalid_values(string input) =>
        Assert.Throws<DomainException>(() => Email.Create(input));

    [Fact]
    public void Emails_with_same_value_are_equal() =>
        Assert.Equal(Email.Create("X@Y.com"), Email.Create("x@y.com"));

    [Theory]
    [InlineData("emp000123", "EMP000123")]
    [InlineData(" EMP999999 ", "EMP999999")]
    public void EmployeeCode_is_normalized(string input, string expected) =>
        Assert.Equal(expected, EmployeeCode.Create(input).Value);

    [Theory]
    [InlineData("")]
    [InlineData("EMP12345")]
    [InlineData("EMP1234567")]
    [InlineData("ABC000001")]
    public void EmployeeCode_rejects_invalid_values(string input) =>
        Assert.Throws<DomainException>(() => EmployeeCode.Create(input));

    [Fact]
    public void FullName_requires_first_and_last_name()
    {
        Assert.Throws<DomainException>(() => FullName.Create("", "Doe"));
        Assert.Throws<DomainException>(() => FullName.Create("John", " "));
        Assert.Equal("John Doe", FullName.Create(" John ", " Doe ").Value);
    }
}

public sealed class UserTests
{
    [Fact]
    public void Register_creates_active_employee_with_public_id_and_event()
    {
        var user = User.Register(FullName.Create("A", "B"), Email.Create("a@b.com"), EmployeeCode.FromSequence(1), Guid.NewGuid());

        Assert.True(user.IsActive);
        Assert.Equal(UserRole.Employee, user.Role);
        Assert.NotEqual(Guid.Empty, user.PublicId);
        Assert.False(user.HasPassword);
        Assert.NotEmpty(user.DomainEvents);
    }

    [Fact]
    public void Register_requires_department() =>
        Assert.Throws<DomainException>(() =>
            User.Register(FullName.Create("A", "B"), Email.Create("a@b.com"), EmployeeCode.FromSequence(1), Guid.Empty));

    [Fact]
    public void Delete_soft_deletes_and_deactivates()
    {
        var user = Build.User();
        user.Delete();

        Assert.True(user.IsDeleted);
        Assert.NotNull(user.DeletedAtUtc);
        Assert.False(user.IsActive);
    }

    [Fact]
    public void ChangeRole_rejects_undefined_role() =>
        Assert.Throws<DomainException>(() => Build.User().ChangeRole((UserRole)42));
}

public sealed class ProjectTests
{
    [Fact]
    public void Create_normalizes_code_and_defaults_to_planned()
    {
        var project = Project.Create(" Apollo ", " apo-1 ", Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("Apollo", project.Name);
        Assert.Equal("APO-1", project.Code);
        Assert.Equal(ProjectStatus.Planned, project.Status);
    }

    [Theory]
    [InlineData("1ABC")]
    [InlineData("A")]
    [InlineData("AB_C")]
    public void Create_rejects_invalid_code(string code) =>
        Assert.Throws<DomainException>(() => Project.Create("P", code, Guid.NewGuid(), Guid.NewGuid()));

    [Fact]
    public void Create_rejects_end_before_start()
    {
        var start = DateTimeOffset.UtcNow;
        Assert.Throws<DomainException>(() =>
            Project.Create("P", "PRJ", Guid.NewGuid(), Guid.NewGuid(), startDateUtc: start, targetEndDateUtc: start.AddDays(-1)));
    }

    [Fact]
    public void Create_requires_department_and_manager()
    {
        Assert.Throws<DomainException>(() => Project.Create("P", "PRJ", Guid.Empty, Guid.NewGuid()));
        Assert.Throws<DomainException>(() => Project.Create("P", "PRJ", Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Closed_project_status_is_final()
    {
        var project = Build.Project(Guid.NewGuid());
        project.ChangeStatus(ProjectStatus.Completed);

        Assert.Throws<DomainException>(() => project.ChangeStatus(ProjectStatus.Active));
    }

    [Fact]
    public void Delete_soft_deletes_and_cancels_open_project()
    {
        var project = Build.Project(Guid.NewGuid());
        project.Delete();

        Assert.True(project.IsDeleted);
        Assert.Equal(ProjectStatus.Cancelled, project.Status);
    }

    [Fact]
    public void IsManagedBy_matches_only_manager_public_id()
    {
        var manager = Guid.NewGuid();
        var project = Build.Project(manager);

        Assert.True(project.IsManagedBy(manager));
        Assert.False(project.IsManagedBy(Guid.NewGuid()));
    }
}

public sealed class TaskItemTests
{
    private static TaskItem Assigned(TaskItemStatus status = TaskItemStatus.Todo) =>
        Build.Task(Guid.NewGuid(), Guid.NewGuid(), status);

    [Fact]
    public void Create_starts_in_todo_with_trimmed_title()
    {
        var task = TaskItem.Create("  Write tests ", Guid.NewGuid());

        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal("Write tests", task.Title);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.False(task.IsAssigned);
    }

    [Fact]
    public void Create_validates_title_and_project()
    {
        Assert.Throws<DomainException>(() => TaskItem.Create(" ", Guid.NewGuid()));
        Assert.Throws<DomainException>(() => TaskItem.Create(new string('x', TaskItem.MaxTitleLength + 1), Guid.NewGuid()));
        Assert.Throws<DomainException>(() => TaskItem.Create("T", Guid.Empty));
    }

    [Fact]
    public void Create_treats_empty_assignee_as_unassigned() =>
        Assert.Null(TaskItem.Create("T", Guid.NewGuid(), assignedUserId: Guid.Empty).AssignedUserId);

    [Theory]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Blocked)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Review)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Blocked)]
    [InlineData(TaskItemStatus.Review, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Review, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Blocked, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.Blocked, TaskItemStatus.InProgress)]
    public void Allowed_transitions_succeed(TaskItemStatus from, TaskItemStatus to)
    {
        var task = Assigned(from);
        task.ChangeStatus(to);
        Assert.Equal(to, task.Status);
    }

    [Theory]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Review)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Blocked, TaskItemStatus.Completed)]
    [InlineData(TaskItemStatus.Blocked, TaskItemStatus.Review)]
    public void Disallowed_transitions_throw(TaskItemStatus from, TaskItemStatus to) =>
        Assert.Throws<DomainException>(() => Assigned(from).ChangeStatus(to));

    [Fact]
    public void Same_status_is_a_no_op()
    {
        var task = Assigned(TaskItemStatus.Review);
        task.ChangeStatus(TaskItemStatus.Review);
        Assert.Equal(TaskItemStatus.Review, task.Status);
    }

    [Fact]
    public void Unassigned_task_cannot_start_work() =>
        Assert.Throws<DomainException>(() => Build.Task(Guid.NewGuid()).ChangeStatus(TaskItemStatus.InProgress));

    [Fact]
    public void Completing_sets_completed_at_and_leaving_clears_it()
    {
        var task = Assigned(TaskItemStatus.Review);
        task.ChangeStatus(TaskItemStatus.Completed);
        Assert.NotNull(task.CompletedAtUtc);
        Assert.False(task.IsOpen);
    }

    [Fact]
    public void Completed_task_is_locked()
    {
        var task = Assigned(TaskItemStatus.Completed);

        Assert.Throws<DomainException>(() => task.ChangeStatus(TaskItemStatus.InProgress));
        Assert.Throws<DomainException>(() => task.Update("New", null, TaskPriority.High, null));
        Assert.Throws<DomainException>(() => task.AssignTo(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => task.Unassign());
    }

    [Theory]
    [InlineData(TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Review)]
    public void Active_task_cannot_be_unassigned(TaskItemStatus status) =>
        Assert.Throws<DomainException>(() => Assigned(status).Unassign());

    [Fact]
    public void AssignTo_rejects_empty_id() =>
        Assert.Throws<DomainException>(() => Build.Task(Guid.NewGuid()).AssignTo(Guid.Empty));

    [Fact]
    public void Delete_is_soft_and_idempotent()
    {
        var task = Assigned();
        task.Delete();
        var first = task.DeletedAtUtc;
        task.Delete();

        Assert.True(task.IsDeleted);
        Assert.Equal(first, task.DeletedAtUtc);
    }
}
