# Post-Transition Reactions

`Nalu.SharpState` now keeps the generated actor surface synchronous: every trigger method returns `void` and dispatch happens through `Fire(...)`. When you need asynchronous follow-up work, use `ReactAsync(...)` on the trigger builder.

## What `ReactAsync(...)` does

`ReactAsync(...)` schedules fire-and-forget work **after** the transition is already finished:

```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
            // TServiceProvider overload: resolve collaborators from DI, not from the context.
            .ReactAsync(async (actor, ctx, services, id) =>
            {
                var approvals = services.GetRequiredService<IApprovalService>();
                try {
                    await approvals.ApproveAsync(id);
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

`GetRequiredService` is an extension from **`Microsoft.Extensions.DependencyInjection`**. Register **`IApprovalService`** (and logging types such as **`ILogger<T>`** where you use them) in your DI container.

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

This keeps the main trigger path synchronous while still giving UI applications predictable follow-up scheduling. The callback receives the generated `IActor` first, so it can fire more triggers after awaited work completes. Overloads may include **`TServiceProvider`** after the context; that value comes from **`CreateScopedServiceProvider(out TServiceProvider)`**, and the engine disposes the returned token after the reaction.

With **`Microsoft.Extensions.DependencyInjection`**, use **`StateMachineServiceProviderResolver`** (or **`AddScopedStateMachineServiceProviderResolver()`**) when each `ReactAsync` should receive its own DI scope. That separate reaction scope is intentional: the reaction must not keep using services from the caller's scope, and once the reaction scope is opened it can continue even if the caller scope is disposed. Use **`AddSingletonStateMachineServiceProviderResolver()`** when synchronous clauses and reactions should share the root provider. For custom rules, implement **`IStateMachineServiceProviderResolver<IServiceProvider>`** yourself. See [Service provider and actor factories](index.html#service-provider-and-actor-factories). For **`AddScoped<T>()`** types resolved from the reaction provider (and optional custom **`CreateScopedServiceProvider`**), see [Scoped services in ReactAsync](index.html#scoped-services-in-reactasync).

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
- the boxed trigger arguments
- the thrown exception

## When to use `Invoke(...)` vs `ReactAsync(...)`

Use `Invoke(...)` when the side effect is part of the transition itself and must complete before the new state becomes visible.

Use `ReactAsync(...)` when the transition should commit immediately and the asynchronous work is a follow-up concern such as telemetry, notifications, cache refreshes, or best-effort synchronization.
