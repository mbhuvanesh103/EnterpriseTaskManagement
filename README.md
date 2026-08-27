# Enterprise Task Management System

Full-stack architecture skeleton:

- **Frontend** — React 19 + TypeScript + Vite, Tailwind, shadcn/ui conventions, Axios,
  React Hook Form, Zod, TanStack Query. It talks to a real ASP.NET Core REST API over HTTP.
- **Backend** — `EnterpriseTaskManagement/` .NET 10 solution following Clean Architecture
  (Domain / Application / Infrastructure / API), DDD, CQRS with MediatR, EF Core 10 + SQL Server,
  JWT + refresh tokens, FluentValidation, Swagger/OpenAPI.

No mock endpoints, no seeded/fake data, no Supabase/Firebase/Node/Mongo backend.

> **Routing note:** this workspace runs TanStack Start, whose router is fixed to
> TanStack Router. The requested React Router role (route shell + file-based routes)
> is fulfilled with `@tanstack/react-router`; `react-router-dom` cannot be installed here.

---

## Frontend

| Path | Purpose |
| --- | --- |
| `.env.example` | Template for `VITE_API_BASE_URL` / `VITE_API_TIMEOUT_MS`. Copy to `.env`. |
| `src/config/env.ts` | Single typed accessor for frontend environment config and token storage keys. |
| `src/lib/api/client.ts` | Centralized Axios instance: base URL, timeout, JWT bearer request interceptor, `ApiError` normalization, `tokenStorage`, refresh-token extension point. |
| `src/lib/api/endpoints.ts` | Registry of API route paths mirroring the ASP.NET Core controllers. |
| `src/lib/api/types.ts` | Shared transport contracts (`PagedRequest`, `PagedResult<T>`). |
| `src/lib/query-client.ts` | `createQueryClient()` defaults (stale time, retry policy that skips 4xx) and the `queryKeys` registry. |
| `src/routes/__root.tsx` | App shell: head metadata, `QueryClientProvider`, error/not-found boundaries, `<Outlet />`. |
| `src/routes/index.tsx` | Initial architecture-oriented page (no feature workflows, no fake data). |
| `src/components/ui/*` | shadcn/ui primitives. |

### Run the frontend

```sh
cp .env.example .env    # point VITE_API_BASE_URL at your local API
npm install
npm run dev
```

### Frontend → API flow

```text
component / route
  → TanStack Query hook (queryKeys + queryFn)
    → feature api function (endpoints.*)
      → apiClient (Axios: base URL + Bearer token)
        → HTTPS → ASP.NET Core API controller
          → MediatR command/query → Application handler
            → repository → EF Core → SQL Server
```

Errors bubble back as `ApiError` (status + field errors from RFC 7807 problem details);
`401` clears stored tokens so the shell can route to sign-in.

---

## Backend solution layout

