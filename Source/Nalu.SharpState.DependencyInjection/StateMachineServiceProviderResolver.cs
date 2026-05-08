using Microsoft.Extensions.DependencyInjection;

namespace Nalu.SharpState;

/// <summary>
/// Resolver that uses the injected <see cref="IServiceProvider"/> for synchronous dispatch and opens a child DI scope for
/// each <c>ReactAsync</c> reaction.
/// </summary>
public class StateMachineServiceProviderResolver : IStateMachineServiceProviderResolver<IServiceProvider>
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a resolver backed by <paramref name="serviceProvider"/>.
    /// </summary>
    /// <param name="serviceProvider">Provider returned by <see cref="GetServiceProvider"/>.</param>
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
