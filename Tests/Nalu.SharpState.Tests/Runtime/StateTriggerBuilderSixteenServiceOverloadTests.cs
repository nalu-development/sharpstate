using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
// ReSharper disable NotAccessedPositionalProperty.Local

namespace Nalu.SharpState.Tests.Runtime;

/// <summary>
/// Exercises each DI arity T1..Tn (n = 1..16) on <see cref="StateTriggerBuilder{TContext, TMachineArgs, TState, TActor}"/>
/// and <see cref="StateTriggerBuilder{TContext, TMachineArgs, TState, TActor, TArgs}"/> via reflection on
/// <see cref="MethodInfo.MakeGenericMethod(Type[])"/>.
/// </summary>
public class StateTriggerBuilderSixteenServiceOverloadTests
{
    private sealed class DictionaryProvider(Dictionary<Type, object?> services) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            services.TryGetValue(serviceType, out var value) ? value : null;
    }

    private static readonly Assembly _sharpStateAssembly = typeof(StateTriggerBuilder<,,,>).Assembly;

    private static readonly Type[] _diTypes =
    [
        typeof(DiMarker01),
        typeof(DiMarker02),
        typeof(DiMarker03),
        typeof(DiMarker04),
        typeof(DiMarker05),
        typeof(DiMarker06),
        typeof(DiMarker07),
        typeof(DiMarker08),
        typeof(DiMarker09),
        typeof(DiMarker10),
        typeof(DiMarker11),
        typeof(DiMarker12),
        typeof(DiMarker13),
        typeof(DiMarker14),
        typeof(DiMarker15),
        typeof(DiMarker16)
    ];

    private static readonly object?[] _diInstances =
    [
        new DiMarker01(),
        new DiMarker02(),
        new DiMarker03(),
        new DiMarker04(),
        new DiMarker05(),
        new DiMarker06(),
        new DiMarker07(),
        new DiMarker08(),
        new DiMarker09(),
        new DiMarker10(),
        new DiMarker11(),
        new DiMarker12(),
        new DiMarker13(),
        new DiMarker14(),
        new DiMarker15(),
        new DiMarker16()
    ];

#pragma warning disable CA1852 // records are types for DI markers
    private sealed record DiMarker01(int Ordinal = 1);

    private sealed record DiMarker02(int Ordinal = 2);

    private sealed record DiMarker03(int Ordinal = 3);

    private sealed record DiMarker04(int Ordinal = 4);

    private sealed record DiMarker05(int Ordinal = 5);

    private sealed record DiMarker06(int Ordinal = 6);

    private sealed record DiMarker07(int Ordinal = 7);

    private sealed record DiMarker08(int Ordinal = 8);

    private sealed record DiMarker09(int Ordinal = 9);

    private sealed record DiMarker10(int Ordinal = 10);

    private sealed record DiMarker11(int Ordinal = 11);

    private sealed record DiMarker12(int Ordinal = 12);

    private sealed record DiMarker13(int Ordinal = 13);

    private sealed record DiMarker14(int Ordinal = 14);

    private sealed record DiMarker15(int Ordinal = 15);

    private sealed record DiMarker16(int Ordinal = 16);
