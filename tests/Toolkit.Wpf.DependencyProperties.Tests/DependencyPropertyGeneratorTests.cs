using Microsoft.CodeAnalysis;
using Xunit;

namespace Toolkit.Wpf.DependencyProperties.Tests;

/// <summary>
/// Covers the source that the dependency property generator emits and the diagnostics it reports.
/// </summary>
public sealed class DependencyPropertyGeneratorTests
{
    [Fact]
    public void Instance_property_generates_registration_and_clr_wrapper()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<string>("Title", IsNullable = true)]
            public sealed partial class SampleView
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains(
            "public static readonly global::System.Windows.DependencyProperty TitleProperty =",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("global::System.Windows.DependencyProperty.Register(", generated, StringComparison.Ordinal);
        Assert.Contains("public string? Title", generated, StringComparison.Ordinal);
        Assert.Contains("get => (string?)GetValue(TitleProperty);", generated, StringComparison.Ordinal);
        Assert.Contains("propertyChangedCallback: null", generated, StringComparison.Ordinal);
        Assert.Contains(
            "/// <summary>Identifies the <see cref=\"Title\"/> dependency property.</summary>",
            generated,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Attached_property_generates_getter_and_setter()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateAttachedDependencyProperty<string>("Title", IsNullable = true, BrowsableForType = typeof(System.Windows.FrameworkElement))]
            public static partial class SampleAttached
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("global::System.Windows.DependencyProperty.RegisterAttached(", generated, StringComparison.Ordinal);
        Assert.Contains(
            "[global::System.Windows.AttachedPropertyBrowsableForTypeAttribute(typeof(global::System.Windows.FrameworkElement))]",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "public static string? GetTitle(global::System.Windows.DependencyObject element) =>",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            "public static void SetTitle(global::System.Windows.DependencyObject element, string? value) =>",
            generated,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Static_callback_method_is_wired_as_property_changed_callback()
    {
        const string source = """
            using System.Windows;
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<bool>("IsObsolete")]
            public sealed partial class SampleView
            {
                private static void OnIsObsoleteChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
                {
                }
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("propertyChangedCallback: OnIsObsoleteChanged", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("partial void OnIsObsoleteChanged", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Named_callback_lets_several_properties_share_one_method()
    {
        const string source = """
            using System.Windows;
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateAttachedDependencyProperty<string>("Title", IsNullable = true, OnChanged = nameof(OnContentChanged))]
            [GenerateAttachedDependencyProperty<string>("Note", IsNullable = true, OnChanged = nameof(OnContentChanged))]
            public static partial class SampleAttached
            {
                private static void OnContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
                {
                }
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        int wiring = Count(generated, "propertyChangedCallback: OnContentChanged");
        Assert.Equal(2, wiring);
    }

    [Fact]
    public void Partial_hook_is_wrapped_into_a_static_callback()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<bool>("IsSpinning", DefaultValueExpression = "true")]
            public sealed partial class SampleView
            {
                partial void OnIsSpinningChanged(bool oldValue, bool newValue)
                {
                }
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("partial void OnIsSpinningChanged(bool oldValue, bool newValue);", generated, StringComparison.Ordinal);
        Assert.Contains("propertyChangedCallback: OnIsSpinningChanged", generated, StringComparison.Ordinal);
        Assert.Contains(
            "((global::Sample.SampleView)sender).OnIsSpinningChanged((bool)args.OldValue, (bool)args.NewValue);",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("defaultValue: true", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Nullable_value_type_keeps_its_annotation()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<bool?>("DialogResult")]
            public sealed partial class SampleView
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("public bool? DialogResult", generated, StringComparison.Ordinal);
        Assert.Contains("get => (bool?)GetValue(DialogResultProperty);", generated, StringComparison.Ordinal);
        Assert.Contains("defaultValue: default(bool?),", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_only_property_uses_a_property_key()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<double>("Progress", IsReadOnly = true, DefaultValueExpression = "0.0")]
            public partial class SampleView
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("private static readonly global::System.Windows.DependencyPropertyKey ProgressPropertyKey =", generated, StringComparison.Ordinal);
        Assert.Contains("global::System.Windows.DependencyProperty.RegisterReadOnly(", generated, StringComparison.Ordinal);
        Assert.Contains(
            "public static readonly global::System.Windows.DependencyProperty ProgressProperty = ProgressPropertyKey.DependencyProperty;",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("protected set => SetValue(ProgressProperty, value);", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Metadata_options_and_default_value_are_used()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<double>(
                "Glow",
                DefaultValueExpression = "1.0",
                Options = System.Windows.FrameworkPropertyMetadataOptions.AffectsRender)]
            public sealed partial class SampleView
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("new global::System.Windows.FrameworkPropertyMetadata(", generated, StringComparison.Ordinal);
        Assert.Contains(
            "flags: global::System.Windows.FrameworkPropertyMetadataOptions.AffectsRender,",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("defaultValue: 1.0,", generated, StringComparison.Ordinal);
        Assert.Contains("isAnimationProhibited: false),", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultUpdateSourceTrigger:", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Explicit_update_source_trigger_is_emitted()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<string>(
                "Text",
                IsNullable = true,
                Options = System.Windows.FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                DefaultUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged)]
            public sealed partial class SampleView
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains(
            "defaultUpdateSourceTrigger: global::System.Windows.Data.UpdateSourceTrigger.PropertyChanged),",
            generated,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Metadata_with_options_and_no_update_source_trigger_initializes()
    {
        const string source = """
            using System.Windows.Input;
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateAttachedDependencyProperty<ICommand>(
                "HelpCommand",
                IsNullable = true,
                Options = System.Windows.FrameworkPropertyMetadataOptions.Inherits)]
            public static partial class SampleAttached
            {
            }
            """;

        GeneratorRunner.RunAndLoad(source, "Sample.SampleAttached", "HelpCommandProperty");
    }

    [Fact]
    public void Unsupported_callback_signature_is_reported()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<bool>("IsReady")]
            public sealed partial class SampleView
            {
                private void OnIsReadyChanged(bool value)
                {
                }
            }
            """;

        GeneratorDriverRunResult result = GeneratorRunner.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "VDP0004");
    }

    [Fact]
    public void Named_callback_that_does_not_exist_is_reported()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<bool>("IsReady", OnChanged = "OnMissing")]
            public sealed partial class SampleView
            {
            }
            """;

        GeneratorDriverRunResult result = GeneratorRunner.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "VDP0005");
    }

    [Fact]
    public void Non_partial_type_is_reported()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<bool>("IsReady")]
            public sealed class SampleView
            {
            }
            """;

        GeneratorDriverRunResult result = GeneratorRunner.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "VDP0001");
        Assert.DoesNotContain(
            result.Results.SelectMany(static runResult => runResult.GeneratedSources),
            static generated => generated.HintName.EndsWith(".DependencyProperties.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Attached_property_requires_a_static_type()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateAttachedDependencyProperty<bool>("IsReady")]
            public sealed partial class SampleView
            {
            }
            """;

        GeneratorDriverRunResult result = GeneratorRunner.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "VDP0002");
    }

    [Fact]
    public void Nested_type_is_reported()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            public partial class Outer
            {
                [GenerateDependencyProperty<bool>("IsReady")]
                public sealed partial class Inner
                {
                }
            }
            """;

        GeneratorDriverRunResult result = GeneratorRunner.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "VDP0006");
    }

    [Fact]
    public void Attributes_are_added_to_the_compilation()
    {
        const string source = """
            using VolocyNazad.Wpf.DependencyProperties;

            namespace Sample;

            [GenerateDependencyProperty<string>("Title", IsNullable = true)]
            public sealed partial class SampleView
            {
            }
            """;

        string generated = GeneratorRunner.GeneratedText(GeneratorRunner.Run(source));

        Assert.Contains("internal sealed class GenerateDependencyPropertyAttribute<T>", generated, StringComparison.Ordinal);
        Assert.Contains("internal sealed class GenerateAttachedDependencyPropertyAttribute<T>", generated, StringComparison.Ordinal);
    }

    private static int Count(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
