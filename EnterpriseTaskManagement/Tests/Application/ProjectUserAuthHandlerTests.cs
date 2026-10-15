using EnterpriseTaskManagement.Application.Common.Behaviors;
using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Features.Auth.Commands.Login;
using EnterpriseTaskManagement.Application.Features.Projects.Commands.CreateProject;
using EnterpriseTaskManagement.Application.Features.Projects.Commands.DeleteProject;
using EnterpriseTaskManagement.Application.Features.Projects.Commands.UpdateProject;
using EnterpriseTaskManagement.Application.Features.Tasks.Commands.CreateTask;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Tests.TestDoubles;

using FluentValidation;

namespace EnterpriseTaskManagement.Tests.Application;

public sealed class ProjectHandlerTests
{
    private readonly FakeProjectRepository _projects = new();
    private readonly FakeDepartmentRepository _departments = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly Department _department = Department.Create("Engineering", "ENG");
    private readonly User _manager = Build.User(UserRole.Manager, seq: 10);
    private readonly User _otherManager = Build.User(UserRole.Manager, seq: 11);
    private readonly User _employee = Build.User(UserRole.Employee, seq: 12);

    public ProjectHandlerTests()
    {
        _departments.Seed(_department);
        _users.Seed(_manager, _otherManager, _employee);
    }

    private CreateProjectCommandHandler Create() => new(_projects, _departments, _users, _uow);

    private UpdateProjectCommand Update(Project p, Guid requester, bool admin, Guid? managerId = null) =>
        new(p.PublicId, "Renamed", p.Code, _department.PublicId, managerId ?? p.ManagerId, requester, admin);

    [Fact]
    public async Task Create_rejects_employee_as_manager() =>
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Create().Handle(
            new CreateProjectCommand("P", "PRJ", _department.PublicId, _employee.PublicId), default));

    [Fact]
    public async Task Create_rejects_duplicate_code()
    {
        await Create().Handle(new CreateProjectCommand("P", "DUP", _department.PublicId, _manager.PublicId), default);
        await Assert.ThrowsAsync<ConflictException>(() => Create().Handle(
            new CreateProjectCommand("Q", "dup", _department.PublicId, _manager.PublicId), default));
    }

    [Fact]
    public async Task Create_rejects_unknown_department_and_bad_status()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Create().Handle(
            new CreateProjectCommand("P", "PRJ", Guid.NewGuid(), _manager.PublicId), default));
        await Assert.ThrowsAsync<ArgumentException>(() => Create().Handle(
            new CreateProjectCommand("P", "PRJ", _department.PublicId, _manager.PublicId, Status: "Paused"), default));
    }

    [Fact]
    public async Task Create_returns_public_ids_only()
    {
        var response = await Create().Handle(
            new CreateProjectCommand("P", "PRJ", _department.PublicId, _manager.PublicId), default);

        Assert.Equal(_department.PublicId, response.DepartmentId);
        Assert.Equal(_manager.PublicId, response.ManagerId);
        Assert.NotEqual(Guid.Empty, response.PublicId);
    }

    [Fact]
    public async Task Manager_may_update_own_project_but_not_foreign_one()
    {
        var own = Build.Project(_manager.PublicId, "OWN");
        var foreign = Build.Project(_otherManager.PublicId, "FOREIGN");
        _projects.Seed(own, foreign);
        var handler = new UpdateProjectCommandHandler(_projects, _departments, _users, _uow);

        var updated = await handler.Handle(Update(own, _manager.PublicId, admin: false), default);
        Assert.Equal("Renamed", updated.Name);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(Update(foreign, _manager.PublicId, admin: false), default));
    }

    [Fact]
    public async Task Only_admin_may_reassign_project_manager()
    {
        var own = Build.Project(_manager.PublicId, "OWN");
        _projects.Seed(own);
        var handler = new UpdateProjectCommandHandler(_projects, _departments, _users, _uow);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(Update(own, _manager.PublicId, admin: false, _otherManager.PublicId), default));

        var result = await handler.Handle(Update(own, Guid.NewGuid(), admin: true, _otherManager.PublicId), default);
        Assert.Equal(_otherManager.PublicId, result.ManagerId);
    }

    [Fact]
    public async Task Delete_is_soft_and_ownership_checked()
    {
        var own = Build.Project(_manager.PublicId, "OWN");
        _projects.Seed(own);
        var handler = new DeleteProjectCommandHandler(_projects, _uow);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new DeleteProjectCommand(own.PublicId, _otherManager.PublicId, false), default));

        var result = await handler.Handle(new DeleteProjectCommand(own.PublicId, _manager.PublicId, false), default);
        Assert.True(result.IsDeleted);
        Assert.Contains(own, _projects.SoftDeleted);
    }
}