#pragma warning restore CA1852

    private static int Triangular(int n) => n * (n + 1) / 2;

    /// <summary>
    /// Open generic types in this assembly use CLR metadata names like <c>StateGuard`3</c>.
    /// We must not use <c>nameof(StateGuard)</c> as a type reference (many arities share one C# name).
    /// </summary>
    private static Type GetOpenGenericTypeByClrName(string baseName, int typeParameterCount)
    {
        var expected = baseName + "`" + typeParameterCount;
        return _sharpStateAssembly.GetTypes().Single(t => t.IsGenericTypeDefinition && t.Name == expected);
    }

    private static IServiceProvider CreateProvider(int n)
    {
        var map = new Dictionary<Type, object?>(n);
        for (var i = 0; i < n; i++)
        {
            map[_diTypes[i]] = _diInstances[i];
        }

        return new DictionaryProvider(map);
    }

    private static MethodInfo GetGenericInstanceMethod(Type builderType, string name, int genericArgCount) =>
        builderType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == name && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == genericArgCount);

    private static PropertyInfo OrdinalProperty(Type diType) =>
        diType.GetProperty(nameof(DiMarker01.Ordinal)) ?? throw new InvalidOperationException(diType.Name);

    private static Expression OrdinalSumExpression(Type[] diTypes, ParameterExpression[] serviceParams)
    {
        Expression sum = Expression.Property(serviceParams[0], OrdinalProperty(diTypes[0]));
        for (var i = 1; i < diTypes.Length; i++)
        {
            sum = Expression.Add(sum, Expression.Property(serviceParams[i], OrdinalProperty(diTypes[i])));
        }

        return sum;
    }

    private static Delegate BuildParameterlessGuard(int n, Type[] diTypes)
    {
        var open = GetOpenGenericTypeByClrName("StateTriggerParameterlessGuard", n + 1);
        var closed = open.MakeGenericType([typeof(TestContext), .. diTypes]);
        var ctx = Expression.Parameter(typeof(TestContext), "ctx");
        var services = diTypes.Select((t, i) => Expression.Parameter(t, $"s{i}")).ToArray();
        var body = Expression.Equal(OrdinalSumExpression(diTypes, services), Expression.Constant(Triangular(n)));
        return Expression.Lambda(closed, body, [ctx, .. services]).Compile();
    }

    private static Delegate BuildArgsGuard(int n, Type[] diTypes)
    {
        var open = GetOpenGenericTypeByClrName("StateGuard", n + 2);
        var closed = open.MakeGenericType([typeof(TestContext), typeof(TestTriggerArgs), .. diTypes]);
        var ctx = Expression.Parameter(typeof(TestContext), "ctx");
        var args = Expression.Parameter(typeof(TestTriggerArgs), "args");
        var services = diTypes.Select((t, i) => Expression.Parameter(t, $"s{i}")).ToArray();
        var body = Expression.Equal(OrdinalSumExpression(diTypes, services), Expression.Constant(Triangular(n)));
        return Expression.Lambda(closed, body, [ctx, args, .. services]).Compile();
    }

    private static Delegate BuildParameterlessTarget(int n, Type[] diTypes)
    {
        var open = GetOpenGenericTypeByClrName("StateTriggerParameterlessTargetSelector", n + 2);
        var closed = open.MakeGenericType([typeof(TestContext), .. diTypes, typeof(FlatState)]);
        var ctx = Expression.Parameter(typeof(TestContext), "ctx");
        var services = diTypes.Select((t, i) => Expression.Parameter(t, $"s{i}")).ToArray();
        var sum = OrdinalSumExpression(diTypes, services);
        var body = Expression.Condition(
            Expression.Equal(sum, Expression.Constant(Triangular(n))),
            Expression.Constant(FlatState.B),
            Expression.Constant(FlatState.C));
        return Expression.Lambda(closed, body, [ctx, .. services]).Compile();
    }

    private static Delegate BuildArgsTarget(int n, Type[] diTypes)
    {
        var open = GetOpenGenericTypeByClrName("StateTargetSelector", n + 3);
        var closed = open.MakeGenericType([typeof(TestContext), typeof(TestTriggerArgs), .. diTypes, typeof(FlatState)]);
        var ctx = Expression.Parameter(typeof(TestContext), "ctx");
        var args = Expression.Parameter(typeof(TestTriggerArgs), "args");
        var services = diTypes.Select((t, i) => Expression.Parameter(t, $"s{i}")).ToArray();
        var sum = OrdinalSumExpression(diTypes, services);
        var body = Expression.Condition(
            Expression.Equal(sum, Expression.Constant(Triangular(n))),
            Expression.Constant(FlatState.B),
            Expression.Constant(FlatState.C));
        return Expression.Lambda(closed, body, [ctx, args, .. services]).Compile();
    }

    private static void AddInvokeLogIfMatch(TestContext ctx, object?[] svcs, int expected, string tag)
    {
        var sum = 0;
        foreach (var s in svcs)
        {
            sum += (int)OrdinalProperty(s!.GetType()).GetValue(s)!;
        }

        if (sum == expected)
        {
            ctx.Log.Add(tag);
        }
    }

    private static Delegate BuildParameterlessInvoke(int n, Type[] diTypes, string tag)
    {
        var open = GetOpenGenericTypeByClrName("StateLifecycleAction", n + 1);
        var closed = open.MakeGenericType([typeof(TestContext), .. diTypes]);
        var ctx = Expression.Parameter(typeof(TestContext), "ctx");
        var services = diTypes.Select((t, i) => Expression.Parameter(t, $"s{i}")).ToArray();
        var arrVar = Expression.Variable(typeof(object[]), "svc");
        var assignArr = Expression.Assign(arrVar, Expression.NewArrayBounds(typeof(object), Expression.Constant(n)));
        var stores = new Expression[n];
        for (var i = 0; i < n; i++)
        {
            stores[i] = Expression.Assign(
                Expression.ArrayAccess(arrVar, Expression.Constant(i)),
                Expression.Convert(services[i], typeof(object)));
        }

        var call = Expression.Call(
            typeof(StateTriggerBuilderSixteenServiceOverloadTests),
            nameof(AddInvokeLogIfMatch),
            Type.EmptyTypes,
            ctx,
            arrVar,
            Expression.Constant(Triangular(n)),
            Expression.Constant(tag));
        var block = Expression.Block(
            [arrVar],
            new[] { assignArr }.Concat(stores).Concat(new[] { call }).ToArray());
        return Expression.Lambda(closed, block, [ctx, .. services]).Compile();
    }

    private static Delegate BuildArgsInvoke(int n, Type[] diTypes, string tag)
    {
        var open = GetOpenGenericTypeByClrName("StateAction", n + 2);
        var closed = open.MakeGenericType([typeof(TestContext), typeof(TestTriggerArgs), .. diTypes]);
        var ctx = Expression.Parameter(typeof(TestContext), "ctx");
        var args = Expression.Parameter(typeof(TestTriggerArgs), "args");
        var services = diTypes.Select((t, i) => Expression.Parameter(t, $"s{i}")).ToArray();
        var arrVar = Expression.Variable(typeof(object[]), "svc");
        var assignArr = Expression.Assign(arrVar, Expression.NewArrayBounds(typeof(object), Expression.Constant(n)));
        var stores = new Expression[n];
        for (var i = 0; i < n; i++)
        {
            stores[i] = Expression.Assign(
                Expression.ArrayAccess(arrVar, Expression.Constant(i)),
                Expression.Convert(services[i], typeof(object)));
        }

        var call = Expression.Call(
            typeof(StateTriggerBuilderSixteenServiceOverloadTests),
            nameof(AddInvokeLogIfMatch),
            Type.EmptyTypes,
            ctx,
            arrVar,
            Expression.Constant(Triangular(n)),
            Expression.Constant(tag));
        var block = Expression.Block(
            [arrVar],
            new[] { assignArr }.Concat(stores).Concat(new[] { call }).ToArray());
        return Expression.Lambda(closed, block, [ctx, args, .. services]).Compile();
    }

    private static ValueTask LogIfOrdinalSum(TestContext ctx, object?[] svcs, int expected, string tag)
    {
        var sum = 0;
        foreach (var s in svcs)
        {
            sum += (int)OrdinalProperty(s!.GetType()).GetValue(s)!;
        }

        if (sum == expected)
        {
            ctx.Log.Add(tag);
        }

        return default;
    }

    private static Delegate BuildParameterlessReaction(int n, Type[] diTypes, string tag)
    {
        var closed = GetOpenGenericTypeByClrName("StateTriggerParameterlessReaction", n + 2)
            .MakeGenericType([typeof(TestActor), typeof(TestContext), .. diTypes]);
        var paramTypes = new[] { typeof(TestActor), typeof(TestContext) }.Concat(diTypes).ToArray();
        var dm = new DynamicMethod("React" + n, typeof(ValueTask), paramTypes, typeof(StateTriggerBuilderSixteenServiceOverloadTests).Module, skipVisibility: true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < n; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 2);
            il.Emit(OpCodes.Stelem_Ref);
        }

        il.Emit(OpCodes.Ldc_I4, Triangular(n));
        il.Emit(OpCodes.Ldstr, tag);
        il.Emit(OpCodes.Call, typeof(StateTriggerBuilderSixteenServiceOverloadTests).GetMethod(nameof(LogIfOrdinalSum), BindingFlags.NonPublic | BindingFlags.Static)!);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate(closed);
    }

    private static Delegate BuildArgsReaction(int n, Type[] diTypes, string tag)
    {
        var closed = GetOpenGenericTypeByClrName("StateReaction", n + 3)
            .MakeGenericType([typeof(TestActor), typeof(TestContext), typeof(TestTriggerArgs), .. diTypes]);
        var paramTypes = new[] { typeof(TestActor), typeof(TestContext), typeof(TestTriggerArgs) }.Concat(diTypes).ToArray();
        var dm = new DynamicMethod("ReactArgs" + n, typeof(ValueTask), paramTypes, typeof(StateTriggerBuilderSixteenServiceOverloadTests).Module, skipVisibility: true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < n; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 3);
            il.Emit(OpCodes.Stelem_Ref);
        }

        il.Emit(OpCodes.Ldc_I4, Triangular(n));
        il.Emit(OpCodes.Ldstr, tag);
        il.Emit(OpCodes.Call, typeof(StateTriggerBuilderSixteenServiceOverloadTests).GetMethod(nameof(LogIfOrdinalSum), BindingFlags.NonPublic | BindingFlags.Static)!);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate(closed);
    }

    private static void ChainParameterlessBuilder(object builder, int n, Type[] diTypes)
    {
        var t = builder.GetType();
        GetGenericInstanceMethod(t, nameof(IStateTriggerBuilder<TestContext, FlatState, TestActor>.When), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildParameterlessGuard(n, diTypes), null]);
        GetGenericInstanceMethod(t, nameof(IStateTriggerBuilder<TestContext, FlatState, TestActor>.TransitionTo), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildParameterlessTarget(n, diTypes), Array.Empty<(FlatState, string)>()]);
        GetGenericInstanceMethod(t, nameof(IStateTransitionBuilder<TestContext, FlatState, TestActor>.Invoke), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildParameterlessInvoke(n, diTypes, "invoke")]);
        GetGenericInstanceMethod(t, nameof(IStateTransitionBuilder<TestContext, FlatState, TestActor>.ReactAsync), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildParameterlessReaction(n, diTypes, "react")]);
    }

    private static void ChainArgsBuilder(object builder, int n, Type[] diTypes)
    {
        var t = builder.GetType();
        GetGenericInstanceMethod(t, nameof(IStateTriggerArgsBuilder<TestContext, FlatState, TestActor, TestTriggerArgs>.When), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildArgsGuard(n, diTypes), null]);
        GetGenericInstanceMethod(t, nameof(IStateTriggerArgsBuilder<TestContext, FlatState, TestActor, TestTriggerArgs>.TransitionTo), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildArgsTarget(n, diTypes), Array.Empty<(FlatState, string)>()]);
        GetGenericInstanceMethod(t, nameof(IStateTransitionArgsBuilder<TestContext, FlatState, TestActor, TestTriggerArgs>.Invoke), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildArgsInvoke(n, diTypes, "invoke-args")]);
        GetGenericInstanceMethod(t, nameof(IStateTransitionArgsBuilder<TestContext, FlatState, TestActor, TestTriggerArgs>.ReactAsync), n)
            .MakeGenericMethod(diTypes)
            .Invoke(builder, [BuildArgsReaction(n, diTypes, "react-args")]);
    }

    [Theory(DisplayName = "Parameterless trigger builder resolves T1..Tn for each arity")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    public async Task ParameterlessBuilderDiOverloads(int n)
    {
        var diTypes = _diTypes.Take(n).ToArray();
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor>();
        ChainParameterlessBuilder(builder, n, diTypes);
        builder.Validate();

        var sp = CreateProvider(n);
        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();

        transition.Guard!(context, sp, TestTriggerArgs.Empty).Should().BeTrue();
        transition.SyncAction!(context, sp, TestTriggerArgs.Empty);
        context.Log.Should().Equal("invoke");
        await transition.ReactionAsync!(new TestActor(), context, sp, TestTriggerArgs.Empty);
        context.Log.Should().Equal("invoke", "react");
        transition.TargetSelector!(context, sp, TestTriggerArgs.Empty).Should().Be(FlatState.B);
    }

    [Theory(DisplayName = "Args trigger builder resolves T1..Tn for each arity")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    public async Task ArgsBuilderDiOverloads(int n)
    {
        var diTypes = _diTypes.Take(n).ToArray();
        var builder = new StateTriggerBuilder<TestContext, TestTriggerArgs, FlatState, TestActor, TestTriggerArgs>();
        ChainArgsBuilder(builder, n, diTypes);
        builder.Validate();

        var sp = CreateProvider(n);
        var transition = builder.BuildTransitions()[0];
        var context = new TestContext();
        var args = TestTriggerArgs.From(7);

        transition.Guard!(context, sp, args).Should().BeTrue();
        transition.SyncAction!(context, sp, args);
        context.Log.Should().Equal("invoke-args");
        await transition.ReactionAsync!(new TestActor(), context, sp, args);
        context.Log.Should().Equal("invoke-args", "react-args");
        transition.TargetSelector!(context, sp, args).Should().Be(FlatState.B);
    }
}
