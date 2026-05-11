using Microsoft.Extensions.DependencyInjection;

namespace Nalu.SharpState;

/// <summary>
/// Resolver backed by an <see cref="IServiceProvider"/>.
/// Synchronous clauses use <see cref="GetServiceProvider"/>; each <c>ReactAsync</c> reaction opens a child DI scope.
/// </summary>
public class StateMachineServiceProviderResolver : IStateMachineServiceProviderResolver
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a resolver that uses <paramref name="serviceProvider"/> to retrieve dependencies.
    /// </summary>
    public StateMachineServiceProviderResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public IServiceProvider GetServiceProvider() => _serviceProvider;

    /// <inheritdoc />
    public virtual IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider)
    {
        var scope = _serviceProvider.CreateScope();
        serviceProvider = scope.ServiceProvider;
        return scope;
    }
}
