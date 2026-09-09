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

---

## API layer (Step 6) — controllers + Swagger

| File | Purpose |
| --- | --- |
| `API/EnterpriseTaskManagement.API.csproj` | ASP.NET Core Web API (`net10.0`), `Swashbuckle.AspNetCore`, XML doc generation for Swagger summaries. References Application + Infrastructure only. |
| `API/Program.cs` | Composition root: `AddControllers()`, `AddApplication()`, `AddInfrastructure(configuration)`, `AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()`, `AddEndpointsApiExplorer()` + `AddSwaggerGen()` (configured exactly once). Middleware order: `UseExceptionHandler()` → Swagger (Development only) → `UseHttpsRedirection()` → `MapControllers()`. |
| `API/Controllers/UsersController.cs` | The five User endpoints. Injects only `ISender`; no DbContext, repository, EF Core type, validation or business logic. |
| `API/Contracts/Users/UpdateUserRequest.cs` | PUT body that deliberately has **no** `PublicId`, so the identity can only come from the route — a mismatched body id is impossible instead of merely rejected. |
| `API/Middleware/GlobalExceptionHandler.cs` | `IExceptionHandler` that converts exceptions into RFC 7807 `ProblemDetails`. |
| `API/appsettings*.json` | `ConnectionStrings:SqlServer` placeholder (empty — set it via user secrets or `ConnectionStrings__SqlServer`). No credentials, no JWT values committed. |
| `API/Properties/launchSettings.json` | Development profiles with `ASPNETCORE_ENVIRONMENT=Development` and `launchUrl: swagger`. |
| `Application/Common/Exceptions/ConflictException.cs` | New: duplicate email / employee code detected by the existing Create/Update handlers, mapped to 409. Previously these threw `DomainException` (400), which conflated "conflict" with "invalid input". |

### Endpoints

| Method & route | MediatR message | Success | Errors |
| --- | --- | --- | --- |
| `POST /api/Users` | `CreateUserCommand` | `201 Created` + `Location` via `CreatedAtAction(nameof(GetByPublicId))` | 400 validation/invariant, 409 duplicate email or employee code |
| `GET /api/Users?departmentId=` | `GetAllUsersQuery` | `200 OK` (`GetAllUsersResponse`) | — |
| `GET /api/Users/{publicId}` | `GetUserByPublicIdQuery` | `200 OK` | 404 from the global handler |
| `PUT /api/Users/{publicId}` | `UpdateUserCommand` (publicId from route) | `200 OK` | 400, 404, 409 |
| `DELETE /api/Users/{publicId}` | `DeleteUserCommand` (soft delete only) | `204 No Content` | 400, 404 |

`DELETE` never removes a row: the handler calls `User.Delete()` (audit deletion state +
deactivation + `UserDeletedDomainEvent`) and `IUserRepository.SoftDelete`, and the
`IsDeleted` query filter hides the row from later reads.

### Exception → status mapping

| Exception | Status | Title |
| --- | --- | --- |
| `Application.Common.Exceptions.NotFoundException` | 404 | Resource not found |
| `Application.Common.Exceptions.ConflictException` | 409 | Conflict |
| `Application.Common.Exceptions.ValidationException` | 400 | Validation failed (+ `errors` per field) |
| `FluentValidation.ValidationException` | 400 | Validation failed (+ `errors` per field) |
| `Domain.Exceptions.DomainException` | 400 | Business rule violation |
| `ArgumentException` (incl. `ArgumentNullException`) | 400 | Invalid argument |
| anything else | 500 | Internal server error (generic detail) |

Every response body is `{ type, title, status, detail, instance, traceId }`. A separate
`BusinessRuleException` was **not** added: `DomainException` already is the domain's
"business rule broken" signal and the validation pipeline already produces field errors, so
a third type would be unused ceremony. Unhandled exceptions log server-side and return a
fixed generic `detail` — no stack traces, connection strings, passwords or token values
ever reach the client.

### Controller → MediatR → SQL Server

```text
HTTP request (Axios from the frontend, or Swagger UI)
  → UsersController action (ISender only)
    → MediatR pipeline
      → ValidationBehavior (FluentValidation: input shape) → ValidationException → 400
      → Command/Query handler
        → Domain value objects + aggregate methods → DomainException → 400
        → duplicate checks via IUserRepository        → ConflictException → 409
        → missing aggregate                           → NotFoundException → 404
        → IUserRepository (Infrastructure) → ApplicationDbContext + entity configurations
          → IUnitOfWork.SaveChangesAsync() → EF Core 10 → SQL Server (schema `etm`)
    ← Response DTO → 200 / 201 / 204, or ProblemDetails from GlobalExceptionHandler
```

### Swagger discovery

```sh
cd EnterpriseTaskManagement/API
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=EnterpriseTaskManagement;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run
# → https://localhost:7183/swagger  (Development only; UI lists all five User endpoints)
```

Swagger is registered once (`AddSwaggerGen`) and exposed only when
`app.Environment.IsDevelopment()`, at `RoutePrefix = "swagger"` reading
`/swagger/v1/swagger.json`. `GenerateDocumentationFile` plus
`IncludeXmlComments(..., includeControllerXmlComments: true)` surface the action summaries
and `ProducesResponseType` declarations (including 409 where the flow really produces it).

### Step 6 build status

This workspace has **no .NET SDK** (`dotnet` is not installed), so `dotnet build` could not
be run and the API could not be started — the Swagger UI smoke test is impossible here.
The projects, package references, DI registrations, middleware order and Development
Swagger configuration were reviewed by hand so that `/swagger` serves all five endpoints
once you run `dotnet build` / `dotnet run` locally.

No JWT/authentication, no React changes, and no Projects or Tasks features were added in
this step.

## PHASE 7 — COMPLETED (JWT Authentication)

### Inspected before changing
`Domain/Entities/Users/User.cs`, `Application/Interfaces/Persistence/IUserRepository.cs`,
`Infrastructure/Persistence/Repositories/UserRepository.cs`, `Application/DependencyInjection.cs`,
`Infrastructure/DependencyInjection.cs`, `API/Program.cs`, `API/Middleware/GlobalExceptionHandler.cs`,
`API/Controllers/UsersController.cs`, `appsettings*.json`, all four `.csproj` files.
No pre-existing auth code was found, so nothing was duplicated or rewritten.

### New files
- `Application/Interfaces/Authentication/IPasswordHasher.cs` — hash/verify abstraction.
- `Application/Interfaces/Authentication/IJwtTokenService.cs` — access-token abstraction (`AccessToken`).
- `Application/Common/Exceptions/AuthenticationFailedException.cs` — single generic failure (no user enumeration).
- `Application/Features/Auth/Commands/Login/{LoginCommand,LoginCommandHandler,LoginCommandValidator,LoginResponse}.cs`.
- `Infrastructure/Authentication/PasswordHasher.cs` — PBKDF2 (`Rfc2898DeriveBytes`, HMAC-SHA256, per-password salt, fixed-time compare).
- `Infrastructure/Authentication/JwtSettings.cs` — strongly typed `Jwt` section with validation.
- `Infrastructure/Authentication/JwtTokenService.cs` — HMAC-SHA256 signing via `JsonWebTokenHandler`.
- `API/Controllers/AuthController.cs` — `POST /api/Auth/login`, `ISender` only.
- `EnterpriseTaskManagement.slnx` — the solution file (it was missing; created and all four projects added).