```text
EnterpriseTaskManagement/
  EnterpriseTaskManagement.sln                 # Domain + Application + Infrastructure + API
  Domain/                                      # implemented — net10.0, zero external dependencies
    Common/
      BaseEntity.cs                            # identity-based equality, transient detection
      AuditableEntity.cs                       # audit + soft delete + byte[] Version concurrency token
      AggregateRoot.cs                         # consistency boundary + domain event collection
      ValueObject.cs                           # structural equality base
      IDomainEvent.cs                          # framework-free domain event marker
    ValueObjects/
      FullName.cs                              # first + last required, max 100 chars total
      Email.cs                                 # required, validated, lowercased
      EmployeeCode.cs                          # EMP + exactly 6 digits (EMP000001)
    Entities/Users/User.cs                     # User aggregate root (required DepartmentId)
    Entities/Departments/Department.cs         # Department aggregate (PublicId, Name, Code, IsActive)
    Events/Users/                              # UserRegistered, UserEmailChanged, UserDeleted
    Exceptions/DomainException.cs              # invariant violation signal
  Application/                                 # implemented — depends on Domain only, no EF Core
    DependencyInjection.cs                     # AddApplication(): MediatR + FluentValidation + pipeline
    Common/Interfaces/IUnitOfWork.cs           # SaveChangesAsync contract
    Common/Exceptions/NotFoundException.cs     # missing aggregate → HTTP 404 at the API edge
    Common/Exceptions/ValidationException.cs   # aggregated field errors → HTTP 400
    Common/Exceptions/ConflictException.cs     # duplicate email / employee code → HTTP 409

    Common/Behaviors/ValidationBehavior.cs     # MediatR pipeline: validators run before handlers
    Common/Models/UserSummary.cs               # shared user read model (DTO)
    Interfaces/Persistence/IUserRepository.cs  # persistence contract for the User aggregate
    Features/Users/Commands/CreateUser/        # Command, Handler, Response, Validator
    Features/Users/Commands/UpdateUser/        # Command, Handler, Response, Validator
    Features/Users/Commands/DeleteUser/        # Command, Handler, Response, Validator (soft delete)
    Features/Users/Queries/GetAllUsers/        # Query, Handler, Response
    Features/Users/Queries/GetUserByPublicId/  # Query, Handler, Response
  Infrastructure/                              # implemented — EF Core 10 + SQL Server
    EnterpriseTaskManagement.Infrastructure.csproj  # EF Core 10 (SqlServer/Relational/Design) + DI/Config
    DependencyInjection.cs                     # AddInfrastructure(configuration): DbContext + repositories
    Persistence/ApplicationDbContext.cs        # DbSet<User>, DbSet<Department>, audit stamping, IUnitOfWork
    Persistence/Configurations/UserConfiguration.cs
    Persistence/Configurations/DepartmentConfiguration.cs
    Persistence/Repositories/UserRepository.cs # IUserRepository over ApplicationDbContext
    Persistence/Migrations/README.md           # placeholder + dotnet ef commands (no migrations generated)
  API/                                         # implemented — controllers + Swagger (no JWT yet)
    EnterpriseTaskManagement.API.csproj        # Microsoft.NET.Sdk.Web + Swashbuckle, XML docs on
    Program.cs                                 # composition root + middleware order
    Controllers/UsersController.cs             # 5 endpoints, depends only on MediatR ISender
    Contracts/Users/UpdateUserRequest.cs       # PUT body without PublicId (route is the only source)
    Middleware/GlobalExceptionHandler.cs       # IExceptionHandler → RFC 7807 ProblemDetails
    appsettings.json                           # empty ConnectionStrings:SqlServer placeholder
    appsettings.Development.json               # local overrides (no credentials committed)
    Properties/launchSettings.json             # Development profiles, launches /swagger
```

Dependency direction is enforced by project references: `Domain` references nothing,
`Application → Domain`, `Infrastructure → Application + Domain`, and
`API → Application + Infrastructure` (for DI registration only — the API has no EF Core
package reference and never touches `ApplicationDbContext`).


> **Environment note:** this workspace has no .NET SDK installed, so the backend was not
> compiled or migrated here. The code is written as a structurally valid .NET 10 solution —
> run `dotnet build` and the `dotnet ef` commands from `EnterpriseTaskManagement/` locally.

### How the Domain pieces connect

- **`BaseEntity<TId>`** gives every entity an identity and identity equality — two `User`
  instances are the same user when their `Id` matches, regardless of field values.
- **`AuditableEntity<TId>`** extends it with audit state, soft-delete state and the
  `Version` concurrency token, mutated only via `MarkCreated` / `MarkModified` /
  `MarkDeleted` / `MarkRestored`. `Version` is a plain `byte[]`, so no EF type leaks in.
- **`AggregateRoot<TId>`** extends the auditable entity and marks the transactional
  boundary. Repositories load and save aggregate roots only, and roots collect
  `IDomainEvent`s (`DomainEvents`, `ClearDomainEvents`) that Infrastructure drains after commit.
- **`User : AggregateRoot<long>`** is the entity/aggregate for a person: internal `Id` for
  persistence, `PublicId` (Guid) for the API surface, a mandatory `DepartmentId`, and
  behaviour instead of setters — `Register`, `Rehydrate`, `Rename`, `ChangeEmail`,
  `TransferToDepartment`, `Activate`, `Deactivate`.
- **`Department : AggregateRoot<long>`** exists only to satisfy that required relationship;
  users point at `Department.PublicId`.
- **Value objects** (`FullName`, `Email`, `EmployeeCode`) derive from `ValueObject`, are
  immutable, compare structurally (`Email.Create("A@X.com") == Email.Create("a@x.com")`), and
  own their own validation/normalization. `User` therefore cannot hold an invalid name,
  email or employee code — invalid input throws `DomainException` at construction time,
  before any Application or API code runs.
- **Validation placement:** format/shape rules live in the value objects, cross-field
  aggregate rules in `User`, and only cross-aggregate rules (e.g. email uniqueness) belong
  to Application handlers with FluentValidation.

### Infrastructure and persistence (Step 4)

