namespace Nalu.SharpState;

/// <summary>
/// Marks a partial class as a state machine definition operating over a given context type.
/// The source generator scans the class for <see cref="StateTriggerDefinitionAttribute"/>-annotated
/// partial methods (to discover triggers) and <see cref="StateDefinitionAttribute"/>-annotated static
/// properties (to discover states) and emits the matching state/trigger enums, configurator, and
/// <c>Instance</c> runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateMachineDefinitionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineDefinitionAttribute"/>.
    /// </summary>
    /// <param name="contextType">The type of the context on which the machine operates.</param>
    /// <param name="serviceProviderType">The type used as <c>TServiceProvider</c> for this machine (for example <see cref="IServiceProvider"/>).</param>
    public StateMachineDefinitionAttribute(Type contextType, Type serviceProviderType)
    {
        ContextType = contextType;
        ServiceProviderType = serviceProviderType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineDefinitionAttribute"/> with
    /// <see cref="IServiceProvider"/> as the service provider type.
    /// </summary>
    /// <param name="contextType">The type of the context on which the machine operates.</param>
    public StateMachineDefinitionAttribute(Type contextType)
        : this(contextType, typeof(IServiceProvider))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineDefinitionAttribute"/> with
    /// <see cref="object"/> as the context type and <see cref="IServiceProvider"/> as the service provider type.
    /// </summary>
    public StateMachineDefinitionAttribute()
        : this(typeof(object), typeof(IServiceProvider))
    { }

    /// <summary>
    /// The type of the context on which the machine operates.
    /// </summary>
    public Type ContextType { get; }

    /// <summary>
    /// The type used as <c>TServiceProvider</c> for guards, actions, and reactions (see <see cref="IStateMachineServiceProviderResolver{TServiceProvider}"/>).
    /// </summary>
    public Type ServiceProviderType { get; }
}
