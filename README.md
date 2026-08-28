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
