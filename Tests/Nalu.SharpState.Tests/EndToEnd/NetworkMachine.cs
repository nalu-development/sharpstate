namespace Nalu.SharpState.Tests.EndToEnd;

public class NetworkContext
{
    public List<string> Log { get; } = [];
}

[StateMachineDefinition(typeof(NetworkContext))]
public partial class NetworkMachine
{
    [StateTriggerDefinition] static partial void Connect();

    [StateTriggerDefinition] static partial void Disconnect();

    [StateTriggerDefinition] static partial void AuthOk();

    [StateTriggerDefinition] static partial void Message(string text);

    [StateTriggerDefinition] static partial void StartEdit();

    [StateTriggerDefinition] static partial void Save();

    [StateDefinition(Initial = true)]
    private static IStateConfiguration Idle { get; } = ConfigureState()
        .OnConnect(t => t.Target(State.Connected));

    [StateDefinition]
    private static IStateConfiguration Connected { get; } = ConfigureState()
        .OnDisconnect(t => t.Target(State.Idle));

    [SubStateMachine(parent: State.Connected)]
    private partial class ConnectedRegion
    {
        [StateDefinition(Initial = true)]
        private static IStateConfiguration Authenticating { get; } = ConfigureState()
            .OnAuthOk(t => t.Target(State.Authenticated));

        [StateDefinition]
        private static IStateConfiguration Authenticated { get; } = ConfigureState()
            .OnMessage(t => t.Stay().Invoke((ctx, text) => ctx.Log.Add(text)));

        [SubStateMachine(parent: State.Authenticated)]
        private partial class AuthenticatedRegion
        {
            [StateDefinition(Initial = true)]
            private static IStateConfiguration Browsing { get; } = ConfigureState()
                .OnStartEdit(t => t.Target(State.Editing));

            [StateDefinition]
            private static IStateConfiguration Editing { get; } = ConfigureState()
                .OnSave(t => t.Target(State.Browsing));
        }
    }
}
