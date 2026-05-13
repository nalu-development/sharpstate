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
    public void ResolveThrowsArgumentNullExceptionWhenProviderIsNull()
    {
        var act = () => StateMachineServiceResolver.Resolve<int>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void ResolveThrowsInvalidOperationExceptionWhenServiceMissing()
    {
        var provider = EmptyServiceProvider.Instance;

        var act = () => StateMachineServiceResolver.Resolve<int>(provider);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{typeof(int).FullName}'*");
    }

    [Fact]
    public void ResolveReturnsServiceWhenPresentAndAssignable()
    {
        var expected = new object();
        var provider = new DictionaryProvider(new Dictionary<Type, object?> { [typeof(object)] = expected });

        var result = StateMachineServiceResolver.Resolve<object>(provider);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void ResolveThrowsInvalidCastExceptionWhenServiceHasWrongType()
    {
        var provider = new WrongTypeProvider();

        var act = () => StateMachineServiceResolver.Resolve<string>(provider);

        act.Should().Throw<InvalidCastException>();
    }
}
