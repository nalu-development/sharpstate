namespace Nalu.SharpState.Tests.Runtime;

/// <summary>
/// Minimal <see cref="IServiceProvider"/> for transition-builder tests that do not resolve services.
/// </summary>
internal sealed class EmptyServiceProvider : IServiceProvider
{
    public static readonly EmptyServiceProvider Instance = new();

    private EmptyServiceProvider()
    {
    }

    public object? GetService(Type serviceType) => null;
}

internal static class TestServiceProviders
{
    /// <summary>
    /// Resolver backed by an empty <see cref="IServiceProvider"/>.
    /// </summary>
    public static readonly IStateMachineServiceProviderResolver EmptyResolver =
        StateMachineEmptyServiceProviderResolver.Instance;
}

/// <summary>
/// Counts how often <see cref="IStateMachineServiceProviderResolver.CreateScopedServiceProvider"/> is invoked.
/// </summary>
internal sealed class CountingScopeServiceProviderResolver : IStateMachineServiceProviderResolver
{
    private static readonly IDisposable _emptyDisposable = new EmptyDisposable();

    private readonly IServiceProvider _root;

    public CountingScopeServiceProviderResolver(IServiceProvider root)
    {
        _root = root;
    }

    public int ScopeCreateCount { get; private set; }

    public IServiceProvider GetServiceProvider() => _root;

    public IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider)
    {
        ScopeCreateCount++;
        serviceProvider = _root;
        return _emptyDisposable;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
