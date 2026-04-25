#if !NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>Polyfill for C# 9 init-only properties on netstandard2.0.</summary>
[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
internal static class IsExternalInit;
#endif