public sealed class UserAndLoginHandlerTests
{
    [Fact]
    public async Task CreateUser_rejects_duplicate_email_and_code()
    {
        var users = new FakeUserRepository();
        var handler = new CreateUserCommandHandler(users, new FakeUnitOfWork());
        var dept = Guid.NewGuid();

        var created = await handler.Handle(new CreateUserCommand("A", "B", "a@b.com", "EMP000001", dept), default);
        Assert.True(created.IsActive);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateUserCommand("C", "D", "A@B.com", "EMP000002", dept), default));
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateUserCommand("C", "D", "c@d.com", "emp000001", dept), default));
    }

    private static LoginCommandHandler Login(params User[] users)
    {
        var repo = new FakeUserRepository();
        repo.Seed(users);
        return new LoginCommandHandler(repo, new FakePasswordHasher(), new FakeTokenService());
    }

    [Fact]
    public async Task Login_succeeds_and_returns_role_and_public_id()
    {
        var user = Build.User(UserRole.Manager, passwordHash: "hashed:pw");

        var response = await Login(user).Handle(new LoginCommand(user.Email.Value, "pw"), default);

        Assert.Equal(user.PublicId, response.UserPublicId);
        Assert.Equal("Manager", response.Role);
    }

    [Theory]
    [InlineData("not-an-email", "pw")]
    [InlineData("missing@example.com", "pw")]
    [InlineData("person1@example.com", "wrong")]
    public async Task Login_failures_are_indistinguishable(string email, string password)
    {
        var user = Build.User(passwordHash: "hashed:pw");

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            Login(user).Handle(new LoginCommand(email, password), default));
        Assert.Equal(AuthenticationFailedException.GenericMessage, ex.Message);
    }

    [Fact]
    public async Task Login_rejects_inactive_deleted_or_passwordless_accounts()
    {
        var inactive = Build.User(active: false, passwordHash: "hashed:pw", seq: 1);
        var deleted = Build.User(passwordHash: "hashed:pw", seq: 2);
        deleted.Delete();
        var noPassword = Build.User(seq: 3);

        foreach (var user in new[] { inactive, deleted, noPassword })
        {
            await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
                Login(user).Handle(new LoginCommand(user.Email.Value, "pw"), default));
        }
    }
}

public sealed class ValidatorTests
{
    [Fact]
    public void CreateUserValidator_requires_all_fields()
    {
        var result = new CreateUserValidator().Validate(new CreateUserCommand("", "", "", "", Guid.Empty));

        Assert.Equal(
            new[] { "DepartmentId", "Email", "EmployeeCode", "FirstName", "LastName" },
            result.Errors.Select(e => e.PropertyName).Distinct().Order());
    }

    [Fact]
    public void CreateTaskValidator_rejects_bad_priority_and_empty_ids()
    {
        var result = new CreateTaskValidator().Validate(new CreateTaskCommand(
            "", Guid.Empty, Guid.Empty, TaskRequesterRole.Admin, AssignedUserId: Guid.Empty, Priority: "Urgent"));

        var props = result.Errors.Select(e => e.PropertyName).ToHashSet();
        Assert.Superset(new HashSet<string> { "Title", "ProjectId", "RequesterPublicId", "AssignedUserId", "Priority" }, props);
    }

    [Fact]
    public void CreateTaskValidator_accepts_valid_command() =>
        Assert.True(new CreateTaskValidator().Validate(new CreateTaskCommand(
            "Title", Guid.NewGuid(), Guid.NewGuid(), TaskRequesterRole.Manager, Priority: "high")).IsValid);

    [Fact]
    public async Task ValidationBehavior_throws_before_handler_runs()
    {
        var behavior = new ValidationBehavior<CreateUserCommand, CreateUserResponse>(
            new IValidator<CreateUserCommand>[] { new CreateUserValidator() });
        var called = false;

        var ex = await Assert.ThrowsAsync<EnterpriseTaskManagement.Application.Common.Exceptions.ValidationException>(() =>
            behavior.Handle(new CreateUserCommand("", "B", "a@b.com", "EMP000001", Guid.NewGuid()), _ =>
            {
                called = true;
                return Task.FromResult<CreateUserResponse>(null!);
            }, default));

        Assert.False(called);
        Assert.True(ex.Errors.ContainsKey("FirstName"));
    }
}
