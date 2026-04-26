# Nalu.SharpState

[![Nalu.SharpState NuGet Package](https://img.shields.io/nuget/v/Nalu.SharpState.svg)](https://www.nuget.org/packages/Nalu.SharpState/) [![Nalu.SharpState NuGet Package Downloads](https://img.shields.io/nuget/dt/Nalu.SharpState)](https://www.nuget.org/packages/Nalu.SharpState/) [![codecov](https://codecov.io/gh/nalu-development/sharpstate/graph/badge.svg?branch=main)](https://codecov.io/gh/nalu-development/sharpstate)

**Nalu.SharpState** is a Roslyn source generator for **declarative, strongly typed, hierarchical** state machines in .NET: you declare states and triggers on a `public static partial` class, configure transitions with a **fluent API**, and the generator emits compile-time registration tables and an `IActor` with typed trigger methods.

Optional **`ReactAsync(...)`** callbacks run *after* a transition commits; they receive the generated actor so you can **await I/O and then fire more triggers** transitioning to new states.

Dispatch does not use string dictionaries or runtime reflection, so the machine stays **AOT and trim-friendly** with a **small CPU and memory footprint** on the hot path.

## Install

```bash
dotnet add package Nalu.SharpState
```

The package includes the analyzer; no extra registration call is required.

## At a glance

Define a context (with eventual service dependencies), mark a `public static partial` class with `[StateMachineDefinition]`, add `[StateTriggerDefinition]` methods for inputs and `[StateDefinition]` properties for states, then wire transitions with `ConfigureState()` (see the full [door sample](Tests/Nalu.SharpState.Tests/EndToEnd/DoorMachine.cs) in the test suite):

```csharp
public class DoorContext
{
    public int OpenCount { get; set; }
    public string? LastReason { get; set; }
}

[StateMachineDefinition(typeof(DoorContext))]
public static partial class DoorMachine
{
    [StateTriggerDefinition] static partial void Open(string reason);
    [StateTriggerDefinition] static partial void Close();

    [StateDefinition(Initial = true)]
    private static IStateConfiguration Closed { get; } = ConfigureState()
        .OnOpen(t => t
            .When((_, reason) => reason is not "spying", "Not spying")
            .Target(State.Opened)
            .Invoke((ctx, reason) => { ctx.OpenCount++; ctx.LastReason = reason; }));

    [StateDefinition]
    private static IStateConfiguration Opened { get; } = ConfigureState()
        .OnClose(t => t.Target(State.Closed));
}
```

Use the generated API from your app:

```csharp
var door = DoorMachine.CreateActor(new DoorContext());
door.Open("delivery");
Console.WriteLine(door.CurrentState); // Opened
```

### Asynchronous reactions

The synchronous trigger API can still schedule async follow-up work after a transition commits; the callback gets the `IActor` and can fire more triggers (for example after `await`ing a service on the context):

```csharp
[StateDefinition]
private static IStateConfiguration Pending { get; } = ConfigureState()
    .OnRequestApproval(t => t
        .Target(State.Approving)
        .ReactAsync(async (actor, ctx, id) =>
        {
            try {
                await ctx.ApproveService.ApproveAsync(id);
                actor.Approve();
            } catch {
                actor.Reject();
            }
        }));
```

Details and ordering: [Post-Transition Reactions](https://nalu-development.github.io/sharpstate/sharpstate-async.html).

### Benchmarks

Outperform the industry standard ([Stateless](https://github.com/dotnet-state-machine/stateless)) with **4x to 8x faster execution** and **7x to 12x** lower memory overhead depending on the usage.

| Method             | StateChanges | Mean        | Error     | StdDev    | Gen0      | Gen1     | Allocated   |
|------------------- |------------- |------------:|----------:|----------:|----------:|---------:|------------:|
| SingletonActor     | 100          |    10.32 us |  0.029 us |  0.025 us |    4.3945 |        - |    35.94 KB |
| SingletonStateless | 100          |    41.63 us |  0.484 us |  0.404 us |   30.0293 |        - |   245.31 KB |
| TransientActor     | 100          |    11.27 us |  0.027 us |  0.023 us |    5.9204 |        - |    48.44 KB |
| TransientStateless | 100          |    89.98 us |  1.224 us |  1.022 us |   75.0732 |   1.3428 |   614.08 KB |
| SingletonActor     | 10000        | 1,020.74 us |  6.633 us |  5.539 us |  439.4531 |        - |  3593.75 KB |
| SingletonStateless | 10000        | 3,956.54 us | 41.182 us | 38.521 us | 2953.1250 |        - | 24140.63 KB |
| TransientActor     | 10000        | 1,120.78 us |  6.764 us |  5.648 us |  591.7969 |        - |  4843.75 KB |
| TransientStateless | 10000        | 8,699.77 us | 87.558 us | 77.618 us | 7468.7500 | 140.6250 | 61016.85 KB |

See the [benchmarks](https://github.com/nalu-development/sharpstate/tree/main/Tests/Nalu.SharpState.Benchmarks) for more details.

### Dependency Injection and Unit Testing

The generator adds `CreateActorFactory` and `CreateActorWithStateFactory` (aligned with `CreateActor` / `CreateActorWithState`) so you can register the delegate in a container, inject it where you build actors, and stub `IActor` in tests—`CreateActorFactory` is the typical choice when the default initial state is enough. The context you pass into every transition can hold your services, so async reactions such as the `ReactAsync` block above keep dependencies mockable. See [Testability](https://nalu-development.github.io/sharpstate/index.html#testability) in the full guide.

### Visualize the configured state machine

The same type also emits a **Graphviz** diagram as text: `DoorMachine.ToDot()` returns a `digraph` you can pass to the `dot` tool (for example `dot -Tpng -o door.png`) or paste into any Graphviz-compatible viewer—useful for documentation, reviews, or debugging transitions and guards.

For the door sample above, that call produces the DOT below; the diagram is the same graph rendered with Graphviz (`dot -Tsvg`).

<table>
<tr valign="middle">
<td>

<pre>
digraph G {
  label = "DoorMachine";
  labelloc = t;
  compound = true;
  start [shape=Mdiamond,label="Closed"];

  state_1 [shape=rectangle,label="Opened"];
  trigger_0 [shape=ellipse,label="Close"];
  state_1 -> trigger_0;
  trigger_1 [shape=ellipse,label="Open\n[Not spying]"];
  start -> trigger_1;

  trigger_0 -> start;
  trigger_1 -> state_1;
}
</pre>

</td>
<td width="35%">

<img src="docs/assets/images/door-machine.svg" alt="DoorMachine state diagram (Graphviz render of ToDot output)" style="max-height: 380px" />

</td>
</tr>
</table>

## Documentation

Full guides (transitions, hierarchy, `ReactAsync`, diagnostics, API reference) live here:

**[https://nalu-development.github.io/sharpstate/](https://nalu-development.github.io/sharpstate/)**

---

## Contributing & building from source

See [CONTRIBUTING.md](CONTRIBUTING.md).


