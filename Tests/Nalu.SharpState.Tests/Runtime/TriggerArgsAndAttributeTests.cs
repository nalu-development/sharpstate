using System.Reflection;
using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class TriggerArgsAndAttributeTests
{
    [Fact]
    public void TriggerArgs_indexer_throws_when_out_of_range()
    {
        var args = TriggerArgs.From(1);
        var act = () => _ = args[1];
        act.Should().ThrowExactly<IndexOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TriggerArgs_ToArray_round_trips_arity(int arity)
    {
        var args = arity switch
        {
            0 => TriggerArgs.Empty,
            1 => TriggerArgs.From("a"),
            2 => TriggerArgs.From("a", "b"),
            3 => TriggerArgs.From("a", "b", "c"),
            4 => TriggerArgs.From("a", "b", "c", "d"),
            _ => throw new InvalidOperationException()
        };
        args.Count.Should().Be(arity);
        object?[] expected = arity switch
        {
            0 => [],
            1 => ["a"],
            2 => ["a", "b"],
            3 => ["a", "b", "c"],
            4 => ["a", "b", "c", "d"],
            _ => throw new InvalidOperationException()
        };
        args.ToArray().Should().Equal(expected);
    }

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

    [Fact]
    public void ToArray_supports_defensive_path_for_unsupported_counts_via_reflection()
    {
        var ctor = typeof(TriggerArgs).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(int), typeof(object), typeof(object), typeof(object), typeof(object)],
            modifiers: null);
        ctor.Should().NotBeNull();
        var args = (TriggerArgs)ctor.Invoke([5, 0, 1, 2, 3]);
        var act = args.ToArray;
        act.Should().Throw<InvalidOperationException>().WithMessage("*Unsupported*5*");
    }
}
