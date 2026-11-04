using Microsoft.CodeAnalysis;

namespace Toolkit.Wpf.DependencyProperties.Generator;

/// <summary>
/// Describes the diagnostics that the dependency property generator reports.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>
    /// Gets the diagnostic for a type that declares a dependency property but is not partial.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "VDP0001",
        title: "The annotated type must be partial",
        messageFormat: "Type '{0}' must be declared partial to receive generated dependency properties",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for an attached dependency property declared outside a static class.
    /// </summary>
    public static readonly DiagnosticDescriptor AttachedTypeMustBeStatic = new(
        id: "VDP0002",
        title: "An attached dependency property requires a static type",
        messageFormat: "Type '{0}' must be a static class to receive generated attached dependency properties",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for an instance dependency property declared in a static class.
    /// </summary>
    public static readonly DiagnosticDescriptor InstanceTypeMustNotBeStatic = new(
        id: "VDP0003",
        title: "An instance dependency property requires an instantiable type",
        messageFormat: "Type '{0}' is static and cannot declare the instance dependency property '{1}'",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for a method whose name the generator reserves but whose signature it cannot use.
    /// </summary>
    public static readonly DiagnosticDescriptor CallbackSignatureNotSupported = new(
        id: "VDP0004",
        title: "The dependency property callback signature is not supported",
        messageFormat: "Method '{0}' must be a static method taking (DependencyObject, DependencyPropertyChangedEventArgs) "
            + "or a partial hook taking the property value, otherwise the generated callback would stay silent",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for a callback name that no method declares.
    /// </summary>
    public static readonly DiagnosticDescriptor CallbackNotFound = new(
        id: "VDP0005",
        title: "The named dependency property callback does not exist",
        messageFormat: "The callback '{0}' of the dependency property '{1}' must be a method of type '{2}'",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for an unsupported declaring type shape.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeShapeNotSupported = new(
        id: "VDP0006",
        title: "The declaring type shape is not supported",
        messageFormat: "Type '{0}' must not be generic or nested to receive generated dependency properties",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for a property name that cannot become a CLR member.
    /// </summary>
    public static readonly DiagnosticDescriptor PropertyNameIsInvalid = new(
        id: "VDP0007",
        title: "The dependency property name is invalid",
        messageFormat: "The dependency property name '{0}' must be a non-empty identifier",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Gets the diagnostic for a property that declares both a static callback and a partial hook.
    /// </summary>
    public static readonly DiagnosticDescriptor CallbackConflict = new(
        id: "VDP0008",
        title: "The dependency property declares two change callbacks",
        messageFormat: "Method '{0}' is declared both as a static callback and as a partial hook; keep only one form",
        category: "Toolkit.Wpf.DependencyProperties",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
