using System.Collections;
using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class InternalEnumMapTests
{
    [Fact]
    public void GetThrowsForUnsetKey()
    {
        var map = new InternalEnumMap<FlatState, string>();
        map[FlatState.A] = "a";
        var act = () => _ = map[FlatState.C];
        act.Should().ThrowExactly<KeyNotFoundException>();
    }

    [Fact]
    public void CopyConstructorThrowsWhenSourceIsNull()
    {
        var act = () => new InternalEnumMap<FlatState, int>(null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NonGenericIEnumerableGetEnumeratorEnumeratesPairs()
    {
        var map = new InternalEnumMap<FlatState, string>();
        map[FlatState.A] = "x";
        map[FlatState.C] = "z";
        var list = new List<object?>();
        var en = ((IEnumerable)map).GetEnumerator();
        while (en.MoveNext())
        {
            list.Add(en.Current);
        }

        list.Should().HaveCount(2);
    }
}
