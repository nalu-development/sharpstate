using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class DefinitionValidationTests
{
    private static StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor> Build(
        Dictionary<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>> map)
    {
        var forDef = new InternalEnumMap<HierState, IStateConfiguration<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>();
        foreach (var kv in map)
        {
            forDef[kv.Key] = kv.Value;
        }

        return new StateMachineDefinition<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>(forDef);
    }

    [Fact]
    public void ParentWithoutMatchingAsStateMachineThrows()
    {
        var map = new Dictionary<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>
        {
            [HierState.Idle] = new(),
            [HierState.Connected] = new(),
            [HierState.Authenticating] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>().Parent(HierState.Connected),
            [HierState.Authenticated] = new(),
            [HierState.Outside] = new()
        };

        var act = () => Build(map);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SubStateMachine*");
    }

    [Fact]
    public void AsStateMachineWhoseInitialChildDoesNotClaimParentThrows()
    {
        var map = new Dictionary<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>
        {
            [HierState.Idle] = new(),
            [HierState.Connected] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>().AsStateMachine(HierState.Authenticating),
            [HierState.Authenticating] = new(),
            [HierState.Authenticated] = new(),
            [HierState.Outside] = new()
        };

        var act = () => Build(map);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SubStateMachine*");
    }

    [Fact]
    public void MultiParentSingleConfiguratorThrowsOnSecondParentCall()
    {
        var act = () => new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
            .Parent(HierState.Connected)
            .Parent(HierState.Outside);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DoubleAsStateMachineThrows()
    {
        var act = () => new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
            .AsStateMachine(HierState.Authenticating)
            .AsStateMachine(HierState.Authenticated);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ParentReferringToUnknownStateThrows()
    {
        var map = new Dictionary<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>
        {
            [HierState.Idle] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>().Parent(HierState.Outside),
            [HierState.Connected] = new()
        };

        var act = () => Build(map);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CycleInStateHierarchyThrows()
    {
        var map = new Dictionary<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>
        {
            [HierState.Idle] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
                .Parent(HierState.Outside)
                .AsStateMachine(HierState.Connected),
            [HierState.Connected] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
                .Parent(HierState.Idle)
                .AsStateMachine(HierState.Authenticated),
            [HierState.Authenticating] = new(),
            [HierState.Authenticated] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
                .Parent(HierState.Connected)
                .AsStateMachine(HierState.Outside),
            [HierState.Outside] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
                .Parent(HierState.Authenticated)
                .AsStateMachine(HierState.Idle)
        };

        var act = () => Build(map);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cycle detected*");
    }

    [Fact]
    public void AncestorsOfReturnsImmediateParentFirst()
    {
        var definition = HierarchyTests.CreateStandardHierarchy();

        definition.AncestorsOf(HierState.Authenticated)
            .Should()
            .Equal(HierState.Connected);
    }

    [Fact]
    public void LowestCommonAncestorReturnsNearestCommonComposite()
    {
        var definition = HierarchyTests.CreateStandardHierarchy();

        definition.LowestCommonAncestor(HierState.Authenticating, HierState.Authenticated)
            .Should()
            .Be(HierState.Connected);
    }

    [Fact]
    public void LowestCommonAncestorOfIdenticalChildStatesReturnsThatState()
    {
        var definition = HierarchyTests.CreateStandardHierarchy();

        definition.LowestCommonAncestor(HierState.Authenticated, HierState.Authenticated)
            .Should()
            .Be(HierState.Authenticated);
    }

    [Fact]
    public void InitialChildNotInDefinitionThrows()
    {
        var map = new Dictionary<HierState, TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>>
        {
            [HierState.Idle] = new(),
            [HierState.Connected] = new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
                .AsStateMachine(HierState.Authenticating),
            [HierState.Authenticated] = new(),
            [HierState.Outside] = new()
        };

        // Authenticating is declared as the composite initial child of Connected, but is not present in the map.
        var act = () => Build(map);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'Connected'*initial child*'Authenticating'*not registered*");
    }

    [Fact]
    public void SecondWhenEnteringThrows()
    {
        var act = () => new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
            .WhenEntering((_, _) => { })
            .WhenEntering((_, _) => { });
        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void SecondWhenExitingThrows()
    {
        var act = () => new TestStateConfigurator<TestContext, IServiceProvider, HierState, HierTrigger, TestActor>()
            .WhenExiting((_, _) => { })
            .WhenExiting((_, _) => { });
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