### Modified files
- `Domain/Entities/Users/User.cs` — added framework-free `PasswordHash`/`HasPassword` and `SetPasswordHash` (provisioning pathway only; no plaintext ever stored or returned).
- `Infrastructure/Persistence/Configurations/UserConfiguration.cs` — maps `PasswordHash` (non-unicode, 512), ignores `HasPassword`.
- `Application/Interfaces/Persistence/IUserRepository.cs` + `UserRepository.cs` — email lookup used by login (still no EF types in Application).
- `Infrastructure/DependencyInjection.cs` — registers `JwtSettings`, `IPasswordHasher`, `IJwtTokenService`.
- `API/Middleware/GlobalExceptionHandler.cs` — `AuthenticationFailedException` → 401 with a fixed generic detail. Single exception pipeline retained.
- `API/Program.cs` — `AddAuthentication().AddJwtBearer(...)` validating issuer, audience, signing key, signature (HMAC-SHA256 only), lifetime and expiration; `AddAuthorization()`; `UseAuthentication()` before `UseAuthorization()`; Swagger Bearer scheme added once.
- `API/Controllers/UsersController.cs` — `[Authorize]` on `GET /api/Users` only. No roles.
- `appsettings.json` / `appsettings.Development.json` — `Jwt` placeholders with an **empty** `SecretKey` (no usable secret committed).
- `Application/Common/Behaviors/ValidationBehavior.cs` — disambiguated `ValidationException` (build fix).
- `Infrastructure/*.csproj` — `Microsoft.IdentityModel.JsonWebTokens 8.15.0`; documented `NoWarn NU1903` for the SqlClient-transitive `System.Security.Cryptography.Xml` advisory that no published version resolves.

### Flow
`POST /api/Auth/login` → `AuthController` → `LoginCommand` → MediatR (validation behavior) →
`LoginCommandHandler` → `Email` VO normalization → `IUserRepository.GetByEmailAsync` →
`IPasswordHasher.Verify` → active/not-deleted check → `IJwtTokenService.CreateAccessToken` →
`LoginResponse { accessToken, expiresAt, userPublicId, fullName, email }`.
Protected request: `Authorization: Bearer <token>` → JwtBearer validation → `GET /api/Users` → `GetAllUsersQuery`.

### Local secret setup (never committed)
```
cd EnterpriseTaskManagement/API
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "<32+ byte random value>"
dotnet user-secrets set "ConnectionStrings:SqlServer" "<your SQL Server connection string>"
```
Or environment variables `Jwt__SecretKey` / `ConnectionStrings__SqlServer`. Startup fails fast with a clear message if the key is missing or shorter than 32 bytes.

