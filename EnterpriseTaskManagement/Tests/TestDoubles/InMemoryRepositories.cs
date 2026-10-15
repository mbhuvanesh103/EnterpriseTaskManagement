using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Authentication;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

namespace EnterpriseTaskManagement.Tests.TestDoubles;

// Hand-written unit-test doubles. They hold only the objects a single test
// arranges; they are never used by production code and are not a database.

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _items = [];
    private readonly Func<Guid, IEnumerable<Guid>> _projectsManagedBy;

    public FakeTaskRepository(Func<Guid, IEnumerable<Guid>>? projectsManagedBy = null) =>
        _projectsManagedBy = projectsManagedBy ?? (_ => []);

    public List<TaskItem> SoftDeleted { get; } = [];

    public void Seed(params TaskItem[] tasks) => _items.AddRange(tasks);

    public Task<TaskItem?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(t => t.PublicId == publicId && !t.IsDeleted));

    public Task<IReadOnlyList<TaskItem>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(_items.Where(t => !t.IsDeleted).ToList());

    public Task<IReadOnlyList<TaskItem>> ListByProjectAsync(Guid projectPublicId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(_items.Where(t => !t.IsDeleted && t.ProjectId == projectPublicId).ToList());

    public Task<IReadOnlyList<TaskItem>> ListByAssigneeAsync(Guid assignedUserPublicId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(_items.Where(t => !t.IsDeleted && t.AssignedUserId == assignedUserPublicId).ToList());

    public Task<IReadOnlyList<TaskItem>> ListByProjectManagerAsync(Guid managerPublicId, CancellationToken cancellationToken = default)
    {
        var projects = _projectsManagedBy(managerPublicId).ToHashSet();
        return Task.FromResult<IReadOnlyList<TaskItem>>(_items.Where(t => !t.IsDeleted && projects.Contains(t.ProjectId)).ToList());
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _items.Add(task);
        return Task.CompletedTask;
    }

    public void Update(TaskItem task)
    {
    }

    public void SoftDelete(TaskItem task, string? deletedBy = null) => SoftDeleted.Add(task);
}

internal sealed class FakeProjectRepository : IProjectRepository
{
    private readonly List<Project> _items = [];

    public void Seed(params Project[] projects) => _items.AddRange(projects);

    public List<Project> SoftDeleted { get; } = [];

    public IEnumerable<Guid> ManagedBy(Guid manager) =>
        _items.Where(p => p.ManagerId == manager).Select(p => p.PublicId);

    public Task<Project?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(p => p.PublicId == publicId && !p.IsDeleted));

    public Task<bool> CodeExistsAsync(string code, Guid? excludePublicId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(p => !p.IsDeleted && p.Code == code && p.PublicId != excludePublicId));

    public Task<IReadOnlyList<Project>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Project>>(_items.Where(p => !p.IsDeleted).ToList());

    public Task<IReadOnlyList<Project>> ListByDepartmentAsync(Guid departmentPublicId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Project>>(_items.Where(p => !p.IsDeleted && p.DepartmentId == departmentPublicId).ToList());

    public Task<IReadOnlyList<Project>> ListByManagerAsync(Guid managerPublicId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Project>>(_items.Where(p => !p.IsDeleted && p.ManagerId == managerPublicId).ToList());

    public Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        _items.Add(project);
        return Task.CompletedTask;
    }

    public void Update(Project project)
    {
    }

    public void SoftDelete(Project project, string? deletedBy = null) => SoftDeleted.Add(project);
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _items = [];

    public void Seed(params User[] users) => _items.AddRange(users);

    public List<User> Added { get; } = [];

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(u => u.PublicId == publicId && !u.IsDeleted));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByEmployeeCodeAsync(EmployeeCode employeeCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(u => u.EmployeeCode == employeeCode));

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(u => u.Email == email));

    public Task<bool> EmployeeCodeExistsAsync(EmployeeCode employeeCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(u => u.EmployeeCode == employeeCode));

    public Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_items.ToList());

    public Task<IReadOnlyList<User>> ListByDepartmentAsync(Guid departmentPublicId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_items.Where(u => u.DepartmentId == departmentPublicId).ToList());

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _items.Add(user);
        Added.Add(user);
        return Task.CompletedTask;
    }

    public void Update(User user)
    {
    }

    public void SoftDelete(User user, string? deletedBy = null)
    {
    }
}

internal sealed class FakeDepartmentRepository : IDepartmentRepository
{
    private readonly List<Department> _items = [];

    public void Seed(params Department[] departments) => _items.AddRange(departments);

    public Task<Department?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(d => d.PublicId == publicId && !d.IsDeleted));

    public Task<bool> CodeExistsAsync(string code, Guid? excludePublicId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Any(d => d.Code == code && d.PublicId != excludePublicId));

    public Task<IReadOnlyList<Department>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Department>>(_items.ToList());

    public Task AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        _items.Add(department);
        return Task.CompletedTask;
    }

    public void Update(Department department)
    {
    }

    public void SoftDelete(Department department, string? deletedBy = null)
    {
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string password, string passwordHash) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenService : IJwtTokenService
{
    public AccessToken CreateAccessToken(User user) =>
        new("token-for-" + user.PublicId.ToString("N"), DateTimeOffset.UtcNow.AddHours(1));
}

internal static class Build
{
    public static User User(UserRole role = UserRole.Employee, bool active = true, string? passwordHash = null, int seq = 1) =>
        global::EnterpriseTaskManagement.Domain.Entities.Users.User.Rehydrate(
            seq,
            Guid.NewGuid(),
            FullName.Create("Test", "Person"),
            Email.Create($"person{seq}@example.com"),
            EmployeeCode.FromSequence(seq),
            Guid.NewGuid(),
            active,
            role,
            passwordHash);

    public static Project Project(Guid managerId, string code = "PRJ-1") =>
        global::EnterpriseTaskManagement.Domain.Entities.Projects.Project.Create("Project", code, Guid.NewGuid(), managerId);

    public static TaskItem Task(Guid projectId, Guid? assignee = null, TaskItemStatus status = TaskItemStatus.Todo) =>
        TaskItem.Rehydrate(1, Guid.NewGuid(), "Task", null, projectId, assignee, TaskPriority.Medium, status, null, null);
}
