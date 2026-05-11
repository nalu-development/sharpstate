namespace Nalu.SharpState;

/// <summary>
/// Resolver that reuses the same provider instance for synchronous dispatch and <c>ReactAsync</c> reactions.
/// </summary>
public class StateMachineStaticServiceProviderResolver : IStateMachineServiceProviderResolver
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a resolver backed by <paramref name="serviceProvider"/>.
    /// </summary>
    /// <param name="serviceProvider">Provider returned for synchronous clauses and reactions.</param>
    public StateMachineStaticServiceProviderResolver(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IServiceProvider GetServiceProvider() => _serviceProvider;

    /// <inheritdoc />
    public IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider)
    {
        serviceProvider = _serviceProvider;
        return StateMachineReactiveNonOwningScope.Instance;
    }
}
