namespace Nalu.SharpState.Tests.Runtime;

internal sealed class TestContext
{
    public int Counter { get; set; }

    public string? LastArg { get; set; }

    public List<string> Log { get; } = [];
}

// Test double implementing IStateAwareContext to record OnStateChanged calls.
internal sealed class StateAwareTestContext<TState> : IStateAwareContext<TState>
    where TState : struct, Enum
{
    public int Counter { get; set; }

    public List<string> Log { get; } = [];

    public List<TState> NotifiedStates { get; } = [];

    public void OnStateChanged(TState state)
    {
        NotifiedStates.Add(state);
        Log.Add("notify:" + state);
    }
}

internal enum FlatState
{
    A,
    B,
    C
}

internal enum FlatTrigger
{
    Go,
    Alt,
    NoMatch
}

internal enum HierState
{
    Idle,
    Connected,
    Authenticating,
    Authenticated,
    Outside
}

internal enum HierTrigger
{
    Connect,
    Disconnect,
    AuthOk,
    Message,
    GoOutside
}
