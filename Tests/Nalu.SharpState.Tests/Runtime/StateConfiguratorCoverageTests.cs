using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class StateConfiguratorCoverageTests
{
    private sealed class ExposedConfigurator :
        StateConfigurator<TestContext, IServiceProvider, FlatState, FlatTrigger, TestActor>
    {
        public void Add(FlatTrigger trigger, IReadOnlyList<Transition<TestContext, IServiceProvider, FlatState, TestActor>> transitions)
            => AddTransitions(trigger, transitions);
    }

    [Fact]
    public void AddTransitionsWithEmptyListReturnsImmediately()
    {
        var c = new ExposedConfigurator();
        c.Add(FlatTrigger.Go, Array.Empty<Transition<TestContext, IServiceProvider, FlatState, TestActor>>());
    }

    [Fact]
    public void AddTransitionsAppendsToSameTriggerBucket()
    {
        var c = new ExposedConfigurator();
        var t1 = new Transition<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.B, null, false, null, null, null, null);
        var t2 = new Transition<TestContext, IServiceProvider, FlatState, TestActor>(FlatState.C, null, false, null, null, null, null);
        c.Add(FlatTrigger.Go, [t1]);
        c.Add(FlatTrigger.Go, [t2]);
        c.TryGetTransitions(FlatTrigger.Go, out var list);
        list.Should().HaveCount(2);
    }
}
