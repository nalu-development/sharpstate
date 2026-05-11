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
            .ReactAsync((actor, ctx, _) =>
            {
                ctx.Inspections++;
                actor.Finish();
                return default;
            }))
        .OnFinish(t => t
            .Target(State.Done)
            .ReactAsync((_, ctx, _) =>
            {
                ctx.Inspections += 10;
                return default;
            }));

    [StateDefinition]
    private static IStateConfiguration Done { get; } = ConfigureState();
}