| File | Purpose |
| --- | --- |
| `Infrastructure/EnterpriseTaskManagement.Infrastructure.csproj` | EF Core 10 packages (`Microsoft.EntityFrameworkCore`, `.Relational`, `.SqlServer`, `.Design`) plus configuration/DI abstractions; project references to Application and Domain. |
| `Persistence/ApplicationDbContext.cs` | `DbSet<User>` and `DbSet<Department>`, default schema `etm`, `ApplyConfigurationsFromAssembly` for all `IEntityTypeConfiguration<T>`, audit stamping in `SaveChangesAsync`, converts a hard `Remove` into a soft delete, and implements `IUnitOfWork`. |
| `Persistence/Configurations/UserConfiguration.cs` | Table/keys; `PublicId` stored as a 32-char `"N"` string (`HasMaxLength(32)`, unique); `FullName` mapped as an owned value object (FirstName/MiddleName/LastName/FullName columns); `Email` and `EmployeeCode` mapped through value converters onto their `Create` factories with unique indexes; required FK `DepartmentId → Department.PublicId` (`OnDelete(Restrict)`, `IsRequired`); audit columns; `IsDeleted` + `HasQueryFilter(u => !u.IsDeleted)`; `Version` as `IsRowVersion()`; `DomainEvents` ignored. |
| `Persistence/Configurations/DepartmentConfiguration.cs` | Same pattern for departments; `PublicId` is an alternate key (32-char) so users can reference it, unique `Code`, audit columns, soft-delete filter, rowversion. |
| `Persistence/Repositories/UserRepository.cs` | `IUserRepository` implemented with `ApplicationDbContext` (lookups by id/public id/email/employee code, uniqueness checks, list by department, add/update/soft delete). Returns domain objects and `IReadOnlyList<T>` — never `IQueryable`. |
| `Persistence/Migrations/README.md` | Placeholder for the not-yet-generated migrations plus the exact `dotnet ef` commands. |
| `DependencyInjection.cs` | `AddInfrastructure(IConfiguration)`: reads `ConnectionStrings:SqlServer` (throws a clear error when absent), registers `AddDbContext<ApplicationDbContext>(UseSqlServer(...))` with migrations assembly/history table and retry-on-failure, then `IUnitOfWork` → `ApplicationDbContext` and `IUserRepository` → `UserRepository`, both scoped. |
| `Application/Common/Interfaces/IUserRepository.cs`, `IUnitOfWork.cs` | The abstractions Application depends on; Application has no EF Core reference at all. |
| `Domain/Common/AuditableEntity.cs` (changed) | Added the framework-free `byte[]? Version` concurrency token. |
| `Domain/Entities/Users/User.cs` (changed) | `DepartmentId` is now a required `Guid`; `AssignToDepartment`/`RemoveFromDepartment` replaced by `TransferToDepartment`. |
| `Domain/Entities/Departments/Department.cs` (new) | Minimal aggregate backing the required relationship. |

**Wiring, end to end:**

```text
API startup  →  services.AddInfrastructure(builder.Configuration)
                  ├─ reads ConnectionStrings:SqlServer (appsettings / user-secrets /
                  │  ConnectionStrings__SqlServer env var — never hard-coded)
                  ├─ AddDbContext<ApplicationDbContext>(UseSqlServer(connectionString))
                  ├─ IUnitOfWork        → ApplicationDbContext
                  └─ IUserRepository    → UserRepository

Application handler depends on IUserRepository + IUnitOfWork (abstractions only)
   → UserRepository (Infrastructure) queries ApplicationDbContext.Users
     → ApplicationDbContext model built from UserConfiguration / DepartmentConfiguration
       (value-object mapping, soft-delete filter, rowversion, required FK)
       → EF Core 10 SQL Server provider → SQL Server database
   → IUnitOfWork.SaveChangesAsync() stamps audit values and commits
```

Configure the connection string without committing credentials:

```sh
cd EnterpriseTaskManagement/API
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=EnterpriseTaskManagement;Trusted_Connection=True;TrustServerCertificate=True"
# or: export ConnectionStrings__SqlServer="..."
```


### Domain rules enforced

| Value object | Rule |
| --- | --- |
| `FullName` | First and last name required; represented value ("First [Middle] Last") ≤ 100 chars; whitespace collapsed. `Create(first, last, middle?)` and `Parse("Ada Byron Lovelace")`. |
| `Email` | Required; syntactically valid; trimmed and lowercased; ≤ 256 chars. |
| `EmployeeCode` | Exactly `EMP` + 6 digits (`EMP000001`); trimmed/upper-cased; `FromSequence(1)` builds `EMP000001`. |

