namespace Nalu.SharpState;

/// <summary>
/// Resolves the provider used by synchronous transition clauses and by <c>ReactAsync</c> reactions.
/// </summary>
public interface IStateMachineServiceProviderResolver
{
    /// <summary>
    /// Returns the provider captured by the actor for synchronous <c>When</c>, <c>Target</c>, and <c>Invoke</c> clauses.
    /// </summary>
    /// <returns>The actor-scoped provider used by synchronous transition clauses.</returns>
    IServiceProvider GetServiceProvider();
    
    /// <summary>
    /// Creates the provider and ownership token for one <c>ReactAsync</c> reaction.
    /// A resolver can use this to give background work a scope that outlives the caller's scope.
    /// </summary>
    /// <param name="serviceProvider">Receives the provider passed to <c>ReactAsync</c> overloads.</param>
    /// <returns>
    /// Token disposed after the reaction finishes, such as an <c>IServiceScope</c>; return a no-op token when no cleanup is needed.
    /// </returns>
    IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider);
}
