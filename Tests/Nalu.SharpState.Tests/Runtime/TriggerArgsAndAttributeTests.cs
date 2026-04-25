using System.Reflection;
using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class TriggerArgsAndAttributeTests
{
    [Fact]
    public void TriggerArgs_Get_throws_when_out_of_range()
    {
        var args = TriggerArgs.From(1);
        var act = () => args.Get<int>(1);
        act.Should().ThrowExactly<IndexOutOfRangeException>();
    }

    [Fact]
    public void TriggerArgs_Get_returns_typed_value_at_each_index()
    {
        var args = TriggerArgs.From(10, "b", 3.0);
        args.Get<int>(0).Should().Be(10);
        args.Get<string>(1).Should().Be("b");
        args.Get<double>(2).Should().Be(3.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TriggerArgs_ToArray_round_trips_arity(int arity)
    {
        var args = arity switch
        {
            0 => TriggerArgs.Empty,
            1 => TriggerArgs.From("a"),
            2 => TriggerArgs.From("a", "b"),
            3 => TriggerArgs.From("a", "b", "c"),
            _ => throw new InvalidOperationException()
        };
        args.Count.Should().Be(arity);
        object?[] expected = arity switch
        {
            0 => [],
            1 => ["a"],
            2 => ["a", "b"],
            3 => ["a", "b", "c"],
            _ => throw new InvalidOperationException()
        };
        args.ToArray().Should().Equal(expected);
    }

    [Fact]
    public void ToArray_throws_for_unsupported_count_via_reflection()
    {
        var ctor = typeof(TriggerArgs).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(int), typeof(object), typeof(object), typeof(object)],
            modifiers: null);
        ctor.Should().NotBeNull();
        var args = (TriggerArgs)ctor.Invoke([4, 0, 1, 2]);
        var act = args.ToArray;
        act.Should().Throw<InvalidOperationException>().WithMessage("*Unsupported*4*");
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
}