---

## Local prerequisites

- Node.js 20+ (frontend)
- .NET 10 SDK (backend)
- SQL Server 2019+ / SQL Server Express / Azure SQL, plus `dotnet-ef` for migrations

```sh
dotnet --version
dotnet tool install --global dotnet-ef
```

## Backend configuration (added with the API project)

Connection string and JWT values are supplied through `appsettings.json` /
`appsettings.Development.json` and are overridden locally with user secrets — never commit
real credentials.

```jsonc
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=EnterpriseTaskManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "EnterpriseTaskManagement.API",
    "Audience": "EnterpriseTaskManagement.Client",
    "Key": "<32+ char secret — set via user secrets>",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

```sh
cd EnterpriseTaskManagement/API
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=EnterpriseTaskManagement;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<a long random secret>"
```

## Build

```sh
cd EnterpriseTaskManagement
dotnet build
```

---

## Application layer (Step 5) — use cases only

Every file created or changed in this step:

| File | Purpose |
| --- | --- |
| `Application/EnterpriseTaskManagement.Application.csproj` (changed) | Added `MediatR`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `Microsoft.Extensions.DependencyInjection.Abstractions`. Still references **Domain only** — no EF Core, no ASP.NET Core, no provider. |
| `Application/DependencyInjection.cs` (new) | `AddApplication()`: registers MediatR handlers from this assembly, all FluentValidation validators, and `ValidationBehavior<,>` as an open pipeline behavior. |
| `Application/Common/Exceptions/NotFoundException.cs` (new) | Robust not-found signal with `EntityName`/`Key` and the `NotFoundException.For("User", publicId, "public id")` factory. API maps it to 404. |
| `Application/Common/Exceptions/ValidationException.cs` (new) | Groups FluentValidation failures into `IDictionary<string, string[]> Errors` for RFC 7807 problem details. |
| `Application/Common/Behaviors/ValidationBehavior.cs` (new) | Runs validators before the handler; throws `ValidationException` on failure. |
| `Application/Common/Models/UserSummary.cs` (new) | Shared query DTO (`PublicId`, `FullName`, `Email`, `EmployeeCode`, `IsActive`) with `FromUser(User)` projection, so value objects never leave the layer. |
| `Application/Interfaces/Persistence/IUserRepository.cs` (moved from `Common/Interfaces/`) | Repository abstraction; namespace is now `EnterpriseTaskManagement.Application.Interfaces.Persistence`. Added `ListAllAsync`. Returns domain objects / `IReadOnlyList<T>` — never `IQueryable`. |
| `Application/Common/Interfaces/IUnitOfWork.cs` (unchanged) | `SaveChangesAsync` contract implemented by `ApplicationDbContext`. |
| `Features/Users/Commands/CreateUser/{CreateUserCommand,CreateUserCommandHandler,CreateUserResponse,CreateUserValidator}.cs` | Inputs `firstName,lastName,email,employeeCode,departmentId`; handler builds `FullName` → `Email` → `EmployeeCode`, checks email / employee-code uniqueness via the repository's real `EmailExistsAsync` / `EmployeeCodeExistsAsync`, calls `User.Create`, `AddAsync`, `SaveChangesAsync`. |
| `Features/Users/Commands/UpdateUser/{UpdateUserCommand,UpdateUserCommandHandler,UpdateUserResponse,UpdateUserValidator}.cs` | Inputs `publicId,firstName,lastName,email,departmentId`; loads by `PublicId` or throws `NotFoundException`, builds value objects, calls `ChangeName()`, `ChangeEmail()`, `TransferDepartment()`, then `Update` + `SaveChangesAsync`. |
| `Features/Users/Commands/DeleteUser/{DeleteUserCommand,DeleteUserCommandHandler,DeleteUserResponse,DeleteUserValidator}.cs` | Input `publicId`; loads or throws `NotFoundException`, calls the new domain method `User.Delete()` (audit soft-delete state + deactivate + `UserDeletedDomainEvent`), then persists. No physical delete. |
| `Features/Users/Queries/GetAllUsers/{GetAllUsersQuery,GetAllUsersQueryHandler,GetAllUsersResponse}.cs` | Returns `UserSummary` list + count; optional department filter. Soft-deleted users are excluded by the EF query filter. |
| `Features/Users/Queries/GetUserByPublicId/{GetUserByPublicIdQuery,GetUserByPublicIdQueryHandler,GetUserByPublicIdResponse}.cs` | Returns `PublicId`, `FullName`, `Email`, `EmployeeCode`, `IsActive`; throws `NotFoundException` when missing. |
| `Domain/Entities/Users/User.cs` (changed) | Added use-case-named behaviour: `Create` (alias of `Register`), `ChangeName` (alias of `Rename`), `TransferDepartment` (alias of `TransferToDepartment`) and `Delete(deletedBy?)` which soft-deletes, deactivates and raises the deleted event. Still framework-free. |
| `Domain/Events/Users/UserDeletedDomainEvent.cs` (new) | `UserDeletedDomainEvent(UserPublicId, Email, OccurredOnUtc)`. |
| `Infrastructure/Persistence/Repositories/UserRepository.cs` (changed) | Implements the relocated `IUserRepository`, adds `ListAllAsync`, and `SoftDelete` is now idempotent (the aggregate already marked itself deleted). |
| `Infrastructure/DependencyInjection.cs` (changed) | Uses the new interface namespace; registration unchanged (`IUserRepository → UserRepository`, `IUnitOfWork → ApplicationDbContext`). |
| `EnterpriseTaskManagement.sln` (added) | Solution containing Domain, Application and Infrastructure. |

### Validation split

Validators check **input shape only**: required strings and a supplied department.
`DepartmentId` is a `Guid` in the Domain (users reference `Department.PublicId`), so the
requested "department > 0" rule is expressed as `NotEmpty()` — a non-empty identifier.
Email syntax, the 100-char full-name limit and the `EMP######` pattern are **not**
duplicated: they live in `Email`, `FullName` and `EmployeeCode` and throw `DomainException`.

