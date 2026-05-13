using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class TriggerArgsAndAttributeTests
{
    [Fact]
    public void StateDefinitionAttributeInitialRoundTrips()
    {
        new StateDefinitionAttribute { Initial = true }.Initial.Should().BeTrue();
    }

    [Fact]
    public void StateMachineDefinitionAttributeDefaultCtorUsesObjectContext()
    {
        new StateMachineDefinitionAttribute().ContextType.Should().Be(typeof(object));
    }

    [Fact]
    public void StateMachineDefinitionAttributeStoresContextType()
    {
        new StateMachineDefinitionAttribute(typeof(TriggerArgsAndAttributeTests)).ContextType
            .Should().Be(typeof(TriggerArgsAndAttributeTests));
    }

    [Fact]
    public void SubStateMachineAttributeStoresParentObject()
    {
        new SubStateMachineAttribute(FlatState.B).Parent.Should().Be(FlatState.B);
    }
}
