using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateMachineStaticServiceProviderResolverTests
{
    [Fact]
    public void Static_resolver_reuses_same_provider_for_sync_and_reaction()
    {
        var services = EmptyServiceProvider.Instance;
        var resolver = new StateMachineStaticServiceProviderResolver(services);

        resolver.GetServiceProvider().Should().BeSameAs(services);

        using var ownership = resolver.CreateScopedServiceProvider(out var reactionServices);
        reactionServices.Should().BeSameAs(services);
        ownership.Dispose();
    }
}
