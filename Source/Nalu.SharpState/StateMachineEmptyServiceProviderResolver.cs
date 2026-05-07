namespace Nalu.SharpState;

/// <summary>
/// Resolver backed by an empty <see cref="IServiceProvider"/> for machines that do not resolve services.
/// </summary>
public sealed class StateMachineEmptyServiceProviderResolver : IStateMachineServiceProviderResolver<IServiceProvider>
{
    /// <summary>
    /// Shared resolver instance for machines that do not resolve services.
    /// </summary>
    public static StateMachineEmptyServiceProviderResolver Instance { get; } = new();

    /// <summary>
    /// Initializes a new empty service-provider resolver.
    /// </summary>
    public StateMachineEmptyServiceProviderResolver()
    {
    }

    /// <inheritdoc />
    public IServiceProvider GetServiceProvider() => EmptyServiceProvider.Instance;

    /// <inheritdoc />
    public IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider)
    {
        serviceProvider = EmptyServiceProvider.Instance;
        return StateMachineReactiveNonOwningScope.Instance;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        private EmptyServiceProvider()
        {
        }

        public object? GetService(Type serviceType) => null;
    }
}
