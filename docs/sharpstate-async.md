# Post-Transition Async Work

`Nalu.SharpState` keeps the primary generated actor surface synchronous: each trigger has a `void` method that dispatches through `Fire(...)`. For asynchronous follow-up, you can:

1. Register **`WhenExitedAsync(...)`**, **`WhenEnteredAsync(...)`**, and **`ReactAsync(...)`** on the fluent configuration.
2. Call the generated **`{Trigger}Async(...)`** methods on **`IActor`** when callers need to **await** that post-transition work (they use `FireAsync(...)` internally).

Sync trigger methods still **schedule** post-transition async work (fire-and-forget, subject to the captured synchronization context). Async trigger methods **await** the same pipeline inline.

## What post-transition async does

For **external** transitions, synchronous work runs first (guards, exit, invoke, commit, entry, notifications, `StateChanged`). Then a single post-transition pipeline may run:

1. **`WhenExitedAsync(...)`** for each state on the **exit path** from the source up to the lowest common ancestor (in order).
2. **`WhenEnteredAsync(...)`** for each state on the **entry path** from the lowest common ancestor to the destination (in order).
3. **`ReactAsync(...)`** for the transition, if any.

For **internal** transitions (`Stay()` / `Ignore()`) and for **dynamic** `TransitionTo(...)` when the resolved **leaf** equals the current leaf, **only** **`ReactAsync`** runs in the post-transition phase (no async entered/exited hooks).

Lifecycle hooks and ordering mirror the synchronous paths used for `WhenEntering` / `WhenExiting` (`EnumerateEntryPath` / `EnumerateExitPath`).

## Example: `ReactAsync` with DI

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
            .TransitionTo(State.Approving)
            // Request collaborators as typed callback parameters.
            .ReactAsync<IApprovalService>(async (actor, ctx, args, approvals) =>
            {
                try {
                    await approvals.ApproveAsync(args.Id);
                    await actor.ApproveAsync();
                } catch {
                    await actor.RejectAsync();
                }
            }));

    [StateDefinition]
    private static IStateConfiguration Approving { get; } = ConfigureState()
        .OnApprove(t => t.TransitionTo(State.Approved))
        .OnReject(t => t.TransitionTo(State.Rejected));

    [StateDefinition]
    private static IStateConfiguration Approved { get; } = ConfigureState();

    [StateDefinition]
    private static IStateConfiguration Rejected { get; } = ConfigureState();
}
```

Register **`IApprovalService`** (and logging types such as **`ILogger<T>`** where you use them) in your DI container. SharpState resolves requested service parameters with `IServiceProvider.GetService(typeof(T))` and throws if the result is `null`.

## Full ordering (external transition)

1. **`WhenExiting(...)`** (synchronous)
2. **`Invoke(...)`** (synchronous transition action)
3. State commit
4. **`WhenEntering(...)`** (synchronous)
5. **`StateChanged`**
6. **`WhenExitedAsync(...)`** (post-transition, along exit path)
7. **`WhenEnteredAsync(...)`** (post-transition, along entry path)
8. **`ReactAsync(...)`** (if registered)

For internal transitions, only synchronous **`Invoke`** runs before the post-transition phase; then only **`ReactAsync`** may run.

## Service provider behavior

- **Synchronous** **`Fire(...)`** (and generated `void` trigger methods): if there is post-transition async work, the engine captures **`SynchronizationContext.Current`** and schedules the pipeline. It calls **`CreateScopedServiceProvider(out IServiceProvider)`** exactly **once** per scheduled run; that provider is passed to **all** async lifecycle hooks and **`ReactAsync`** for that transition, then disposed when the pipeline completes.
- **`FireAsync(...)`** (and generated **`ValueTask {Trigger}Async(...)`** methods): the pipeline runs inline and receives the actor’s **captured root** provider from **`GetServiceProvider()`**. It does **not** open a reaction scope.

With **`Microsoft.Extensions.DependencyInjection`**, use **`StateMachineServiceProviderResolver`** when scheduled work should use a child DI scope. Use **`StateMachineStaticServiceProviderResolver`** when the same instance should be used everywhere. See [Service provider and actor factories](index.html#service-provider-and-actor-factories).

In ASP.NET Core, do not rely on live `HttpContext` or request-scoped services inside fire-and-forget scheduled work. Copy request data into the machine context or arguments before firing.

If **`ReactAsync`** is registered multiple times on the same transition, the callbacks are awaited sequentially in declaration order.

If **`ReactAsync`** is not registered but async lifecycle hooks are, the pipeline still runs for external transitions.

## Awaiting post-transition work from callers

For each trigger, the generator emits:

- `void {Name}(...)` → **`Fire`**
- `ValueTask {Name}Async(...)` → **`FireAsync`** (awaits exited async, entered async, reaction as applicable)

Use **`OpenAsync`** (etc.) when the caller must observe completion or failure of that async pipeline.

## Synchronization context behavior (scheduled `Fire`)

The scheduled path captures **`SynchronizationContext.Current`** when **`Fire`** runs.

- If a context exists (for example a UI thread), the pipeline starts there.
- If no context exists, the work is queued on the thread pool.

## Failure reporting

### After commit: scheduled `Fire` (fire-and-forget)

Exceptions from **`WhenExitedAsync`**, **`WhenEnteredAsync`**, or **`ReactAsync`** do **not** propagate from **`Fire`**. Subscribe to **`ReactionFailed`**:

```csharp
actor.ReactionFailed += (from, to, trigger, args, exception) =>
    logger.LogError(exception, "Post-transition async work failed for {Trigger}", trigger);
```

Subscriber exceptions are swallowed so a buggy logger does not fault the process.

### After commit: `FireAsync` / `{Trigger}Async`

Failures in the post-transition pipeline still raise **`ReactionFailed`**, then throw **`ReactionFailedException`** with the original exception as **`InnerException`**. The exception message states that the transition **already committed**.

If a **`ReactionFailed`** subscriber throws while handling such a failure, that subscriber exception is ignored; callers still receive **`ReactionFailedException`** for the underlying pipeline failure.

### Before / during commit

Exceptions from guard evaluation, target selection, synchronous **`WhenExiting` / `WhenEntering`**, **`Invoke`**, context notification, or **`StateChanged`** are **not** wrapped in **`ReactionFailedException`** and do **not** raise **`ReactionFailed`**.

## When to use `Invoke` vs `ReactAsync` vs async lifecycle hooks

Use **`Invoke(...)`** when the effect must complete before the new state is visible.

Use **`ReactAsync(...)`** when the state should commit immediately and follow-up work is a separate concern.

Use **`WhenExitedAsync` / `WhenEnteredAsync`** when the concern is tied to **hierarchical** exit or entry paths rather than a single transition’s reaction.

Use **`{Trigger}Async`** from application code when you need to **await** any of that post-transition work reliably.
