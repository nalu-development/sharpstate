namespace Nalu.SharpState.Tests.EndToEnd;

public class HookContext
{
    public List<string> Log { get; } = [];
}

[StateMachineDefinition(typeof(HookContext))]
public static partial class HookMachine
{
    [StateTriggerDefinition] static partial void Start();

    [StateTriggerDefinition] static partial void Ping();

    [StateDefinition(Initial = true)]
    private static IStateConfiguration Idle { get; } = ConfigureState()
        .WhenExiting(ctx => ctx.Log.Add("exit:Idle"))
        .OnStart(t => t.TransitionTo(State.Running));

    [StateDefinition]
    private static IStateConfiguration Running { get; } = ConfigureState()
        .WhenEntering(ctx => ctx.Log.Add("enter:Running"))
        .OnPing(t => t.Ignore());
}
