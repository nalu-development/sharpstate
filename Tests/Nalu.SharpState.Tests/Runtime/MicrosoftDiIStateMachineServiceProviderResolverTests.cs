using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Nalu.SharpState.Tests.Runtime;

public class MicrosoftDiIStateMachineServiceProviderResolverTests
{
    private sealed class TestOpenGenericResolver<T> : IStateMachineServiceProviderResolver<T>
    {
        private readonly T _instance;

        public TestOpenGenericResolver(T instance) => _instance = instance;

        public T GetServiceProvider() => _instance;

        public IDisposable CreateScopedServiceProvider(out T serviceProvider)
        {
            serviceProvider = _instance;
            return StateMachineReactiveNonOwningScope.Instance;
        }
    }

    [Fact]
    public void Microsoft_dependency_injection_resolves_AddScoped_closed_generic_implementation()
    {
        var services = new ServiceCollection();
        services.AddScoped<IStateMachineServiceProviderResolver<IServiceProvider>, TestOpenGenericResolver<IServiceProvider>>();

        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var resolver = sp.GetRequiredService<IStateMachineServiceProviderResolver<IServiceProvider>>();

        resolver.Should().BeOfType<TestOpenGenericResolver<IServiceProvider>>();
        ReferenceEquals(resolver.GetServiceProvider(), sp).Should().BeTrue();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, sp).Should().BeTrue();
        }
    }

    [Fact]
    public void Microsoft_dependency_injection_resolves_AddScoped_open_generic_implementation()
    {
        var services = new ServiceCollection();
        services.AddScoped(typeof(IStateMachineServiceProviderResolver<>), typeof(TestOpenGenericResolver<>));

        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var resolver = sp.GetRequiredService<IStateMachineServiceProviderResolver<IServiceProvider>>();

        resolver.Should().BeOfType<TestOpenGenericResolver<IServiceProvider>>();
        ReferenceEquals(resolver.GetServiceProvider(), sp).Should().BeTrue();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, sp).Should().BeTrue();
        }
    }

    [Fact]
    public void AddSingletonStateMachineServiceProviderResolver_registers_StateMachineStaticServiceProviderResolver()
    {
        var marker = new object();
        var services = new ServiceCollection();
        services.AddSingleton(_ => marker);
        services.AddSingletonStateMachineServiceProviderResolver();

        using var root = services.BuildServiceProvider();
        var resolver = root.GetRequiredService<IStateMachineServiceProviderResolver<IServiceProvider>>();

        resolver.Should().BeOfType<StateMachineStaticServiceProviderResolver>();
        resolver.GetServiceProvider().GetRequiredService<object>().Should().BeSameAs(marker);
        root.GetRequiredService<object>().Should().BeSameAs(marker);
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, resolver.GetServiceProvider()).Should().BeTrue();
        }
    }

    [Fact]
    public void AddScopedStateMachineServiceProviderResolver_registers_StateMachineServiceProviderResolver_per_scope()
    {
        var services = new ServiceCollection();
        services.AddScopedStateMachineServiceProviderResolver();

        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var resolver = sp.GetRequiredService<IStateMachineServiceProviderResolver<IServiceProvider>>();

        resolver.Should().BeOfType<StateMachineServiceProviderResolver>();
        ReferenceEquals(resolver.GetServiceProvider(), sp).Should().BeTrue();
        using (resolver.CreateScopedServiceProvider(out var reactionSp))
        {
            ReferenceEquals(reactionSp, sp).Should().BeFalse();
            reactionSp.Should().NotBeSameAs(sp);
        }
    }
}
