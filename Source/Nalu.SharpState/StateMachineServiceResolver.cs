namespace Nalu.SharpState;

/// <summary>
/// Resolves required callback services from an <see cref="IServiceProvider"/> without depending on DI extension methods.
/// Remains public because source-generated transitions in consuming assemblies invoke <see cref="Resolve{T}"/>.
/// </summary>
public static class StateMachineServiceResolver
{
    /// <summary>
    /// Gets a required service from <paramref name="serviceProvider"/>.
    /// </summary>
    /// <typeparam name="T">Service type to resolve.</typeparam>
    /// <param name="serviceProvider">Provider used for the current transition or reaction.</param>
    /// <returns>The resolved service.</returns>
    /// <exception cref="InvalidOperationException">The provider returned <c>null</c>.</exception>
    public static T Resolve<T>(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var service = serviceProvider.GetService(typeof(T));
        if (service is null)
        {
            throw new InvalidOperationException($"Required service '{typeof(T).FullName}' was not found.");
        }

        return (T)service;
    }
}
