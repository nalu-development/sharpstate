using Nalu.SharpState;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration helpers for <see cref="IStateMachineServiceProviderResolver"/>.
/// </summary>
public static class ServiceCollectionStateMachineExtensions
{
    /// <summary>
    /// Registers <see cref="StateMachineServiceProviderResolver"/> as scoped. Each <c>ReactAsync</c> reaction opens a child DI scope
    /// so background work does not keep using services from the caller's scope.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddScopedStateMachineServiceProviderResolver(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IStateMachineServiceProviderResolver, StateMachineServiceProviderResolver>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="StateMachineStaticServiceProviderResolver"/> as singleton. Synchronous transitions and
    /// <c>ReactAsync</c> reuse the root <see cref="IServiceProvider"/>; no child scope is created.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddSingletonStateMachineServiceProviderResolver(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IStateMachineServiceProviderResolver, StateMachineStaticServiceProviderResolver>();
        return services;
    }
}
