using BenchmarkDotNet.Attributes;
using Stateless;

namespace Nalu.SharpState.Benchmarks;

public class DoorContext
{
    public int OpenCount { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? LastReason { get; set; }
}

[StateMachineDefinition(typeof(DoorContext))]
public static partial class DoorMachine
{
    /// <summary>
    /// Opens the door.
    /// </summary>
    /// <param name="reason">Explains why the door is being opened.</param>
    [StateTriggerDefinition]
    static partial void Open(string reason);

    /// <summary>
    /// Closes the door.
    /// </summary>
    [StateTriggerDefinition]
    static partial void Close();

    /// <summary>
    /// The door is currently closed.
    /// </summary>
    [StateDefinition(Initial = true)]
    private static IStateConfiguration Closed { get; } = ConfigureState()
        .OnOpen(t => t
                     .When((_, reason) => reason is not "spying", "Not spying")
                     .Target(State.Opened)
                     .Invoke((ctx, reason) => ctx.LastReason = reason)
        );

    /// <summary>
    /// The door is currently open.
    /// </summary>
    [StateDefinition]
    private static IStateConfiguration Opened { get; } = ConfigureState()
                                                         .OnClose(t => t.Target(State.Closed))
                                                         .WhenEntering((ctx, _) => ctx.OpenCount++);
}

[MemoryDiagnoser]
public class StateMachineBenchmark
{
    private static (DoorContext doorActorContext, DoorMachine.IActor doorActor) CreateDoorActor()
    {
        var doorActorContext = new DoorContext();
        var doorActor = DoorMachine.CreateActor(doorActorContext, BenchmarkServiceProviders.EmptyResolver);
        return (doorActorContext, doorActor);
    }

    private static (StateMachine<DoorMachine.State, DoorMachine.Trigger> doorStateless, DoorContext doorStatelessContext, StateMachine<DoorMachine.State, DoorMachine.Trigger>.TriggerWithParameters<string> openWithReasonTrigger)
        DoorStateless()
    {
        var doorStateless = new StateMachine<DoorMachine.State, DoorMachine.Trigger>(DoorMachine.State.Closed);
        var doorStatelessContext = new DoorContext();
        var openWithReasonTrigger = doorStateless.SetTriggerParameters<string>(DoorMachine.Trigger.Open);
        doorStateless.Configure(DoorMachine.State.Closed)
                     .PermitIf(openWithReasonTrigger, DoorMachine.State.Opened, reason => reason is not "spying", "Not spying");
        doorStateless.Configure(DoorMachine.State.Opened)
                     .OnEntryFrom(openWithReasonTrigger, reason => doorStatelessContext.LastReason = reason)
                     .OnEntry(() => doorStatelessContext.OpenCount++)
                     .Permit(DoorMachine.Trigger.Close, DoorMachine.State.Closed);
        return (doorStateless, doorStatelessContext, openWithReasonTrigger);
    }
    
    private DoorMachine.IActor _doorActor = null!;
    private DoorContext _doorActorContext = null!;
    private StateMachine<DoorMachine.State, DoorMachine.Trigger> _doorStateless = null!;
    private DoorContext _doorStatelessContext = null!;
    private StateMachine<DoorMachine.State, DoorMachine.Trigger>.TriggerWithParameters<string> _openWithReasonTrigger = null!;

    [GlobalSetup]
    public void Setup()
    {
        var (doorActorContext, doorActor) = CreateDoorActor();
        _doorActorContext = doorActorContext;
        _doorActor = doorActor;

        var (doorStateless, doorStatelessContext, openWithReasonTrigger) = DoorStateless();
        _doorStateless = doorStateless;
        _doorStatelessContext = doorStatelessContext;
        _openWithReasonTrigger = openWithReasonTrigger;
    }

    [Params(100, 10000)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int StateChanges { get; set; }

    [Benchmark]
    public void SingletonActor()
    {
        var count = StateChanges;
        for (var i = 0; i < count; i++)
        {
            _doorActor.Open("reason");
            _doorActor.Close();
        }
    }
    
    [Benchmark]
    public void SingletonStateless()
    {
        var count = StateChanges;
        for (var i = 0; i < count; i++)
        {
            _doorStateless.Fire(_openWithReasonTrigger, "reason");
            _doorStateless.Fire(DoorMachine.Trigger.Close);
        }
    }
    
    [Benchmark]
    public void TransientActor()
    {
        var count = StateChanges;
        for (var i = 0; i < count; i++)
        {
            var (_, doorActor) = CreateDoorActor();
            doorActor.Open("reason");
            doorActor.Close();
        }
    }
    
    [Benchmark]
    public void TransientStateless()
    {
        var count = StateChanges;
        for (var i = 0; i < count; i++)
        {
            var (doorStateless, _, openWithReasonTrigger) = DoorStateless();
            doorStateless.Fire(openWithReasonTrigger, "reason");
            doorStateless.Fire(DoorMachine.Trigger.Close);
        }
    }
}
