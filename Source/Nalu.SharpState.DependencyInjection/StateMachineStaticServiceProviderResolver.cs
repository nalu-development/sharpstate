namespace Nalu.SharpState;

/// <summary>
/// Resolver that reuses the root <see cref="IServiceProvider"/> for synchronous dispatch and <c>ReactAsync</c> reactions.
/// </summary>
public sealed class StateMachineStaticServiceProviderResolver(IServiceProvider serviceProvider) : StateMachineStaticServiceProviderResolver<IServiceProvider>(serviceProvider);