### Actual verification results
- `dotnet restore` — succeeded (.NET SDK 10.0.103).
- `dotnet build EnterpriseTaskManagement.slnx` — **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet test` — ran successfully; there are **no test projects** in the solution, so no tests executed.
- Live checks NOT performed: no SQL Server instance and no provisioned user with a password hash are available in this environment, so login, the 401/200 behaviour of `GET /api/Users`, and the Swagger UI at `/swagger` were **not** exercised over HTTP. Configuration was reviewed instead; Swagger is registered once and enabled in Development, and all six endpoints are attributed.

No role-based authorization, refresh tokens, Departments/Projects/Tasks features, React auth, or mock data were added. **Phase 8 — Role-Based Authorization** remains upcoming and is not implemented.

---

## PHASE 8 — COMPLETED (Role-Based Authorization)

### Files inspected before changing anything
`Domain/Entities/Users/User.cs`, `Domain/Common/*`, `Application/Features/Auth/Commands/Login/*`
(command, handler, validator, response), `Application/Interfaces/Authentication/*`,
`Application/Common/Models/UserSummary.cs`, the User query/command responses,
`Infrastructure/Authentication/JwtTokenService.cs` + `JwtSettings.cs`,
`Infrastructure/DependencyInjection.cs`, `Infrastructure/Persistence/Configurations/UserConfiguration.cs`,
`Infrastructure/Persistence/Migrations/README.md`, `API/Program.cs`,
`API/Controllers/UsersController.cs`, `API/Controllers/AuthController.cs`,
`API/Middleware/GlobalExceptionHandler.cs`, `appsettings*.json`, all four `.csproj` files.
No pre-existing role representation, policy, requirement handler or role claim existed, so only
the true gaps below were filled. No Phase 7 code was rewritten or restarted.

### New file
- `Domain/Entities/Users/UserRole.cs` — framework-free `enum UserRole { Employee = 0, Manager = 1, Admin = 2 }`.
  Exactly three roles, `Employee` first so it is the default value of the type. This is the single
  source of truth for role names; no arbitrary role strings can enter the system.

### Modified files
- `Domain/Entities/Users/User.cs` — added `Role` with a private setter (default `UserRole.Employee`),
  a `role` constructor parameter, `ChangeRole(UserRole)` validating with `Enum.IsDefined` and throwing
  `DomainException` otherwise, `Register` fixed to `Employee`, and `Rehydrate` taking an optional
  `role` that defaults to `Employee`. Still no EF/ASP.NET/JWT references.
- `Infrastructure/Persistence/Configurations/UserConfiguration.cs` — `Role` mapped as a required
  non-unicode `varchar(20)` via `HasConversion<string>()` with `HasDefaultValue(UserRole.Employee)`,
  so existing rows and inserts without a role are valid and least-privileged.
- `Infrastructure/Authentication/JwtTokenService.cs` — adds **exactly one** role claim,
  `ClaimTypes.Role`, sourced from `user.Role.ToString()`. Issuer, audience, HMAC-SHA256 signing,
  `NotBefore`/`Expires`/`IssuedAt` and the existing claim set are unchanged.
- `API/Program.cs` — `TokenValidationParameters.RoleClaimType = ClaimTypes.Role` (and
  `NameClaimType = "name"`). Required because Phase 7 sets `MapInboundClaims = false`; all Phase 7
  issuer/audience/signature/lifetime validation is preserved verbatim.
- `API/Controllers/UsersController.cs` — role attributes only (see table). Added 401/403
  `ProducesResponseType` declarations so Swagger documents the real behaviour.
- `Application/Features/Auth/Commands/Login/LoginResponse.cs` + `LoginCommandHandler.cs` —
  appended a `Role` string (role name only) so a client can render the correct UI without decoding
  the token. Additive and non-breaking; no hash, secret or internal id is exposed. Authorization
  itself never reads this field — the JWT role claim is authoritative.

### Endpoint restrictions and rationale
| Endpoint | Attribute | Rationale |
| --- | --- | --- |
| `POST /api/Users` | `[Authorize(Roles = "Admin")]` | Creating accounts is user administration. |
| `PUT /api/Users/{publicId}` | `[Authorize(Roles = "Admin")]` | Mutating identity/department is administration. |
| `DELETE /api/Users/{publicId}` | `[Authorize(Roles = "Admin")]` | Soft-deleting accounts is administration. |
| `GET /api/Users` | `[Authorize(Roles = "Admin,Manager")]` | Organizational read: managers need the roster. |
| `GET /api/Users/{publicId}` | `[Authorize(Roles = "Admin,Manager")]` | Same read surface, single record. |
| `POST /api/Auth/login` | anonymous | Authentication must precede authorization. |

Employees hold a valid token but no user-management role, so every `/api/Users` call returns a
framework-produced **403**; an absent/expired/invalid token returns a framework-produced **401**.
No role checks exist in controllers or handlers, no custom policy, requirement handler or
middleware was added, and the single `GlobalExceptionHandler` was left untouched — it never sees
401/403 because the authorization middleware short-circuits before an exception can be thrown.

### Database / migration status
No EF Core migrations exist in this repository yet (`Persistence/Migrations` holds only its
README), so there was nothing to add additively — the `Role` column will be included in the first
`InitialCreate` migration generated locally. `dotnet ef` tooling is not installed here and **no SQL
Server instance is available**, so no migration was generated and **no migration was applied**.
For an already-deployed database the column is additive and safe: required with default `'Employee'`.
No credentials, users or seed data were created.

### Swagger status
The Phase 7 Bearer security definition/requirement is preserved unchanged and still uses the same
JWT mechanism; only the per-endpoint 401/403 response declarations were added. **Swagger UI was not
exercised live** — the API cannot start without SQL Server, so `/swagger` was not opened in a browser.

### Exact build/test output
```
$ dotnet restore EnterpriseTaskManagement.slnx
  Restored .../EnterpriseTaskManagement.API.csproj (in 12.23 sec).
  Restored .../EnterpriseTaskManagement.Application.csproj (in 12.13 sec).
  Restored .../EnterpriseTaskManagement.Infrastructure.csproj (in 12.55 sec).

$ dotnet build EnterpriseTaskManagement.slnx
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
`dotnet test` — no test project exists in the solution, so no tests were executed.

### Actual verification performed (and what was blocked)
Verified: a throwaway probe project (referencing the built Infrastructure assembly, then deleted)
issued a token for a user with `UserRole.Admin` using a local throwaway key and validated it with
`RoleClaimType = ClaimTypes.Role`. Observed output — no token or secret printed:
```
valid=True
claim types: aud, iss, exp, iat, nbf, sub, publicId, email, name,
             http://schemas.microsoft.com/ws/2008/06/identity/claims/role, jti
IsInRole(Admin)=True IsInRole(Employee)=False
```
This confirms exactly one role claim of the standard type is emitted and that `[Authorize(Roles = ...)]`
resolves it (`IsInRole` is the same check the framework performs).

Blocked: no SQL Server instance, no provisioned users with password hashes and no running server,
so live HTTP tests (Admin 200 vs Employee 403 vs anonymous 401 on each endpoint), the Swagger UI and
any database migration were **not** performed. Those results are not claimed.

### Problems and fixes
- `MapInboundClaims = false` from Phase 7 means inbound claim types are not remapped, so a role
  claim would not be picked up by default — fixed by setting `RoleClaimType` explicitly, confirmed
  by the probe above.
- The probe initially failed with `DomainException: Last name is required.` from calling the wrong
  `FullName.Create` overload — a probe-only mistake, corrected in the probe; no product code changed.

**PHASE 8 — COMPLETED.** Role representation, JWT role claim and attribute-based endpoint
restrictions are implemented and the claim/`IsInRole` behaviour was verified directly; the remaining
live HTTP/DB scenarios are explicitly marked blocked above.

### Next: Phase 9 — Department CRUD
Implemented in the section at the end of this document.

### Interview explanation
- **Authentication vs authorization** — authentication answers "who is this caller?" (the login
  endpoint verifies a password hash and issues a signed token). Authorization answers "may this
  caller do this?" (the role claim inside the token is matched against `[Authorize(Roles = ...)]`).
  They are separate stages with separate failure codes.
- **How the claim is recognized** — the token carries one `ClaimTypes.Role` claim. JwtBearer builds
  a `ClaimsPrincipal` and, because `RoleClaimType` names that claim type, `IsInRole` and the
  `RolesAuthorizationRequirement` behind `[Authorize(Roles = ...)]` read it natively. No custom code.
- **401 vs 403** — 401 means the caller is not authenticated (missing, expired, wrong-issuer,
  wrong-audience or badly signed token). 403 means the caller *is* authenticated but the role
  requirement is not satisfied. Both come from the framework, not from our exception handler.
- **Middleware order** — `UseAuthentication()` must run before `UseAuthorization()`: the principal
  must exist before its roles can be evaluated. `UseExceptionHandler()` wraps both.
- **Boundary attributes** — role rules live at the HTTP boundary (controller actions), keeping
  handlers pure use cases with no transport or security concerns, so the same command can be reused
  by another entry point with its own policy.
- **Employee vs Admin branch** — an Employee's token authenticates fine, so the request reaches the
  authorization stage and is rejected there with 403; an Admin's token satisfies the requirement and
  the action dispatches its MediatR command as usual.
- **Future expansion** — adding a role means one enum member plus updating the relevant attributes;
  moving to fine-grained permissions means named policies registered in `AddAuthorization` (and
  permission claims from the token service) without touching Domain or Application.

---

## PHASE 9 — COMPLETED (Department CRUD)

### Files inspected before writing
`Domain/Entities/Users/User.cs`, `Domain/Common/{BaseEntity,AuditableEntity,AggregateRoot}.cs`,
the whole `Application/Features/Users/**` tree (command/handler/validator/response conventions),
`Application/Common/Models/UserSummary.cs`, `Application/Interfaces/Persistence/IUserRepository.cs`,
`Infrastructure/Persistence/{ApplicationDbContext.cs,Configurations/UserConfiguration.cs,Repositories/UserRepository.cs}`,
`Infrastructure/DependencyInjection.cs`, `Infrastructure/Persistence/Migrations/README.md`,
`API/Controllers/UsersController.cs`, `API/Contracts/Users/UpdateUserRequest.cs`,
`API/Middleware/GlobalExceptionHandler.cs`, `API/Program.cs`, `EnterpriseTaskManagement.slnx`.
No test project exists in the solution.

### Domain
`Domain/Entities/Departments/Department.cs` — framework-free `AggregateRoot<long>` with `Id`,
`PublicId`, `Name` (trimmed, required, max 150), `Code` (normalized upper case, `^[A-Z][A-Z0-9-]{1,19}$`),
optional trimmed `Description` (max 500), `IsActive`, and behaviour-only mutation
(`Create`, `Rehydrate`, `Update`, `Rename`, `Activate`, `Deactivate`, `Delete`). All invariants live
here and are not duplicated in validators. No domain event was added: the established User events
exist because other work reacts to them, and nothing reacts to department changes yet.

`Domain/Entities/Users/User.cs` — added a private parameterless materialization constructor. EF Core
cannot bind the owned `FullName` navigation through the existing parameterized constructor, so the
model could not be built at all without it. It is private, framework-attribute-free, and unreachable
from application code.

### Application (no EF Core, no DbContext)
- `Interfaces/Persistence/IDepartmentRepository.cs` — `GetByPublicIdAsync`, `CodeExistsAsync`
  (with `excludePublicId`), `ListAllAsync`, `AddAsync`, `Update`, `SoftDelete`. No `IQueryable`.
- `Common/Models/DepartmentSummary.cs` — primitive projection, matching `UserSummary`.
- `Features/Departments/Commands/{CreateDepartment,UpdateDepartment,DeleteDepartment}` and
  `Features/Departments/Queries/{GetAllDepartments,GetDepartmentByPublicId}` — command/query,
  validator, handler and response per folder, exactly mirroring the Users convention. Handlers
  depend only on `IDepartmentRepository` + `IUnitOfWork`, use domain behaviour, raise the existing
  `NotFoundException` / `ConflictException`, and never touch persistence types.
- Validators check input shape only (required, max length, non-empty `PublicId`); format and
  normalization stay in the Domain.

### Infrastructure
- `Persistence/Configurations/DepartmentConfiguration.cs` — `etm.Departments`, PK `Id`, `PublicId`
  as a 32-char `"N"` string with a unique index and an alternate key (users reference it),
  unique `Code` index, `Name`/`Description`/`IsActive`, audit + soft-delete columns,
  `HasQueryFilter(d => !d.IsDeleted)`, `Version` as SQL Server `rowversion`.
- `Persistence/Repositories/DepartmentRepository.cs` — implements the Application interface, relies
  on the query filter to hide deleted rows, and never removes a row physically.
- `ApplicationDbContext` already exposed `DbSet<Department>`; `DependencyInjection` now registers
  `IDepartmentRepository → DepartmentRepository` once.
- `UserConfiguration` already had the required `User.DepartmentId → Department.PublicId`
  relationship with `DeleteBehavior.Restrict`; unchanged, so deleting a department can never
  cascade-delete users.

### Schema and migration — actually generated
`dotnet ef migrations add InitialCreate` succeeded through the Nix-provided SDK and produced
`Infrastructure/Persistence/Migrations/20260830162955_InitialCreate.cs` (+ designer + snapshot).
It creates `etm.Departments` and `etm.Users` with `FK_Users_Departments_DepartmentId` /
`onDelete: ReferentialAction.Restrict`, unique indexes on `Departments.Code` and `Departments.PublicId`,
and the rowversion columns. This is the first migration in the repository, so nothing was deleted,
dropped or recreated. `Microsoft.EntityFrameworkCore.Design` was added to the API project (required by
the EF tools) and a local `dotnet-tools.json` pins `dotnet-ef`. Design-time configuration was supplied
through environment variables only — no credentials or secrets were committed.

**`dotnet ef database update` was NOT run: no SQL Server instance is available in this environment.**

### API
- `API/Contracts/Departments/{CreateDepartmentRequest,UpdateDepartmentRequest}.cs` — the update body
  deliberately has no `PublicId`; identity comes from the route only.
- `API/Controllers/DepartmentsController.cs` — thin, `ISender`-only:
  `POST /api/Departments` 201 `CreatedAtAction`, `GET /api/Departments` 200,
  `GET /api/Departments/{publicId:guid}` 200/404, `PUT /api/Departments/{publicId:guid}` 200/400/404/409,
  `DELETE /api/Departments/{publicId:guid}` 204/404.
- Authorization uses the existing JWT role claim only: writes `[Authorize(Roles = "Admin")]`, reads
  `[Authorize(Roles = "Admin,Manager,Employee")]`. No new permission system, no manual checks.
  401 (missing/invalid token) and 403 (valid token, wrong role) both come from the framework.
- `GlobalExceptionHandler` gained one mapping: `DbUpdateConcurrencyException → 409` with a generic
  message, so a stale `rowversion` surfaces as a conflict instead of silently overwriting a row.
  No database or exception detail is leaked.
- Swagger uses the existing bearer configuration; the five endpoints carry `ProducesResponseType`
  metadata for 200/201/400/401/403/404/409 plus XML summaries.

### Soft delete, auditing, concurrency
Delete is soft only: the aggregate marks itself deleted and deactivated, the row stays, and every
read is filtered by the query filter, so a deleted department returns 404 and disappears from the
list. `SaveChangesAsync` stamps audit values and converts any hard `Remove` into a soft delete.
Concurrency is the database-owned `rowversion`; a lost update produces 409, never a silent overwrite.

### Verification — exact outcomes
- `dotnet restore` / `dotnet build EnterpriseTaskManagement.slnx`: **succeeded, 0 warnings, 0 errors**
  (SDK 10.0.103 via `nix run nixpkgs#dotnet-sdk_10`).
- `dotnet test EnterpriseTaskManagement.slnx`: ran, **no test projects exist**, so no tests were added
  or executed; adding a test project was out of this phase's scope.
- `dotnet ef migrations add`: **succeeded** (file listed above).
- **Not performed:** `database update`, live HTTP calls, Swagger UI inspection and role-matrix
  (Admin/Manager/Employee/anonymous) testing — no SQL Server instance, no running server and no
  provisioned users. These results are not claimed.

### Problems encountered and fixes
1. EF model build failed with *"No suitable constructor was found for the type 'User'"* — the owned
   `FullName` cannot be bound through the parameterized constructor. Fixed with a private
   parameterless materialization constructor in the Domain (no framework reference).
2. EF tooling refused to run until `Microsoft.EntityFrameworkCore.Design` was referenced by the
   startup project; added.
3. Design-time host creation failed on the missing connection string and JWT secret guards (correct,
   secure behaviour). Supplied both as one-off environment variables for the tooling call only.

**PHASE 9 — COMPLETED.** Domain, Application, Infrastructure and API Department CRUD are implemented,
the build is clean and the migration was really generated; the database/HTTP checks that need a live
SQL Server are explicitly marked as not performed above.

### Next: Phase 10 — Project Management (not implemented)
No Project entity, use cases, endpoints, tasks, dashboard or frontend work is part of this phase.

## PHASE 10 — COMPLETED (Project Management)

**Domain (framework-free)**
- `Domain/Entities/Projects/ProjectStatus.cs` — closed status set: `Planned`, `Active`, `OnHold`, `Completed`, `Cancelled`.
- `Domain/Entities/Projects/Project.cs` — aggregate root with `PublicId`, `Name`, `Code`
  (normalized upper-case, `^[A-Z][A-Z0-9-]{1,19}$`), `Description`, `DepartmentId`
  (Department `PublicId`), `ManagerId` (User `PublicId`), `Status`, `StartDateUtc`,
  `TargetEndDateUtc`, plus behaviour: `Create`, `Rehydrate`, `Update`, `Rename`,
  `ChangeCode`, `ChangeDescription`, `ReassignManager`, `TransferDepartment`,
  `ChangeStatus` (terminal states are final), `Delete` (soft delete + cancel) and
  `IsManagedBy`. Auditing, soft delete and `Version` come from `AggregateRoot<long>`.

**Application (CQRS, no EF Core)**
- `Interfaces/Persistence/IProjectRepository.cs`, `Common/Models/ProjectSummary.cs`,
  `Common/Exceptions/ForbiddenAccessException.cs`.
- `Features/Projects/Commands/{CreateProject,UpdateProject,DeleteProject}` and
  `Features/Projects/Queries/{GetAllProjects,GetProjectByPublicId}` — command/query,
  handler, response and FluentValidation validator each, following the Department pattern.
- Handlers verify the referenced department and manager exist (manager must hold
  `Manager` or `Admin`), reject duplicate codes with `ConflictException` (409), and own
  the ownership rule: Admin may change any project; a Manager only projects whose
  `ManagerId` equals the authenticated user's public id (otherwise 403).

**Infrastructure (EF Core 10 + SQL Server)**
- `Persistence/Configurations/ProjectConfiguration.cs` — `etm.Projects`, Guids as 32-char
  "N" strings, unique indexes on `PublicId` and `Code`, indexes on `DepartmentId` /
  `ManagerId`, `Status` stored as its name with a `Planned` default, `rowversion`
  concurrency token, soft-delete query filter, `Restrict` FK to Department and
  `NoAction` FK to User.
- `Persistence/Repositories/ProjectRepository.cs`, `ApplicationDbContext.Projects`,
  and `IProjectRepository → ProjectRepository` registered in `DependencyInjection`.
- Migration `Persistence/Migrations/20260901140815_AddProjects` generated additively;
  the earlier `InitialCreate` migration is untouched. **Not applied** — no SQL Server
  instance is available in this environment.

**API**
- `Contracts/Projects/{CreateProjectRequest,UpdateProjectRequest}.cs` (no `PublicId`,
  no requester identity in the body).
- `Controllers/ProjectsController.cs` — thin, `ISender` only, PublicId routes:
  `POST /api/Projects` (Admin, Manager), `GET /api/Projects` (+ optional
  `departmentId` / `managerId` filters), `GET /api/Projects/{publicId}`,
  `PUT /api/Projects/{publicId}`, `DELETE /api/Projects/{publicId}`.
  Reads: Admin, Manager, Employee. Writes: Admin full; Manager create plus
  update/delete restricted to their own projects (enforced in the handlers from the
  token's `publicId` claim); Employee read-only.
- `Middleware/GlobalExceptionHandler.cs` now maps `ForbiddenAccessException → 403`
  alongside the existing 404/409/400/500 mappings.

**Verification actually performed**
- `dotnet build EnterpriseTaskManagement.slnx` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet ef migrations add AddProjects` → succeeded (design-time placeholder configuration).
- No test projects exist, so no tests were run. No live API/Swagger/SQL Server calls were
  possible: no SQL Server instance and no provisioned user in this environment.

**PHASE 10 — COMPLETED.** Domain, Application, Infrastructure and API project management
are implemented with role- and ownership-based authorization. Phase 11 (Task management)
is upcoming and not started.

## PHASE 11 — COMPLETED (Task Management)

Domain (framework-free)
- `Domain/Entities/Tasks/TaskPriority.cs` — Low, Medium, High, Critical.
- `Domain/Entities/Tasks/TaskItemStatus.cs` — Todo, InProgress, Review, Completed, Blocked
  (named `TaskItemStatus` so it can never clash with `System.Threading.Tasks.TaskStatus`).
- `Domain/Entities/Tasks/TaskItem.cs` — aggregate root (`TaskItem`, so it never clashes
  with `System.Threading.Tasks.Task`; the API resource is still `/api/Tasks`). Internal
  `Id`, external `PublicId`, `ProjectId` and optional `AssignedUserId` both referenced by
  public id. Invariants: title required/trimmed/max 200, description max 2000, project
  required, completed tasks are immutable (no edit, reassign or further status change),
  a task must be assigned before moving to InProgress/Review/Completed, and only these
  transitions are legal: Todo→InProgress|Blocked, InProgress→Review|Blocked|Todo,
  Review→Completed|InProgress|Blocked, Blocked→Todo|InProgress. Completed is terminal and
  stamps `CompletedAtUtc`. `Delete()` soft-deletes; rows are never physically removed.

Application (use cases only — no EF Core)
- `Interfaces/Persistence/ITaskRepository.cs` — EF-free abstraction (get by public id,
  list all / by project / by assignee / by project manager, add, update, soft delete).
- `Common/Models/TaskSummary.cs` — DTO of primitives and public ids only.
- `Features/Tasks/Common/TaskAuthorization.cs` — the one place the ownership rules live.
  Manager scope is resolved server-side from the Task's Project (`Project.IsManagedBy`),
  Employee scope from `TaskItem.IsAssignedTo`; both use the token identity passed in as
  `RequesterPublicId` / `RequesterRole`, never client input.
- Commands: CreateTask, UpdateTask (descriptive fields only — not reassignment, not
  status), DeleteTask, AssignTask (null assignee unassigns), ChangeTaskStatus.
  Queries: GetAllTasks, GetTaskByPublicId. Each with its FluentValidation validator for
  input shape only; all invariants stay in the Domain.

Infrastructure (EF Core 10 + SQL Server)
- `Persistence/Configurations/TaskItemConfiguration.cs` — `etm.Tasks`, Guids as 32-char
  "N" strings, unique index on `PublicId`, indexes on `ProjectId`, `AssignedUserId` and
  `Status`, FK to Project `Restrict` and optional FK to User `NoAction`, `Status` default
  `Todo`, auditing columns, `IsDeleted` global query filter, `rowversion` concurrency.
- `Persistence/Repositories/TaskRepository.cs`, `Tasks` DbSet on `ApplicationDbContext`,
  `ITaskRepository` registered in `DependencyInjection`.
- Additive migration `Persistence/Migrations/20260902135144_AddTasks.cs`. Existing
  migrations were left untouched.

API
- `Contracts/Tasks/*` request records and `Controllers/TasksController.cs`: POST,
  GET (list), GET `{publicId}`, PUT `{publicId}`, PUT `{publicId}/assignee`,
  PUT `{publicId}/status`, DELETE `{publicId}`. The controller only depends on `ISender`,
  reads the role and `publicId` claims from the validated JWT, and carries no business
  logic. Role gates: Admin+Manager for create/update/assign/delete, all roles for reads
  and for `status` (an Employee is then narrowed to their own assigned task inside the
  handler). The existing `GlobalExceptionHandler` maps the reused exception types to
  401/403 (`ForbiddenAccessException`) / 404 (`NotFoundException`) / 409
  (`ConflictException`, `DbUpdateConcurrencyException`) ProblemDetails.

Verification actually performed
- `dotnet restore`: succeeded. `dotnet build EnterpriseTaskManagement.slnx`: **succeeded,
  0 warnings, 0 errors**. `dotnet test`: ran and found **no test project in the solution**
  (none exists yet), so no tests were executed.
- The `AddTasks` migration was **generated** but **not applied**: no SQL Server instance
  is reachable in this environment. Run
  `dotnet ef database update --project Infrastructure --startup-project API` against a
  real server.
- No manual API or Swagger exercising was possible (no database, no provisioned users).

Phase 12 — Dashboard APIs was implemented next (see below).


## PHASE 12 — COMPLETED (Dashboard APIs)

Read-only reporting endpoints on top of Phases 1–11. Nothing existing was rewritten: the
Domain, the aggregate repositories, the JWT/role pipeline, the exception handler, Swagger
and all prior migrations are untouched.

Endpoints (all `GET`, all bearer-token protected)
- `GET /api/Dashboard/summary` — Admin, Manager, Employee. Headline counters for the
  caller's scope.
- `GET /api/Dashboard/tasks` — Admin, Manager, Employee. Task totals plus the status and
  priority breakdowns.
- `GET /api/Dashboard/projects` — Admin, Manager. Project totals plus the status breakdown.
- `GET /api/Dashboard/users` — Admin only. Organization-wide user and department counters.

Responses are explicit DTOs (`DashboardSummary`, `DashboardTaskMetrics`,
`DashboardProjectMetrics`, `DashboardUserMetrics`, `DashboardCount`) wrapped in a per-query
response record carrying `Scope` and `GeneratedAtUtc`. They expose counts and Domain enum
names only — never entities, never internal database ids, never names, e-mail addresses,
employee codes or password hashes.

CQRS (Application, EF/DbContext/SQL free)
- `GetDashboardSummaryQuery` / `GetTaskDashboardQuery` / `GetProjectDashboardQuery` /
  `GetUserDashboardQuery`, each with its own handler and response record. No validators were
  added: the queries carry no client input at all — the caller's identity and role are the
  only inputs and both come from the validated JWT.
- `Features/Dashboard/Common/DashboardScope.cs` holds the visibility rules in one place and
  reuses the existing `TaskRequesterRole` representation, so no second role system exists.

Read abstraction
- The aggregate repositories return whole entity collections, which would mean loading every
  row into memory to count it. Phase 12 therefore adds a purpose-built, EF-free
  `IDashboardRepository` (Application) implemented by `DashboardRepository` (Infrastructure).
  It returns finished read models: no `IQueryable`, no EF type and no `DbContext` crosses the
  Application boundary.

Authorization (server-derived only)
- Admin: full dashboard. Manager: project and task data restricted to the projects whose
  `ManagerId` equals the caller's `publicId`, resolved through the Project rows exactly as
  `TaskRepository` does. Employee: task data restricted to tasks assigned to themselves, and
  no organization endpoints — `/projects` and `/users` are refused. Refusals reuse
  `ForbiddenAccessException`, so the existing `GlobalExceptionHandler` returns the standard
  403 ProblemDetails; a missing or invalid token yields the framework 401.

Soft delete and correctness
- Every count runs through the global query filters configured in the entity configurations,
  so soft-deleted users, departments, projects and tasks are excluded everywhere. Grouped
  buckets are projected onto the full closed set of Domain enum values, so an empty bucket is
  reported as `0` rather than omitted. Status/priority keys are the existing Domain names —
  Planned/Active/OnHold/Completed/Cancelled, Todo/InProgress/Review/Completed/Blocked,
  Low/Medium/High/Critical, Employee/Manager/Admin.

Performance
- All aggregation is pushed to SQL Server: one `GROUP BY (1)` projection of conditional
  `COUNT`s per entity plus one `GROUP BY <enum>` per breakdown. Every query is
  `AsNoTracking()`. No entity collection is materialized, so there is no load-all and no
  N+1 behaviour.

Date/time convention
- The solution has no clock abstraction (Phases 1–11 call `DateTimeOffset.UtcNow` directly),
  and Phase 12 keeps that convention rather than inventing a new one, centralized in
  `DashboardInstant.Now()`. Everything is UTC. **Overdue** = non-deleted task with
  `DueDateUtc < now` and `Status != Completed`. **Due today** = `DueDateUtc` inside the
  half-open current UTC calendar day `[00:00Z, next 00:00Z)` with `Status != Completed`.

Migration
- **None was created, and no prior migration was altered.** Phase 12 adds no schema: the
  indexes the dashboard needs already exist — `Tasks.ProjectId`, `Tasks.AssignedUserId`,
  `Tasks.Status`, `Projects.ManagerId` — and the remaining groupings are full-scope
  aggregates where an extra index would not pay for itself.

Files created
- `Application/Common/Models/Dashboard/DashboardCount.cs`, `DashboardTaskMetrics.cs`,
  `DashboardProjectMetrics.cs`, `DashboardUserMetrics.cs`, `DashboardSummary.cs`
- `Application/Interfaces/Persistence/IDashboardRepository.cs`
- `Application/Features/Dashboard/Common/DashboardScope.cs`
- `Application/Features/Dashboard/Queries/GetDashboardSummary/{Query,Response,QueryHandler}.cs`
- `Application/Features/Dashboard/Queries/GetTaskDashboard/{Query,Response,QueryHandler}.cs`
- `Application/Features/Dashboard/Queries/GetProjectDashboard/{Query,Response,QueryHandler}.cs`
- `Application/Features/Dashboard/Queries/GetUserDashboard/{Query,Response,QueryHandler}.cs`
- `Infrastructure/Persistence/Repositories/DashboardRepository.cs`
- `API/Controllers/DashboardController.cs`

Files modified
- `Infrastructure/DependencyInjection.cs` — registered `IDashboardRepository`.
- `README.md` — this section.

Verification actually performed
- `dotnet restore`: succeeded. `dotnet build EnterpriseTaskManagement.slnx`: **succeeded,
  0 warnings, 0 errors**. `dotnet test`: ran and found **no test project in the solution**
  (none exists), so no tests were executed.
- **No live verification.** No SQL Server instance is reachable in this environment, so the
  endpoints were not called, no Swagger page was opened and no token was exercised. Swagger
  documents the four endpoints automatically through the existing setup and the XML comments.

Limitations / deviations
- Summary sections outside the caller's entitlement are returned as `null` rather than `0`,
  so a scoped caller cannot mistake "hidden" for "none".
- No per-department or per-manager project breakdown is exposed: it would require joining
  Department/User names into a reporting projection, which is beyond the requested metrics —
  an incomplete metric was deliberately not invented.

Phases 1–11 are preserved in full; Phase 13 was **NOT** implemented.

## PHASE 13 — COMPLETED (React Frontend Foundation)

Scope: frontend foundation only. No real API integration, no JWT/auth calls, no
CRUD, no Docker/deployment. Backend Phases 1–12 (Domain, Application,
Infrastructure, API, JWT, roles, repositories, migrations) were **not modified**.

### Files created

| File | Purpose |
| --- | --- |
| `src/types/domain.ts` | Strict frontend types; PublicId only; exact backend enum names (Task status Todo/InProgress/Review/Completed/Blocked, priority Low/Medium/High/Critical, project status Planned/Active/OnHold/Completed/Cancelled, roles Employee/Manager/Admin) |
| `src/types/dashboard.ts` | Mirrors the Phase 12 dashboard DTOs (counts + scope + generatedAtUtc; `null` = not entitled) |
| `src/auth/auth-context.tsx` | `AuthProvider` / `useAuth`: conceptual session (status, user, role, token, login, logout, hasRole/hasAnyRole). `login()` throws `AuthNotImplementedError` — no fake user, no request |
| `src/auth/ProtectedRoute.tsx` | Route guard shell; preview mode until Phase 14 (the layout states plainly that no session exists) |
| `src/auth/RoleGate.tsx` | `RoleGate`, `Can`, `useCapability`, central `capabilities` map — role-aware UX in one place, not per page |
| `src/components/layout/AppLayout.tsx` | Responsive authenticated shell: collapsible sidebar nav, header, breadcrumb, page title/description, actions slot, sign-out placeholder |
| `src/components/common/LoadingState.tsx` | `LoadingState`, `LoadingCards`, `LoadingRows` |
| `src/components/common/EmptyState.tsx` | Reusable empty state |
| `src/components/common/ErrorState.tsx` | `ErrorState` (with retry) and `RestrictedState` |
| `src/components/common/ConfirmDialog.tsx` | Reusable destructive-action confirmation |
| `src/components/common/ValidationSummary.tsx` | Renders ProblemDetails `errors` map + inline `FieldError` |
| `src/components/common/DataTableShell.tsx` | Table shell with loading/empty/error branches + `PaginationBar` matching `PagedResult` |
| `src/features/dashboard/demo-data.ts` | **Clearly labelled demo data, visualization only.** Shaped exactly like the Phase 12 responses; the only fabricated numbers in the app; deleted in Phase 14 |
| `src/routes/login.tsx`, `dashboard.tsx`, `users.tsx`, `departments.tsx`, `projects.tsx`, `tasks.tsx` | The six required routes |

### Files modified

- `src/routes/__root.tsx` — mounts `AuthProvider` and the toast `Toaster`; real app metadata replaces the template defaults.
- `src/routes/index.tsx` — placeholder replaced by a redirect to `/dashboard`.
- `src/lib/api/endpoints.ts` — paths aligned with the actual controllers (`/Auth`, `/Users`, `/Departments`, `/Projects`, `/Tasks`, `/Dashboard/...`).

### Routes

`/` → `/dashboard`, plus `/login`, `/dashboard`, `/users`, `/departments`,
`/projects`, `/tasks`. Every route defines its own `head()` metadata.

### API service foundation

`src/config/env.ts` reads `VITE_API_BASE_URL` / `VITE_API_TIMEOUT_MS`;
`src/lib/api/client.ts` is the single Axios instance with the Authorization
bearer request interceptor (the Phase 14 injection point) and an RFC 7807
ProblemDetails → `ApiError` response interceptor exposing `status` and
`fieldErrors`. No hard-coded URLs, secrets, or tokens exist, and **no API call
is made anywhere in the frontend yet**.

### Dashboard UI

Matches the Phase 12 response shape: headline counters, completion progress,
task-by-status, task-by-priority, project-by-status (Manager/Admin),
people-by-role (Admin only). All five states are demonstrable — loading, empty,
error, normal, and restricted (per-section role fallbacks). Entitlement `null`
renders "Not available", never `0`. A visible amber notice labels the demo
figures on every render.

### Accessibility and responsiveness

Semantic landmarks (`header`, `main`, `nav`), single `<h1>` per page, table
`caption`/`scope` headers, `role="status"`/`aria-live` on loading, `role="alert"`
on errors, labelled inputs with `aria-invalid`/`aria-describedby`, keyboard-
operable sidebar trigger and dialogs, and mobile-first layouts (sidebar collapses
to an off-canvas sheet; grids reflow at `sm`/`lg`/`xl`).

### Dependencies

None added. Everything reuses the existing React 19 / TypeScript / Vite /
TanStack Router + Query / Tailwind v4 / shadcn-ui / Axios / sonner stack.

### Verification actually performed

- `bun run build` (Vite production build): **succeeded**.
- `bunx eslint .` after `--fix`: **0 errors**, 9 warnings — all pre-existing
  `react-refresh/only-export-components` warnings in untouched template
  `src/components/ui/*` files.
- TypeScript is checked as part of the build pipeline and reports no errors.
- No backend build was run in this phase and no backend file was touched.

### Limitations / deviations

- `ProtectedRoute` intentionally renders content (the layout shows a visible
  "not signed in" notice) instead of redirecting, so the foundation is reviewable before a real
  session exists. The redirect switches on in Phase 14.
- Management pages are shells: columns, toolbars, pagination and dialogs exist,
  but no rows, because no API call or mock data is permitted here.
- The dashboard exposes a small state switcher so loading/empty/error/normal can
  be reviewed; it is preview scaffolding and goes away with Phase 14.

### Status

Backend Phases 1–12 preserved unchanged. **Phase 14 (real API + auth
integration) NOT implemented. Phase 15 (CRUD integration) NOT implemented.**

## PHASE 14 — COMPLETED (Real API + JWT Authentication Integration)

Scope: real login against the existing backend endpoint, real bearer-token
transport, true protected-route redirects, and live dashboard reads. **No CRUD
integration (Phase 15) and no fabricated accounts, tokens or data.** Backend
Phases 1–12 are unchanged apart from the single explicit CORS addition
documented below; frontend Phase 13 structure is preserved.

### Files created

| File | Purpose |
| --- | --- |
| `src/features/dashboard/queries.ts` | TanStack Query options for the four dashboard endpoints; shared `staleTime`, and a retry policy that never retries 401/403 |

### Files modified

- `src/lib/api/client.ts` — bearer injected automatically from token storage; RFC 7807 → `ApiError` with `isUnauthorized` / `isForbidden`; central 401 handling via `setUnauthorizedHandler` (skipped for the login request itself, so no redirect loop); 403 normalized to "Access denied. Your role doesn't allow this action." with no redirect.
- `src/auth/auth-context.tsx` — real `login()` calling `POST /api/Auth/login`; identity and role parsed **only** from the backend response (validated against the backend role names); session restored from storage with an `exp` sanity check; registers the central 401 invalidation handler. `AuthNotImplementedError` removed.
- `src/config/env.ts` — added the user-snapshot storage key.
- `src/auth/ProtectedRoute.tsx` — real redirect to `/login?redirect=<href>`; `/login` is never protected.
- `src/routes/login.tsx` — submits real credentials, surfaces backend errors, validates and honours the `redirect` search param (in-app paths only, never back to `/login`).
- `src/auth/RoleGate.tsx` — navigation capabilities added to the central capability map.
- `src/components/layout/AppLayout.tsx` — sidebar entries filtered by capability; the "not signed in" preview banner removed; sign-out cancels in-flight queries, clears the cache, clears the session, then replaces history with `/login`.
- `src/routes/dashboard.tsx` — live data from the four endpoints; per-section loading / empty / error / restricted states.
- `EnterpriseTaskManagement/API/Program.cs`, `appsettings.json`, `appsettings.Development.json` — explicit CORS allow-list.

### Files deleted

- `src/features/dashboard/demo-data.ts` — the last fabricated numbers in the app.

### Authentication endpoint and contract

`POST /api/Auth/login` with `{ email, password }`, returning
`{ accessToken, expiresAtUtc, publicId, fullName, email, role }` exactly as
`LoginResponse` declares it. Role and identity are read from that response only.
No refresh-token endpoint exists yet, so no refresh exchange is attempted — the
extension point in the response interceptor remains unused rather than faked.

### Token storage decision

The access token is stored in `localStorage` under an env-configured key, the
same mechanism Phase 13 established, because the API is a stateless bearer API
on a different origin and sends no cookies (`withCredentials: false`). The
trade-off is accepted deliberately: a cookie-based flow would need backend
`SameSite`/CSRF work that is out of this phase's scope. The token is never
logged and never placed in a URL. The stored `exp` is used only to avoid
presenting an obviously dead token; it is never treated as an authorization
decision.

### Routes and role-based navigation

Sidebar visibility comes from the central capability map: Admin sees dashboard,
users, departments, projects and tasks; Manager sees dashboard, projects and
tasks; Employee sees dashboard and tasks. **These checks are UX only.** The
ASP.NET Core role policies remain the single security boundary.

### Dashboard integration

`/api/Dashboard/summary` and `/tasks` load for every signed-in role;
`/projects` is requested only for Manager/Admin and `/users` only for Admin, so
unentitled roles never fire a request that would 403. Sections render real
loading, empty, error and restricted states, `null` renders "Not available"
(never `0`), and there is no fallback data of any kind.

### Environment and CORS

The API base URL comes only from `VITE_API_BASE_URL` (see `.env.example`); no
URL is hard-coded. The backend had no CORS configuration, which blocks every
browser call from the dev origin, so the smallest sufficient policy was added:
origins come from `Cors:AllowedOrigins` in configuration (development lists
`http://localhost:8080` and `http://localhost:5173`, production is empty), with
only `Authorization`/`Content-Type` headers and `GET/POST/PUT/DELETE` methods.
There is no `AllowAnyOrigin`, no wildcard, and credentials are not allowed.

### 401 vs 403

401 → the session is invalidated once and the user is sent to `/login` with the
attempted path preserved; the login request itself is exempt, so failed sign-ins
show an inline error instead of looping. 403 → an "Access denied" message in
place, no sign-out and no redirect.

### Verification actually performed

- `bunx tsgo --noEmit`: **0 errors**.
- `bun run build`: **succeeded**.
- `bunx eslint src`: **0 errors**, 10 pre-existing `react-refresh` warnings in untouched template `src/components/ui/*` files.
- `dotnet build EnterpriseTaskManagement.slnx`: **0 warnings, 0 errors**.
- `dotnet test`: restore succeeded and reported no test project — there is still no test project in the solution.
- **Live auth / API / Swagger testing was NOT performed and is reported as blocked:** no SQL Server instance is reachable in this environment, so no database, no user accounts and no real credentials exist. No token was ever issued or exercised.

### Security checklist

No password, token or secret is logged, hard-coded or committed; credentials are
sent only in the login request body over the configured API URL; role checks in
the frontend are presentation-only; CORS is an explicit configured allow-list;
no mock accounts, mock API or fake data exist anywhere in the production path.

### Limitations / deviations

- Session restore trusts only the stored token's presence and expiry; a `GET /api/Auth/me` endpoint does not exist, so the cached identity snapshot cannot be re-validated on load.
- No refresh-token rotation, because the backend exposes no refresh endpoint.
- Management list pages remain shells — populating them is Phase 15.

### Status

Phases 1–13 preserved. **Phase 15 (CRUD integration) NOT implemented.**

## PHASE 15 — COMPLETED (Frontend CRUD Integration)

Scope: real create / read / update / delete against the existing Phase 9–11
controllers, plus the dashboard type/entitlement cleanup. **No new backend
behavior, no new endpoints, no migrations, no seed or mock data, and Phase 16 is
not started.** Backend Phases 1–12 are untouched in this phase; frontend Phases
13–14 structure is preserved.

### Files created

| File | Purpose |
| --- | --- |
| `src/lib/api/errors.ts` | `describeApiError` / `toErrorMessage` — one place that turns an `ApiError` into a title, message, field errors and the `canRetry` / `requiresRefresh` / `isForbidden` / `isNotFound` flags |
| `src/features/shared/query-keys.ts` | central `queryKeys`, `invalidateEntities` (dashboard included wherever a write can change a counter), shared retry policy and query options |
| `src/features/users/users.ts` | list/detail query options + create/update/delete mutations |
| `src/features/departments/departments.ts` | list/detail query options + create/update/delete mutations |
| `src/features/projects/projects.ts` | list/detail query options + create/update/delete mutations |
| `src/features/tasks/tasks.ts` | list/detail query options + create/update/delete plus dedicated assign and change-status mutations |
| `src/components/common/FormDialog.tsx` | accessible dialog form shell with a live-region error summary |
| `src/components/common/SelectField.tsx` | `SelectField` and `RelationSelect` (loading / error / forbidden / empty states for relation pickers) |
| `src/components/common/ListToolbar.tsx` | shared search + filter + actions toolbar and the `matchesSearch` helper |

### Files modified

- `src/routes/users.tsx`, `departments.tsx`, `projects.tsx`, `tasks.tsx` — real lists from the API with search, filters, pagination shell, and add / edit / remove dialogs; relation pickers fed by the real department, manager, project and assignee lists; every write invalidates the affected list, detail and dashboard keys.
- `src/routes/dashboard.tsx` — summary props now use the shared `DashboardSummary` / `DashboardCount` types from `src/types/dashboard.ts` instead of a locally declared shape, and the entitlement flags from the central capability map are passed into `dashboardProjectsQuery(enabled)` / `dashboardPeopleQuery(enabled)`. Unentitled roles never fire a request that would 403; those panels render the restricted state instead, and a 403 that does arrive renders restricted rather than an error.

### CRUD endpoints used (all pre-existing)

`/api/Users`, `/api/Departments`, `/api/Projects`, `/api/Tasks` — list, get by
PublicId, POST, PUT, DELETE; plus `/api/Tasks/{publicId}/assign` and
`/api/Tasks/{publicId}/status`. Assignment and status changes always use their
own endpoints, never the general update, so the domain's transition rules stay
authoritative. All identifiers crossing the wire are PublicIds.

### Roles and restricted behavior

Page and action visibility comes from the same central capability map as Phase
14 (Admin: users, departments, projects, tasks; Manager: projects, tasks;
Employee: tasks). **These checks remain UX only** — the ASP.NET Core role
policies and the API's ownership checks are the single security boundary, and
the UI never asserts ownership itself.

### Errors: 400 / 401 / 403 / 404 / 409

400 → inline field errors plus a summary, no retry offered. 401 → the Phase 14
central handler invalidates the session once and redirects to `/login`. 403 →
"Access denied" in place, no sign-out, no redirect. 404 → "No longer available"
with a refresh. 409 → "This record changed elsewhere", and the list is refetched
so the user re-reads before retrying (the backend's rowversion concurrency).

### Verification actually performed

- `bunx eslint src --fix` then `bunx eslint src`: **0 errors**, 11 pre-existing `react-refresh` warnings in untouched template `src/components/ui/*` files.
- `bunx tsgo --noEmit`: **0 errors**.
- `bun run build`: **succeeded**.
- `dotnet restore` + `dotnet build EnterpriseTaskManagement.slnx` (.NET 10 SDK): **0 warnings, 0 errors**.
- `dotnet test`: restore succeeded and reported no test project — there is still no test project in the solution.
- **Live CRUD / auth / Swagger testing was NOT performed and is reported as blocked:** no SQL Server instance is reachable in this environment, so no database, no accounts and no rows exist. No request was ever executed against a running API, and nothing was faked to simulate one.

### Security checklist

No password, token or secret is logged, hard-coded or committed. No mock
accounts, mock API, in-memory database or fabricated rows exist anywhere. Role
checks in the frontend are presentation-only. No `AllowAnyOrigin` and no CORS
relaxation was introduced in this phase.

### Limitations / deviations

- `CreateUserCommand` accepts no password and no role, so the user form collects neither; both are set by other means.
- `UserSummary` returns a single `fullName` and no department, so the user edit dialog's department picker starts empty and must be confirmed by the operator.
- List search and filtering are client-side over the returned page, because the API exposes no search parameter.
- Lists render every returned record; server-side paging is not available yet.

### Status

Phases 1–14 preserved. **Phase 16 NOT implemented.**
