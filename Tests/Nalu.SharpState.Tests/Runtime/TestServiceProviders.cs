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
