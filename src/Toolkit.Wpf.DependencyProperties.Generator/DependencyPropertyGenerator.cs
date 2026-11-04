using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Toolkit.Wpf.DependencyProperties.Generator;

/// <summary>
/// Generates WPF dependency properties, their CLR wrappers or accessors, and their change callback wiring.
/// </summary>
[Generator]
public sealed class DependencyPropertyGenerator : IIncrementalGenerator
{
    private const string InstanceAttributeMetadataName = "VolocyNazad.Wpf.DependencyProperties.GenerateDependencyPropertyAttribute`1";
    private const string AttachedAttributeMetadataName = "VolocyNazad.Wpf.DependencyProperties.GenerateAttachedDependencyPropertyAttribute`1";
    private const string OptionsType = "global::System.Windows.FrameworkPropertyMetadataOptions";
    private const string UpdateSourceTriggerType = "global::System.Windows.Data.UpdateSourceTrigger";

    private static readonly SymbolDisplayFormat NullableTypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Registers the attribute source and the dependency property generation pipeline.
    /// </summary>
    /// <param name="context">The Roslyn incremental generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static output => output.AddSource(
            DependencyPropertyAttributeSource.HintName,
            SourceText.From(DependencyPropertyAttributeSource.Text, Encoding.UTF8)));

        IncrementalValuesProvider<DependencyPropertyModel> instance = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                InstanceAttributeMetadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, _) => Describe(syntaxContext, isAttached: false))
            .SelectMany(static (models, _) => models);

        IncrementalValuesProvider<DependencyPropertyModel> attached = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttachedAttributeMetadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, _) => Describe(syntaxContext, isAttached: true))
            .SelectMany(static (models, _) => models);

        IncrementalValueProvider<ImmutableArray<DependencyPropertyModel>> properties = instance.Collect()
            .Combine(attached.Collect())
            .Select(static (pair, _) => pair.Left.AddRange(pair.Right));

        context.RegisterSourceOutput(properties, static (output, models) => Emit(output, models));
    }

    private static ImmutableArray<DependencyPropertyModel> Describe(
        GeneratorAttributeSyntaxContext context,
        bool isAttached)
    {
        if (context.TargetSymbol is not INamedTypeSymbol type) return [];

        ImmutableArray<DependencyPropertyModel>.Builder builder =
            ImmutableArray.CreateBuilder<DependencyPropertyModel>(context.Attributes.Length);
        foreach (AttributeData attribute in context.Attributes)
        {
            DependencyPropertyModel? model = DescribeProperty(type, attribute, isAttached);
            if (model is not null) builder.Add(model);
        }

        return builder.ToImmutable();
    }

    private static DependencyPropertyModel? DescribeProperty(
        INamedTypeSymbol type,
        AttributeData attribute,
        bool isAttached)
    {
        if (attribute.AttributeClass is not { TypeArguments.Length: 1 } attributeClass) return null;

        ITypeSymbol propertyType = attributeClass.TypeArguments[0];
        if (propertyType.TypeKind is TypeKind.Error) return null;

        Location? typeLocation = type.Locations.FirstOrDefault(static location => location.IsInSource);
        Location? attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? typeLocation;
        string name = attribute.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value as string ?? string.Empty
            : string.Empty;

        Dictionary<string, TypedConstant> arguments = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            arguments[argument.Key] = argument.Value;

        if (arguments.TryGetValue("IsNullable", out TypedConstant isNullable)
            && isNullable.Value is true
            && propertyType.IsReferenceType)
        {
            propertyType = propertyType.WithNullableAnnotation(NullableAnnotation.Annotated);
        }

        DiagnosticInfo? diagnostic = null;
        if (name.Length == 0 || !SyntaxFacts.IsValidIdentifier(name))
            diagnostic = new DiagnosticInfo(DiagnosticDescriptors.PropertyNameIsInvalid, attributeLocation, name);

        (CallbackKind callbackKind, DiagnosticInfo? callbackDiagnostic) = ResolveCallback(
            type,
            name,
            propertyType,
            isAttached,
            ReadString(arguments, "OnChanged"),
            attributeLocation);
        diagnostic ??= callbackDiagnostic;

        (string? coerceName, DiagnosticInfo? coerceDiagnostic) = ResolveCoerce(type, name, attributeLocation);
        (string? validateName, DiagnosticInfo? validateDiagnostic) = ResolveValidate(type, name, attributeLocation);
        diagnostic ??= coerceDiagnostic ?? validateDiagnostic;

        return new DependencyPropertyModel
        {
            Namespace = type.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : type.ContainingNamespace.ToDisplayString(),
            TypeName = type.Name,
            TypeDisplayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Accessibility = ToAccessibilityKeyword(type.DeclaredAccessibility),
            IsStatic = type.IsStatic,
            IsSealed = type.IsSealed,
            IsPartial = IsPartial(type),
            HasUnsupportedShape = type.Arity > 0 || type.ContainingType is not null,
            IsAttached = isAttached,
            Name = name,
            PropertyType = propertyType.ToDisplayString(NullableTypeFormat),
            PropertyTypeExpression = $"typeof({propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})",
            DefaultValueExpression = ReadString(arguments, "DefaultValueExpression") ?? string.Empty,
            IsReadOnly = arguments.TryGetValue("IsReadOnly", out TypedConstant isReadOnly) && isReadOnly.Value is true,
            IsReferenceType = propertyType.IsReferenceType,
            OptionsExpression = arguments.TryGetValue("Options", out TypedConstant options) ? FormatFrameworkOptions(options) : string.Empty,
            UpdateSourceTriggerExpression = arguments.TryGetValue("DefaultUpdateSourceTrigger", out TypedConstant updateSourceTrigger)
                ? FormatUpdateSourceTrigger(updateSourceTrigger)
                : string.Empty,
            Category = ReadString(arguments, "Category"),
            Description = ReadString(arguments, "Description"),
            TypeConverterExpression = arguments.TryGetValue("TypeConverter", out TypedConstant typeConverter)
                ? FormatTypeOf(typeConverter)
                : null,
            BrowsableForTypeExpression = arguments.TryGetValue("BrowsableForType", out TypedConstant browsableForType)
                ? FormatTypeOf(browsableForType)
                : null,
            XmlDocumentation = ReadString(arguments, "XmlDocumentation"),
            ValueXmlDocumentation = isAttached
                ? ReadString(arguments, "GetterXmlDocumentation")
                : ReadString(arguments, "PropertyXmlDocumentation"),
            SetterXmlDocumentation = isAttached ? ReadString(arguments, "SetterXmlDocumentation") : null,
            CallbackName = string.IsNullOrWhiteSpace(ReadString(arguments, "OnChanged"))
                ? $"On{name}Changed"
                : ReadString(arguments, "OnChanged")!,
            CallbackKind = callbackKind,
            CoerceCallbackName = coerceName,
            ValidateCallbackName = validateName,
            TypeLocation = typeLocation,
            Order = attribute.ApplicationSyntaxReference?.Span.Start ?? 0,
            Diagnostic = diagnostic,
        };
    }

    private static (CallbackKind Kind, DiagnosticInfo? Diagnostic) ResolveCallback(
        INamedTypeSymbol type,
        string propertyName,
        ITypeSymbol propertyType,
        bool isAttached,
        string? onChanged,
        Location? fallbackLocation)
    {
        string callbackName = string.IsNullOrWhiteSpace(onChanged) ? $"On{propertyName}Changed" : onChanged!;
        List<IMethodSymbol> members = [.. type.GetMembers(callbackName).OfType<IMethodSymbol>()];
        IMethodSymbol? staticCallback = members.FirstOrDefault(IsEventArgsCallback);
        List<IMethodSymbol> hooks =
        [
            .. members.Where(method => IsImplementedPartialHook(method) && method.IsStatic == isAttached && method.ReturnsVoid),
        ];
        IMethodSymbol? valuesHook = hooks.FirstOrDefault(hook => MatchesHook(hook, propertyType, parameterCount: 2));
        IMethodSymbol? newValueHook = hooks.FirstOrDefault(hook => MatchesHook(hook, propertyType, parameterCount: 1));

        if (staticCallback is not null && (valuesHook is not null || newValueHook is not null))
        {
            return (CallbackKind.None, new DiagnosticInfo(
                DiagnosticDescriptors.CallbackConflict,
                staticCallback.Locations.FirstOrDefault() ?? fallbackLocation,
                callbackName));
        }

        if (staticCallback is not null) return (CallbackKind.StaticMethod, null);
        if (valuesHook is not null) return (CallbackKind.ValuesHook, null);
        if (newValueHook is not null) return (CallbackKind.NewValueHook, null);

        if (members.Count == 0)
        {
            return string.IsNullOrWhiteSpace(onChanged)
                ? (CallbackKind.None, null)
                : (CallbackKind.None, new DiagnosticInfo(
                    DiagnosticDescriptors.CallbackNotFound,
                    fallbackLocation,
                    callbackName,
                    propertyName,
                    type.Name));
        }

        return (CallbackKind.None, new DiagnosticInfo(
            DiagnosticDescriptors.CallbackSignatureNotSupported,
            members[0].Locations.FirstOrDefault() ?? fallbackLocation,
            callbackName));
    }

    private static (string? Name, DiagnosticInfo? Diagnostic) ResolveCoerce(
        INamedTypeSymbol type,
        string propertyName,
        Location? fallbackLocation)
    {
        IMethodSymbol? method = type.GetMembers($"Coerce{propertyName}").OfType<IMethodSymbol>().FirstOrDefault();
        if (method is null) return (null, null);

        bool matches = method.IsStatic
            && !method.ReturnsVoid
            && method.Parameters.Length == 2
            && HasDisplayName(method.Parameters[0].Type, "System.Windows.DependencyObject")
            && HasDisplayName(method.Parameters[1].Type, "System.Object");
        return matches
            ? (method.Name, null)
            : (null, new DiagnosticInfo(
                DiagnosticDescriptors.CallbackSignatureNotSupported,
                method.Locations.FirstOrDefault() ?? fallbackLocation,
                method.Name));
    }

    private static (string? Name, DiagnosticInfo? Diagnostic) ResolveValidate(
        INamedTypeSymbol type,
        string propertyName,
        Location? fallbackLocation)
    {
        IMethodSymbol? method = type.GetMembers($"Validate{propertyName}").OfType<IMethodSymbol>().FirstOrDefault();
        if (method is null) return (null, null);

        bool matches = method.IsStatic
            && method.ReturnType.SpecialType == SpecialType.System_Boolean
            && method.Parameters.Length == 1
            && HasDisplayName(method.Parameters[0].Type, "System.Object");
        return matches
            ? (method.Name, null)
            : (null, new DiagnosticInfo(
                DiagnosticDescriptors.CallbackSignatureNotSupported,
                method.Locations.FirstOrDefault() ?? fallbackLocation,
                method.Name));
    }

    private static bool IsEventArgsCallback(IMethodSymbol method)
    {
        return method.IsStatic
            && method.ReturnsVoid
            && method.Parameters.Length == 2
            && HasDisplayName(method.Parameters[0].Type, "System.Windows.DependencyObject")
            && HasDisplayName(method.Parameters[1].Type, "System.Windows.DependencyPropertyChangedEventArgs");
    }

    private static bool IsImplementedPartialHook(IMethodSymbol method)
    {
        return !method.IsPartialDefinition
            && method.DeclaringSyntaxReferences.Any(static reference =>
                reference.GetSyntax() is MethodDeclarationSyntax declaration
                && declaration.Modifiers.Any(SyntaxKind.PartialKeyword)
                && (declaration.Body is not null || declaration.ExpressionBody is not null));
    }

    private static bool MatchesHook(IMethodSymbol hook, ITypeSymbol propertyType, int parameterCount)
    {
        return hook.Parameters.Length == parameterCount
            && hook.Parameters.All(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, propertyType));
    }

    private static bool HasDisplayName(ITypeSymbol type, string displayName)
    {
        return string.Equals(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            $"global::{displayName}",
            StringComparison.Ordinal);
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        List<SyntaxNode> declarations = [.. type.DeclaringSyntaxReferences.Select(static reference => reference.GetSyntax())];
        return declarations.Count > 0 && declarations.All(declaration =>
            declaration is ClassDeclarationSyntax classDeclaration
            && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static string ToAccessibilityKeyword(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Internal or Accessibility.NotApplicable => "internal",
            _ => "internal",
        };
    }

    private static string? ReadString(Dictionary<string, TypedConstant> arguments, string name)
    {
        return arguments.TryGetValue(name, out TypedConstant argument) ? argument.Value as string : null;
    }

    private static string? FormatTypeOf(TypedConstant argument)
    {
        return argument.Value is ITypeSymbol symbol
            ? $"typeof({symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})"
            : null;
    }

    private static string FormatFrameworkOptions(TypedConstant argument)
    {
        if (argument.Value is not int value || value == 0) return string.Empty;
        if (argument.Type is not INamedTypeSymbol enumType) return string.Empty;

        List<string> names = [];
        int remaining = value;
        foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.HasConstantValue || field.ConstantValue is not int fieldValue || fieldValue == 0) continue;
            if ((remaining & fieldValue) != fieldValue) continue;

            names.Add($"{OptionsType}.{field.Name}");
            remaining &= ~fieldValue;
        }

        return remaining != 0 || names.Count == 0
            ? $"({OptionsType}){value}"
            : string.Join(" | ", names);
    }

    private static string FormatUpdateSourceTrigger(TypedConstant argument)
    {
        if (argument.Value is not int value || value == 0) return string.Empty;
        if (argument.Type is not INamedTypeSymbol enumType) return string.Empty;

        IFieldSymbol? field = enumType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(member =>
            member.HasConstantValue && member.ConstantValue is int fieldValue && fieldValue == value);
        return field is null ? string.Empty : $"{UpdateSourceTriggerType}.{field.Name}";
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<DependencyPropertyModel> models)
    {
        foreach (IGrouping<string, DependencyPropertyModel> group in models.GroupBy(
                     static model => model.TypeDisplayName,
                     StringComparer.Ordinal))
        {
            List<DependencyPropertyModel> properties = [.. group.OrderBy(static model => model.Order)];
            DependencyPropertyModel first = properties[0];
            bool hasError = false;

            foreach (DependencyPropertyModel property in properties)
            {
                if (property.Diagnostic is not { } diagnostic) continue;
                context.ReportDiagnostic(ToDiagnostic(diagnostic));
                hasError = true;
            }

            if (first.HasUnsupportedShape)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TypeShapeNotSupported, first.TypeLocation, first.TypeName));
                hasError = true;
            }
            else if (first.IsAttached && !first.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AttachedTypeMustBeStatic, first.TypeLocation, first.TypeName));
                hasError = true;
            }
            else if (!first.IsAttached && first.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InstanceTypeMustNotBeStatic,
                    first.TypeLocation,
                    first.TypeName,
                    first.Name));
                hasError = true;
            }

            if (!first.IsPartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TypeMustBePartial, first.TypeLocation, first.TypeName));
                hasError = true;
            }

            if (hasError) continue;

            context.AddSource(
                HintName(first),
                SourceText.From(DependencyPropertySourceEmitter.Emit(properties), Encoding.UTF8));
        }
    }

    private static string HintName(DependencyPropertyModel model)
    {
        return model.Namespace.Length > 0
            ? $"{model.Namespace}.{model.TypeName}.DependencyProperties.g.cs"
            : $"{model.TypeName}.DependencyProperties.g.cs";
    }

    private static Diagnostic ToDiagnostic(DiagnosticInfo info)
    {
        return info.Location is null
            ? Diagnostic.Create(info.Descriptor, Location.None, info.Arguments)
            : Diagnostic.Create(info.Descriptor, info.Location, info.Arguments);
    }
}
