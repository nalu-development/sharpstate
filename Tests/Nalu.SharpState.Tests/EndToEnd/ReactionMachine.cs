namespace Nalu.SharpState.Tests.EndToEnd;

public class InspectContext
{
    public int Inspections { get; set; }
}

[StateMachineDefinition(typeof(InspectContext))]
public static partial class ReactionMachine
{
    [StateTriggerDefinition] static partial void Inspect();

    [StateTriggerDefinition] static partial void Finish();

    [StateDefinition(Initial = true)]
    private static IStateConfiguration Idle { get; } = ConfigureState()
        .OnInspect(t => t
            .Stay()
            .ReactAsync(async (actor, ctx) =>
            {
                ctx.Inspections++;
                await actor.FinishAsync();
            }))
        .OnFinish(t => t
            .TransitionTo(State.Done)
            .ReactAsync((_, ctx) =>
            {
                ctx.Inspections += 10;
                return default;
            }));

    [StateDefinition]
    private static IStateConfiguration Done { get; } = ConfigureState();
}
