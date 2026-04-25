using System.Collections;
using FluentAssertions;

namespace Nalu.SharpState.Tests.Runtime;

public class InternalEnumMapTests
{
    [Fact]
    public void Get_throws_for_unset_key()
    {
        var map = new InternalEnumMap<FlatState, string>();
        map[FlatState.A] = "a";
        var act = () => _ = map[FlatState.C];
        act.Should().ThrowExactly<KeyNotFoundException>();
    }

    [Fact]
    public void Copy_constructor_throws_when_source_is_null()
    {
        var act = () => new InternalEnumMap<FlatState, int>(null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Non_generic_IEnumerable_GetEnumerator_enumerates_pairs()
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
