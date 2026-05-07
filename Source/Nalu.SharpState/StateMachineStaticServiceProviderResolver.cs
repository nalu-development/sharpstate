namespace Nalu.SharpState;

/// <summary>
/// Resolver that reuses the same provider instance for synchronous dispatch and <c>ReactAsync</c> reactions.
/// </summary>
/// <typeparam name="TServiceProvider">Service provider type passed to transition clauses and reactions.</typeparam>
public class StateMachineStaticServiceProviderResolver<TServiceProvider> : IStateMachineServiceProviderResolver<TServiceProvider>
{
    private readonly TServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a resolver backed by <paramref name="serviceProvider"/>.
    /// </summary>
    /// <param name="serviceProvider">Provider returned for synchronous clauses and reactions.</param>
    public StateMachineStaticServiceProviderResolver(TServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public TServiceProvider GetServiceProvider() => _serviceProvider;

    /// <inheritdoc />
    public IDisposable CreateScopedServiceProvider(out TServiceProvider serviceProvider)
    {
        serviceProvider = _serviceProvider;
        return StateMachineReactiveNonOwningScope.Instance;
    }
}
