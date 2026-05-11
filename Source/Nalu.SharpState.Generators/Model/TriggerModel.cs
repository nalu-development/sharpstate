namespace Nalu.SharpState.Generators.Model;

/// <summary>
/// Equatable DTO describing one trigger declared with <c>[StateTriggerDefinition]</c>.
/// </summary>
/// <param name="Name">The trigger method name (also the Trigger enum member).</param>
/// <param name="Parameters">The parameter list of the trigger method.</param>
/// <param name="AccessibilityKeyword"><c>public</c>, <c>private</c>, … — must match the declaring partial for emitting the implementing partial.</param>
/// <param name="IsStatic"><c>true</c> when the declaring trigger is <c>static</c>.</param>
/// <param name="DocumentationCommentId">Documentation comment target used for generated <c>inheritdoc</c> comments.</param>
internal readonly record struct TriggerModel(
    string Name,
    EquatableArray<ParameterModel> Parameters,
    string AccessibilityKeyword,
    bool IsStatic,
    string? DocumentationCommentId);
