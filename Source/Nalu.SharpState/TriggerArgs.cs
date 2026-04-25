using System.Diagnostics.CodeAnalysis;

namespace Nalu.SharpState;

/// <summary>
/// Lightweight value carrier for trigger arguments. It keeps the hot path allocation-free for the common
/// case where no external handlers need the boxed <see cref="object"/> array representation.
/// </summary>
public readonly struct TriggerArgs
{
    private readonly object? _arg0;
    private readonly object? _arg1;
    private readonly object? _arg2;

    private TriggerArgs(int count, object? arg0, object? arg1, object? arg2)
    {
        Count = count;
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
    }

    /// <summary>
    /// An empty argument list.
    /// </summary>
    public static TriggerArgs Empty => default;

    /// <summary>
    /// Number of captured trigger arguments.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets an argument by position.
    /// </summary>
    /// <param name="index">Zero-based argument index.</param>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <returns>The parameter value.</returns>
    /// <exception cref="IndexOutOfRangeException">The index is out of range.</exception>
    public T Get<T>(int index)
    {
        var i = index;
        if (i >= Count)
        {
            i = -1;
        }
        
        return i switch
        {
            0 => (T)_arg0!,
            1 => (T)_arg1!,
            2 => (T)_arg2!,
            _ => (T)ThrowIndexOutOfRangeException(index)
        };
    }

    [ExcludeFromCodeCoverage]
    private object ThrowIndexOutOfRangeException(int index)
        => throw new IndexOutOfRangeException($"Trigger argument index {index} is out of range for {Count} argument(s).");

    /// <summary>
    /// Materializes the arguments into a boxed array for public callbacks.
    /// </summary>
    public object?[] ToArray() => Count switch
    {
        0 => [],
        1 => [_arg0],
        2 => [_arg0, _arg1],
        3 => [_arg0, _arg1, _arg2],
        _ => ThrowUnsupportedParametersCount(),
    };

    [ExcludeFromCodeCoverage]
    private object?[] ThrowUnsupportedParametersCount()
        => throw new InvalidOperationException($"Unsupported trigger argument count '{Count}'.");

    /// <summary>
    /// Creates a one-argument payload.
    /// </summary>
    public static TriggerArgs From(object? arg0)
        => new(1, arg0, null, null);

    /// <summary>
    /// Creates a two-argument payload.
    /// </summary>
    public static TriggerArgs From(object? arg0, object? arg1)
        => new(2, arg0, arg1, null);

    /// <summary>
    /// Creates a three-argument payload.
    /// </summary>
    public static TriggerArgs From(object? arg0, object? arg1, object? arg2)
        => new(3, arg0, arg1, arg2);
}
