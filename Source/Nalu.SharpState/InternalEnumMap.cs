using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nalu.SharpState;

static file class EnumKeys<TEnum>
    where TEnum : struct, Enum
{
    public static readonly TEnum[] Values = Enum.GetValues<TEnum>();
    // ReSharper disable once StaticMemberInGenericType
    public static readonly int Length = Convert.ToInt32(Values.Max()) + 1;
}

/// <summary>
/// A simple map that uses enum keys.
/// </summary>
public class InternalEnumMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : struct, Enum
{
    private struct Entry
    {
        public TKey Key;
        public TValue Value;
        public bool IsSet;
    }

    private readonly Entry[] _values = new Entry[EnumKeys<TKey>.Length];

    /// <summary>
    /// Creates a new instance of <see cref="InternalEnumMap{TKey,TValue}"/>.
    /// </summary>
    public InternalEnumMap()
    {
        var keys = EnumKeys<TKey>.Values;
        var keysLength = keys.Length;
        for (var index = 0; index < keysLength; index++)
        {
            var key = keys[index];
            ref var entry = ref _values[Unsafe.As<TKey, int>(ref key)];
            entry.Key = key;
        }
    }

    /// <summary>
    /// Creates a copy of <paramref name="source"/> with the same keys and values.
    /// </summary>
    public InternalEnumMap(InternalEnumMap<TKey, TValue> source)
        : this()
    {
        ArgumentNullException.ThrowIfNull(source);
        foreach (var kvp in source)
        {
            this[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified state.
    /// </summary>
    /// <param name="key"></param>
    public TValue this[TKey key]
    {
        get
        {
            ref var entry = ref _values[Unsafe.As<TKey, int>(ref key)];
            return !entry.IsSet ? throw new KeyNotFoundException($"The state '{key}' is not contained in the map.") : entry.Value;
        }
        set
        {
            ref var entry = ref _values[Unsafe.As<TKey, int>(ref key)];
            entry.Value = value;
            entry.IsSet = true;
        }
    }

    /// <summary>
    /// Gets the keys contained in the map.
    /// </summary>
    public IEnumerable<TKey> Keys => GetKeys();

    /// <summary>
    /// Gets the values contained in the map (only entries that are set).
    /// </summary>
    public IEnumerable<TValue> Values => GetValues();

    /// <summary>
    /// Determines whether the map contains the specified state.
    /// </summary>
    /// <param name="key">The state to look up.</param>
    /// <returns>Whether the state is contained in the map.</returns>
    public bool ContainsKey(TKey key) => _values[Unsafe.As<TKey, int>(ref key)].IsSet;

    /// <summary>
    /// Tries to get the value associated with the specified state.
    /// </summary>
    /// <param name="key">The state to look up.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified state, if the state is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns>A boolean indicating whether the state was found.</returns>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        var index = Unsafe.As<TKey, int>(ref key);
        ref var entry = ref _values[index];
        value = entry.Value;
        return entry.IsSet;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var values = _values;
        var valuesLength = values.Length;
        for (var i = 0; i < valuesLength; ++i)
        {
            ref var entry = ref _values[i];
            if (!entry.IsSet)
            {
                continue;
            }
        
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    private IEnumerable<TKey> GetKeys()
    {
        var values = _values;
        var valuesLength = values.Length;
        for (var i = 0; i < valuesLength; ++i)
        {
            ref var entry = ref _values[i];
            if (entry.IsSet)
            {
                yield return entry.Key;
            }
        }
    }

    private IEnumerable<TValue> GetValues()
    {
        var values = _values;
        var valuesLength = values.Length;
        for (var i = 0; i < valuesLength; ++i)
        {
            ref var entry = ref _values[i];
            if (entry.IsSet)
            {
                yield return entry.Value;
            }
        }
    }
}
