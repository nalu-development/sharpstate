namespace Nalu.SharpState;

/// <summary>
/// No-op ownership token for resolvers that reuse the same provider for <c>ReactAsync</c>.
/// </summary>
internal sealed class StateMachineReactiveNonOwningScope : IDisposable
{
    /// <summary>
    /// Shared instance safe to dispose any number of times.
    /// </summary>
    public static StateMachineReactiveNonOwningScope Instance { get; } = new();

    private StateMachineReactiveNonOwningScope()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