### Create User — end to end

```text
POST /api/users            (API controller — intended, not implemented yet)
  → new CreateUserCommand(firstName, lastName, email, employeeCode, departmentId)
  → ISender.Send(command)                              MediatR
    → ValidationBehavior → CreateUserValidator          required fields present?
    → CreateUserCommandHandler
        FullName.Create / Email.Create / EmployeeCode.Create   Domain validation + normalization
        IUserRepository.EmailExistsAsync / EmployeeCodeExistsAsync   uniqueness policy
        User.Create(...)                                aggregate + UserRegisteredDomainEvent
        IUserRepository.AddAsync(user)
        IUnitOfWork.SaveChangesAsync()
      → UserRepository (Infrastructure) → ApplicationDbContext.Users.Add
        → UserConfiguration mapping (owned FullName, Email/EmployeeCode converters,
          PublicId "N" string, audit columns, rowversion)
          → EF Core 10 SQL Server provider → INSERT etm.Users
  → CreateUserResponse (PublicId, FullName, Email, EmployeeCode, DepartmentId, IsActive)
    → 201 Created
```

### Update User — end to end

```text
PUT /api/users/{publicId}  (API controller — intended, not implemented yet)
  → new UpdateUserCommand(publicId, firstName, lastName, email, departmentId)
  → ISender.Send(command)
    → ValidationBehavior → UpdateUserValidator
    → UpdateUserCommandHandler
        IUserRepository.GetByPublicIdAsync(publicId)
          null → NotFoundException  → 404 problem details
        FullName.Create / Email.Create                   Domain rules
        email changed & already taken → DomainException  → 400/422
        user.ChangeName() / ChangeEmail() (raises UserEmailChangedDomainEvent) /
        TransferDepartment()                             invariants stay in the aggregate
        IUserRepository.Update(user) + IUnitOfWork.SaveChangesAsync()
      → ApplicationDbContext stamps ModifiedAtUtc/ModifiedBy, checks the rowversion
        → UPDATE etm.Users WHERE PublicId = ... AND Version = ...
  → UpdateUserResponse → 200 OK
```

Handlers depend on `IUserRepository` and `IUnitOfWork` only — the Application project
cannot even see `ApplicationDbContext`.

### Build status

This workspace has **no .NET SDK installed** (`dotnet` is unavailable), so `dotnet build`
was not run here. The Application layer is written as structurally valid .NET 10 C#;
compile it locally with:

```sh
cd EnterpriseTaskManagement
dotnet build
```

API composition root wiring, once the API project exists:

```csharp
builder.Services.AddApplication();                     // MediatR + FluentValidation
builder.Services.AddInfrastructure(builder.Configuration);  // EF Core 10 + SQL Server
```
