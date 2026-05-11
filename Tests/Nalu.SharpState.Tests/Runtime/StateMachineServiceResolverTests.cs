using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateMachineServiceResolverTests
{
    private sealed class WrongTypeProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => new object();
    }

    private sealed class DictionaryProvider(Dictionary<Type, object?> services) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            services.TryGetValue(serviceType, out var value) ? value : null;
    }

    [Fact]
    public void Resolve_throws_argument_null_exception_when_provider_is_null()
    {
        var act = () => StateMachineServiceResolver.Resolve<int>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Resolve_throws_invalid_operation_exception_when_service_missing()
    {
        var provider = EmptyServiceProvider.Instance;

        var act = () => StateMachineServiceResolver.Resolve<int>(provider);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{typeof(int).FullName}'*");
    }

    [Fact]
    public void Resolve_returns_service_when_present_and_assignable()
    {
        var expected = new object();
        var provider = new DictionaryProvider(new Dictionary<Type, object?> { [typeof(object)] = expected });

        var result = StateMachineServiceResolver.Resolve<object>(provider);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void Resolve_throws_invalid_cast_exception_when_service_has_wrong_type()
    {
        var provider = new WrongTypeProvider();

        var act = () => StateMachineServiceResolver.Resolve<string>(provider);

        act.Should().Throw<InvalidCastException>();
    }
}
