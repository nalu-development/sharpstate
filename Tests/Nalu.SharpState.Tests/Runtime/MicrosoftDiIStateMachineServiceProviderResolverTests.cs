using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Nalu.SharpState.Tests.Runtime;

public class MicrosoftDiIStateMachineServiceProviderResolverTests
{
    private sealed class TestResolver : IStateMachineServiceProviderResolver
    {
        private readonly IServiceProvider _instance;

        public TestResolver(IServiceProvider instance) => _instance = instance;

        public IServiceProvider GetServiceProvider() => _instance;

        public IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider)
        {
            serviceProvider = _instance;
            return StateMachineReactiveNonOwningScope.Instance;
        }
    }

    [Fact]
    public void MicrosoftDependencyInjectionResolvesScopedResolverImplementation()
    {
        var services = new ServiceCollection();
        services.AddScoped<IStateMachineServiceProviderResolver, TestResolver>();

        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var resolver = sp.GetRequiredService<IStateMachineServiceProviderResolver>();

        resolver.Should().BeOfType<TestResolver>();
        ReferenceEquals(resolver.GetServiceProvider(), sp).Should().BeTrue();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, sp).Should().BeTrue();
        }
    }

    [Fact]
    public void StateMachineStaticServiceProviderResolverReusesProvider()
    {
        var marker = new object();
        var services = new ServiceCollection();
        services.AddSingleton(_ => marker);
        services.AddSingleton<IStateMachineServiceProviderResolver, StateMachineStaticServiceProviderResolver>();

        using var root = services.BuildServiceProvider();
        var resolver = root.GetRequiredService<IStateMachineServiceProviderResolver>();

        resolver.Should().BeOfType<StateMachineStaticServiceProviderResolver>();
        resolver.GetServiceProvider().GetRequiredService<object>().Should().BeSameAs(marker);
        root.GetRequiredService<object>().Should().BeSameAs(marker);
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, resolver.GetServiceProvider()).Should().BeTrue();
        }
    }

    [Fact]
    public void StateMachineServiceProviderResolverCreatesReactionScopeWhenScopeFactoryIsAvailable()
    {
        var services = new ServiceCollection();
        services.AddScoped<IStateMachineServiceProviderResolver, StateMachineServiceProviderResolver>();

        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var resolver = sp.GetRequiredService<IStateMachineServiceProviderResolver>();

        resolver.Should().BeOfType<StateMachineServiceProviderResolver>();
        ReferenceEquals(resolver.GetServiceProvider(), sp).Should().BeTrue();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, sp).Should().BeFalse();
            reactionSp.Should().NotBeSameAs(sp);
        }
    }

    [Fact]
    public void AddScopedStateMachineServiceProviderResolverRegistersScopedResolverThatCreatesChildScopes()
    {
        var services = new ServiceCollection();
        services.AddScopedStateMachineServiceProviderResolver();

        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var resolver = sp.GetRequiredService<IStateMachineServiceProviderResolver>();

        resolver.Should().BeOfType<StateMachineServiceProviderResolver>();
        ReferenceEquals(resolver.GetServiceProvider(), sp).Should().BeTrue();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, sp).Should().BeFalse();
        }
    }

    [Fact]
    public void AddSingletonStateMachineServiceProviderResolverRegistersStaticResolver()
    {
        var services = new ServiceCollection();
        services.AddSingletonStateMachineServiceProviderResolver();

        using var root = services.BuildServiceProvider();
        var resolver = root.GetRequiredService<IStateMachineServiceProviderResolver>();

        resolver.Should().BeOfType<StateMachineStaticServiceProviderResolver>();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, resolver.GetServiceProvider()).Should().BeTrue();
        }
    }
}
