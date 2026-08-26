using System.Reflection;

using EnterpriseTaskManagement.Application.Common.Behaviors;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseTaskManagement.Application;

/// <summary>
/// Composition root of the Application layer. The API calls
/// <c>services.AddApplication()</c> to register MediatR (all commands, queries
/// and handlers in this assembly), every FluentValidation validator, and the
/// validation pipeline behavior that runs before each handler.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
