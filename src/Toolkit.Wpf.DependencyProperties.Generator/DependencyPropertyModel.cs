using Microsoft.CodeAnalysis;

namespace Toolkit.Wpf.DependencyProperties.Generator;

/// <summary>
/// Describes how the generated registration obtains the change callback of a dependency property.
/// </summary>
internal enum CallbackKind
{
    /// <summary>
    /// The property has no callback; the registration passes null.
    /// </summary>
    None = 0,

    /// <summary>
    /// The type declares the callback as a static method taking the WPF event arguments.
    /// </summary>
    StaticMethod = 1,

    /// <summary>
    /// The type implements the generated partial hook that receives the new value.
    /// </summary>
    NewValueHook = 2,

    /// <summary>
    /// The type implements the generated partial hook that receives the old and the new value.
    /// </summary>
    ValuesHook = 3,
}

// The explicit constructor keeps the documented parameters and the params array; a primary
// constructor would move them into positional parameters the documentation cannot name.
#pragma warning disable IDE0290

/// <summary>
/// Describes a diagnostic that the generator reported while it read the annotated type.
/// </summary>
internal sealed class DiagnosticInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticInfo"/> class.
    /// </summary>
    /// <param name="descriptor">The reported diagnostic.</param>
    /// <param name="location">The source location of the reported member.</param>
    /// <param name="arguments">The message arguments of the diagnostic.</param>
    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
    {
        Descriptor = descriptor;
        Location = location;
        Arguments = arguments;
    }

    /// <summary>Gets the reported diagnostic.</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>Gets the source location of the reported member.</summary>
    public Location? Location { get; }

    /// <summary>Gets the message arguments of the diagnostic.</summary>
    public string[] Arguments { get; }
}

#pragma warning restore IDE0290

/// <summary>
/// Describes one dependency property that the generator emits.
/// </summary>
internal sealed class DependencyPropertyModel
{
    /// <summary>Gets the namespace of the declaring type, or an empty string for the global namespace.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>Gets the simple name of the declaring type.</summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>Gets the fully qualified name of the declaring type.</summary>
    public string TypeDisplayName { get; init; } = string.Empty;

    /// <summary>Gets the accessibility keyword of the declaring type.</summary>
    public string Accessibility { get; init; } = "internal";

    /// <summary>Gets a value indicating whether the declaring type is static.</summary>
    public bool IsStatic { get; init; }

    /// <summary>Gets a value indicating whether the declaring type is sealed.</summary>
    public bool IsSealed { get; init; }

    /// <summary>Gets a value indicating whether the declaring type is partial.</summary>
    public bool IsPartial { get; init; }

    /// <summary>Gets a value indicating whether the declaring type is generic or nested.</summary>
    public bool HasUnsupportedShape { get; init; }

    /// <summary>Gets a value indicating whether the property is attached to a dependency object.</summary>
    public bool IsAttached { get; init; }

    /// <summary>Gets the name of the generated dependency property.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the CLR type of the property, including the nullable annotation.</summary>
    public string PropertyType { get; init; } = "object";

    /// <summary>Gets the typeof expression of the property type, without a nullable reference annotation.</summary>
    public string PropertyTypeExpression { get; init; } = "typeof(object)";

    /// <summary>Gets the default value source, or an empty string when the generator supplies it.</summary>
    public string DefaultValueExpression { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the property is registered read-only.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Gets the value indicating whether the property type is a reference type.</summary>
    public bool IsReferenceType { get; init; }

    /// <summary>Gets the WPF metadata options source, or an empty string when no option is set.</summary>
    public string OptionsExpression { get; init; } = string.Empty;

    /// <summary>Gets the default update source trigger source, or an empty string when the default applies.</summary>
    public string UpdateSourceTriggerExpression { get; init; } = string.Empty;

    /// <summary>Gets the category of the property, if any.</summary>
    public string? Category { get; init; }

    /// <summary>Gets the description of the property, if any.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the typeof expression of the type converter, if any.</summary>
    public string? TypeConverterExpression { get; init; }

    /// <summary>Gets the typeof expression of the browsable-for type of an attached property, if any.</summary>
    public string? BrowsableForTypeExpression { get; init; }

    /// <summary>Gets the XML documentation of the dependency property field, if any.</summary>
    public string? XmlDocumentation { get; init; }

    /// <summary>Gets the XML documentation of the CLR property or getter, if any.</summary>
    public string? ValueXmlDocumentation { get; init; }

    /// <summary>Gets the XML documentation of the attached property setter, if any.</summary>
    public string? SetterXmlDocumentation { get; init; }

    /// <summary>Gets the name of the change callback method.</summary>
    public string CallbackName { get; init; } = string.Empty;

    /// <summary>Gets how the registration obtains the change callback.</summary>
    public CallbackKind CallbackKind { get; init; }

    /// <summary>Gets the name of the coerce callback method, if any.</summary>
    public string? CoerceCallbackName { get; init; }

    /// <summary>Gets the name of the validate callback method, if any.</summary>
    public string? ValidateCallbackName { get; init; }

    /// <summary>Gets the source location of the declaring type identifier.</summary>
    public Location? TypeLocation { get; init; }

    /// <summary>Gets the declaration order of the attribute, used to keep the generated order stable.</summary>
    public int Order { get; init; }

    /// <summary>Gets the diagnostic reported while the property was read, if any.</summary>
    public DiagnosticInfo? Diagnostic { get; init; }
}
