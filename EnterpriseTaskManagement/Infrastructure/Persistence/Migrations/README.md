# Migrations

EF Core migrations for `ApplicationDbContext` live here. `InitialCreate` is generated
(Departments + Users). It has **not** been applied: no SQL Server instance was available.
To create further migrations or apply them locally:

```sh
cd EnterpriseTaskManagement
dotnet ef migrations add InitialCreate \
  --project Infrastructure/EnterpriseTaskManagement.Infrastructure.csproj \
  --startup-project API/EnterpriseTaskManagement.API.csproj \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project Infrastructure/EnterpriseTaskManagement.Infrastructure.csproj \
  --startup-project API/EnterpriseTaskManagement.API.csproj
```

The migrations assembly and the `__EFMigrationsHistory` table (schema `etm`) are
configured in `Infrastructure/DependencyInjection.cs`.
