using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateMachineEmptyServiceProviderResolverTests
{
    [Fact]
    public void Instance_reuses_empty_provider_for_sync_and_reaction()
    {
        var resolver = StateMachineEmptyServiceProviderResolver.Instance;

        resolver.GetServiceProvider().GetService(typeof(object)).Should().BeNull();

        using var ownership = resolver.CreateScopedServiceProvider(out var reactionServices);
        reactionServices.Should().BeSameAs(resolver.GetServiceProvider());
        reactionServices.GetService(typeof(object)).Should().BeNull();
        ownership.Dispose();
    }

    [Fact]
    public void Public_constructor_creates_empty_resolver()
    {
        var resolver = new StateMachineEmptyServiceProviderResolver();

        resolver.GetServiceProvider().GetService(typeof(object)).Should().BeNull();
    }
}
