# Post-Transition Reactions

`Nalu.SharpState` now keeps the generated actor surface synchronous: every trigger method returns `void` and dispatch happens through `Fire(...)`. When you need asynchronous follow-up work, use `ReactAsync(...)` on the trigger builder.

## What `ReactAsync(...)` does

`ReactAsync(...)` schedules fire-and-forget work **after** the transition is already finished:

```csharp
using System.Threading.Tasks;
using Nalu.SharpState;

// Register in DI, e.g. services.AddSingleton<IApprovalService, ApprovalService>();
public interface IApprovalService
{
    Task ApproveAsync(string id);
}

[StateMachineDefinition(typeof(InspectContext))]
public static partial class ReviewMachine
{
    [StateTriggerDefinition] static partial void RequestApproval(string id);
    [StateTriggerDefinition] static partial void Approve();
    [StateTriggerDefinition] static partial void Reject();

    [StateDefinition]
    private static IStateConfiguration Pending { get; } = ConfigureState()
        .OnRequestApproval(t => t
            .Target(State.Approving)
            // Request collaborators as typed callback parameters.
            .ReactAsync<IApprovalService>(async (actor, ctx, args, approvals) =>
            {
                try {
                    await approvals.ApproveAsync(args.Id);
                    actor.Approve();
                } catch {
                    actor.Reject();
                }
            }));

    [StateDefinition]
    private static IStateConfiguration Approving { get; } = ConfigureState()
        .OnApprove(t => t.Target(State.Approved))
        .OnReject(t => t.Target(State.Rejected));

    [StateDefinition]
    private static IStateConfiguration Approved { get; } = ConfigureState();

    [StateDefinition]
    private static IStateConfiguration Rejected { get; } = ConfigureState();
}
```

Register **`IApprovalService`** (and logging types such as **`ILogger<T>`** where you use them) in your DI container. SharpState resolves requested service parameters with `IServiceProvider.GetService(typeof(T))` and throws if the result is `null`.

For external transitions, the execution order is:

1. `WhenExiting(...)`
2. `Invoke(...)`
3. state commit
4. `WhenEntering(...)`
5. `StateChanged`
6. `ReactAsync(...)`

For internal transitions (`Stay()` / `Ignore()`), only the inline `Invoke(...)` runs before the background reaction is scheduled.

If you use a dynamic `Target((ctx, args...) => ...)` and it resolves to the current leaf for a specific fire, that fire also behaves like an internal transition.

## Synchronization context behavior

`ReactAsync(...)` captures the current `SynchronizationContext` when the trigger is fired.

- If a context exists (for example a UI thread), the reaction starts there.
- If no context exists, the reaction is queued on the thread pool.

This keeps the main trigger path synchronous while still giving UI applications predictable follow-up scheduling. The callback receives the generated `IActor` first, so it can fire more triggers after awaited work completes. Overloads start with `(actor, context, args)` and may request service parameters after that. Those services come from `CreateScopedServiceProvider(out IServiceProvider)`, and the engine disposes the returned token after the reaction.

With **`Microsoft.Extensions.DependencyInjection`**, add the **`Nalu.SharpState.DependencyInjection`** package and use **`StateMachineServiceProviderResolver`** from **`Nalu.SharpState`** (or **`AddScopedStateMachineServiceProviderResolver`**) when each `ReactAsync` should receive its own child DI scope. Use **`StateMachineStaticServiceProviderResolver`** from **`Nalu.SharpState`** (optionally **`AddSingletonStateMachineServiceProviderResolver`**) when synchronous clauses and reactions should share the same provider. For custom rules, implement **`IStateMachineServiceProviderResolver`** yourself. See [Service provider and actor factories](index.html#service-provider-and-actor-factories).

In ASP.NET Core, do not rely on live `HttpContext` or request-scoped services inside `ReactAsync`. `AsyncLocal` values such as `IHttpContextAccessor.HttpContext` may flow to the scheduled reaction, but the request can finish before the reaction runs. Copy request data you need, such as correlation IDs or `HttpContext.Items`, into the machine context, trigger arguments, or an immutable snapshot service before firing the trigger.

If `ReactAsync(...)` is registered multiple times on the same transition, the callbacks are awaited sequentially in declaration order.

If a caller or test needs to await a specific reaction, keep that coordination explicit. For example, store a `TaskCompletionSource` on the machine context, complete it from the relevant `ReactAsync` callback, and await it from the caller. This waits for the reaction you care about without making every actor expose reaction-draining APIs.

## Failure reporting

Because the reaction is fire-and-forget, exceptions do **not** flow back out of the trigger method. Instead, subscribe to `ReactionFailed`:

```csharp
actor.ReactionFailed += (from, to, trigger, args, exception) =>
    logger.LogError(exception, "Reaction failed for {Trigger}", trigger);
```

Use an **`ILogger`** (or **`ILoggerFactory`**) you resolve from your host or **`IServiceProvider`**, not the state machine **`Context`**, unless you intentionally cache a logger reference there when constructing the context.

The event is raised with:

- the committed source leaf state
- the committed destination leaf state
- the trigger
- the machine-specific trigger args union
- the thrown exception

## When to use `Invoke(...)` vs `ReactAsync(...)`

Use `Invoke(...)` when the side effect is part of the transition itself and must complete before the new state becomes visible.

Use `ReactAsync(...)` when the transition should commit immediately and the asynchronous work is a follow-up concern such as telemetry, notifications, cache refreshes, or best-effort synchronization.
