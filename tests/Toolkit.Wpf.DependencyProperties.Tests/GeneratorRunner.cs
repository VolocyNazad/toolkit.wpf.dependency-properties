using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Toolkit.Wpf.DependencyProperties.Generator;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Toolkit.Wpf.DependencyProperties.Tests;

/// <summary>
/// Runs the dependency property generator over an in-memory compilation.
/// </summary>
internal static class GeneratorRunner
{
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    /// <summary>
    /// Runs the generator over the supplied source.
    /// </summary>
    /// <param name="source">The source to compile and annotate.</param>
    /// <returns>The generator run result.</returns>
    public static GeneratorDriverRunResult Run(string source)
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new DependencyPropertyGenerator().AsSourceGenerator()],
            additionalTexts: null,
            parseOptions,
            optionsProvider: null);
        driver = driver.RunGenerators(compilation, cancellationToken);
        return driver.GetRunResult();
    }

    /// <summary>
    /// Joins the generated sources of a run.
    /// </summary>
    /// <param name="result">The generator run result.</param>
    /// <returns>The generated sources, separated by a line break.</returns>
    public static string GeneratedText(GeneratorDriverRunResult result)
    {
        return string.Join(
            "\n",
            result.Results
                .SelectMany(static runResult => runResult.GeneratedSources)
                .Select(static generated => generated.SourceText.ToString()));
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase)
        {
            typeof(object).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            typeof(List<>).Assembly.Location,
            typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location,
            typeof(System.Windows.DependencyProperty).Assembly.Location,
            typeof(System.Windows.DependencyPropertyChangedEventArgs).Assembly.Location,
            typeof(System.Windows.Data.UpdateSourceTrigger).Assembly.Location,
            typeof(System.Windows.Input.ICommand).Assembly.Location,
        };

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
        {
            foreach (string path in trustedPlatformAssemblies.Split(Path.PathSeparator))
                paths.Add(path);
        }

        return
        [
            .. paths
                .Where(static path => path.Length > 0 && File.Exists(path))
                .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)),
        ];
    }
}
