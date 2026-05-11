namespace Nalu.SharpState.Tests.Runtime;

internal sealed class TestTriggerArgs : IServiceProvider
{
    private readonly object? _arg0;
    private readonly object? _arg1;
    private readonly object? _arg2;

    private TestTriggerArgs(int count, object? arg0, object? arg1, object? arg2)
    {
        Count = count;
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
    }

    public static TestTriggerArgs Empty { get; } = new(0, null, null, null);

    public int Count { get; }

    public T Get<T>(int index)
        => index switch
        {
            0 when Count > 0 => (T)_arg0!,
            1 when Count > 1 => (T)_arg1!,
            2 when Count > 2 => (T)_arg2!,
            _ => throw new IndexOutOfRangeException()
        };

    public static TestTriggerArgs From(object? arg0) => new(1, arg0, null, null);

    public static TestTriggerArgs From(object? arg0, object? arg1) => new(2, arg0, arg1, null);

    public static TestTriggerArgs From(object? arg0, object? arg1, object? arg2) => new(3, arg0, arg1, arg2);

    public object? GetService(Type serviceType) => null;
}

internal static class TestTriggerArgsExtensions
{
    public static T Get<T>(this IServiceProvider args, int index)
        => ((TestTriggerArgs)args).Get<T>(index);
}
