using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class TriggerArgsAndAttributeTests
{
    [Fact]
    public void StateDefinitionAttribute_Initial_round_trips()
    {
        new StateDefinitionAttribute { Initial = true }.Initial.Should().BeTrue();
    }

    [Fact]
    public void StateMachineDefinitionAttribute_default_ctor_uses_object_context()
    {
        new StateMachineDefinitionAttribute().ContextType.Should().Be(typeof(object));
    }

    [Fact]
    public void StateMachineDefinitionAttribute_stores_context_type()
    {
        new StateMachineDefinitionAttribute(typeof(TriggerArgsAndAttributeTests)).ContextType
            .Should().Be(typeof(TriggerArgsAndAttributeTests));
    }

    [Fact]
    public void SubStateMachineAttribute_stores_parent_object()
    {
        new SubStateMachineAttribute(FlatState.B).Parent.Should().Be(FlatState.B);
    }
}
