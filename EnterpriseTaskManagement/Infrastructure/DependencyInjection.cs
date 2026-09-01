using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Authentication;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Infrastructure.Authentication;
using EnterpriseTaskManagement.Infrastructure.Persistence;
using EnterpriseTaskManagement.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseTaskManagement.Infrastructure;

/// <summary>
/// Composition root of the Infrastructure layer: the API calls
/// <c>AddInfrastructure(configuration)</c> and receives the SQL Server
/// <see cref="ApplicationDbContext"/>, the unit of work, and the repository
/// implementations behind their Application-layer abstractions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Configuration key holding the SQL Server connection string.</summary>
    public const string SqlServerConnectionName = "SqlServer";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Credentials come from configuration/environment only:
        //   ConnectionStrings:SqlServer  (appsettings, user-secrets, env var
        //   ConnectionStrings__SqlServer, or Key Vault). Never hard-coded.
        var connectionString = configuration.GetConnectionString(SqlServerConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing connection string 'ConnectionStrings:SqlServer'. Configure it via appsettings, " +
                "user secrets, or the ConnectionStrings__SqlServer environment variable.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", ApplicationDbContext.DefaultSchema);
                sql.EnableRetryOnFailure();
            }));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        // Authentication services behind their Application-layer abstractions.
        // Jwt:SecretKey comes from User Secrets / the Jwt__SecretKey environment
        // variable — the repository holds no usable key.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
