using Microsoft.Extensions.DependencyInjection;

namespace Nalu.SharpState;

/// <summary>
/// Registration helpers for <see cref="IStateMachineServiceProviderResolver{IServiceProvider}"/>.
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
        services.AddScoped<IStateMachineServiceProviderResolver<IServiceProvider>, StateMachineServiceProviderResolver>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="StateMachineServiceProviderResolver"/> as scoped. Each <c>ReactAsync</c> reaction opens a child DI scope
    /// so background work does not keep using services from the caller's scope.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="TResolver">Concrete implementation of <see cref="IStateMachineServiceProviderResolver{IServiceProvider}"/> to register as scoped.</typeparam>
    /// <remarks>Use when you need more than the default child scope—for example, subclass <see cref="StateMachineServiceProviderResolver"/>, override <see cref="StateMachineServiceProviderResolver.CreateScopedServiceProvider"/>, call the base implementation, then wrap or configure the scoped <see cref="IServiceProvider"/>.</remarks>
    public static IServiceCollection AddScopedStateMachineServiceProviderResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, IStateMachineServiceProviderResolver<IServiceProvider>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IStateMachineServiceProviderResolver<IServiceProvider>, TResolver>();
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
        services.AddSingleton<IStateMachineServiceProviderResolver<IServiceProvider>, StateMachineStaticServiceProviderResolver>();
        return services;
    }
}
