namespace Nalu.SharpState.Tests.EndToEnd;

public class DoorContext
{
    public int OpenCount { get; set; }

    public string? LastReason { get; set; }
}

[StateMachineDefinition(typeof(DoorContext))]
public static partial class DoorMachine
{
    /// <summary>
    /// Opens the door.
    /// </summary>
    /// <param name="reason">Explains why the door is being opened.</param>
    [StateTriggerDefinition] static partial void Open(string reason);

    /// <summary>
    /// Closes the door.
    /// </summary>
    [StateTriggerDefinition] static partial void Close();

    /// <summary>
    /// The door is currently closed.
    /// </summary>
    [StateDefinition(Initial = true)]
    private static IStateConfiguration Closed { get; } = ConfigureState()
        .OnOpen(t => t
                     .When((_, reason) => reason is not "spying", "Not spying")
                     .Target(State.Opened)
                     .Invoke((ctx, reason) =>
                         {
                             ctx.OpenCount++;
                             ctx.LastReason = reason;
                         }
                     )
        );

    /// <summary>
    /// The door is currently open.
    /// </summary>
    [StateDefinition]
    private static IStateConfiguration Opened { get; } = ConfigureState()
        .OnClose(t => t.Target(State.Closed));
}
