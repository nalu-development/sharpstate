namespace Nalu.SharpState.Tests.EndToEnd;

/// <summary>
/// Scoped service registered in Microsoft.Extensions.DependencyInjection to assert the reaction scope is disposed of.
/// </summary>
public sealed class ScopedResource : IDisposable
{
    private int _disposeCallCount;

    public int DisposeCallCount => _disposeCallCount;

    public void Dispose() => Interlocked.Increment(ref _disposeCallCount);
}

public sealed class ScopedDiProbeContext
{
    public ScopedResource? Captured { get; set; }
}

[StateMachineDefinition(typeof(ScopedDiProbeContext))]
public static partial class ScopedDiProbeMachine
{
    [StateTriggerDefinition]
    static partial void Go();

    [StateDefinition(Initial = true)]
    private static IStateConfiguration Idle { get; } = ConfigureState()
        .OnGo(t => t
            .Target(State.Done)
            .ReactAsync<ScopedResource>((_, ctx, scopedResource) =>
            {
                ctx.Captured = scopedResource;
                return default;
            }));

    [StateDefinition]
    private static IStateConfiguration Done { get; } = ConfigureState();
}
