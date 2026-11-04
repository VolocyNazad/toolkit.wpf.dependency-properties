# Repository guide

Paths in this document are relative to the repository root.
Read and follow the [development policy](policies/development.md) alongside this guide.

## About

Source for `VolocyNazad.Wpf.DependencyPropertyGenerator` (NuGet package
`VolocyNazad.Wpf.DependencyPropertyGenerator`): a Roslyn source generator for
WPF dependency properties. From `[GenerateDependencyProperty<T>]` on a control
or behavior and `[GenerateAttachedDependencyProperty<T>]` on a static class it
generates:

- the `DependencyProperty.Register` / `RegisterAttached` field, or a
  `DependencyPropertyKey` pair for a read-only property;
- the CLR wrapper property, or the static `Get<Name>` / `Set<Name>` accessors of
  an attached property;
- the XML documentation of every generated member;
- the change callback wiring, either to a canonical WPF static method or to the
  generated partial hooks.

It also reports `VDP0001`-`VDP0008` when a declaration cannot be generated: a
type that is not `partial`, an attached property outside a static class, an
instance property in a static class, a callback with no supported signature, a
missing named callback, a generic or nested type, an invalid property name, and
a callback declared in both forms at once.

The generator is WPF-only and has no other dependency: it does not reference the
Revit API and does not require WPF at analyzer build time. The attributes are
generated into the consuming assembly, so there is no runtime library and no
attributes assembly.

## Repository structure

```
src/
  Toolkit.Wpf.DependencyProperties/                 packaging shell; packs the
                                                    analyzer into the NuGet package
  Toolkit.Wpf.DependencyProperties.Generator/       the generator
    DependencyPropertyGenerator.cs                  incremental generator pipeline
    DependencyPropertyAttributeSource.cs            emitted attributes
    DependencyPropertySourceEmitter.cs              generated source writer
    DependencyPropertyModel.cs                      property model
    DiagnosticDescriptors.cs                        VDP0001-VDP0008 descriptors
    AnalyzerReleases.Shipped.md
    AnalyzerReleases.Unshipped.md
tests/
  Toolkit.Wpf.DependencyProperties.Tests/           generator tests against
                                                    in-memory compilations
```

## Technology stack

- `Microsoft.NET.Sdk`, `netstandard2.0`, `IsRoslynComponent=true`,
  `EnforceExtendedAnalyzerRules=true` for the generator project.
- Packaged through a separate shell project (`src/Toolkit.Wpf.DependencyProperties`):
  `IncludeBuildOutput=false` + a `None` item packs the built generator DLL into
  `analyzers/dotnet/cs`; `DevelopmentDependency=true` so the package does not flow
  as a normal compile-time dependency to consumers of a library that references it.
- Central package management (`Directory.Packages.props`,
  `ManagePackageVersionsCentrally=true`): project packages are versioned in the
  root file, and `tests/Directory.Packages.props` imports it to version test-only
  packages separately.
- Repo-wide global analyzers pinned in `Directory.Packages.props`:
  Roslynator.Analyzers, SonarAnalyzer.CSharp.
- `Microsoft.CodeAnalysis.CSharp` is exact-pinned (`[4.14.0]`) - the Roslyn
  version shipped with the latest Visual Studio 2022 17.14 line, so the generator
  stays loadable by any current VS 2022 install. See the
  [Roslyn compatibility policy](policies/development.md#roslyn-compatibility).
- MinVer with the `v` tag prefix, as configured by `MinVerTagPrefix` in
  `Directory.Build.props`.
- Tests target `net10.0-windows` with `UseWPF=true` and use xUnit v3 through
  Microsoft.Testing.Platform, driving the generator with `CSharpGeneratorDriver`
  over an in-memory compilation and asserting the generated source and the
  reported diagnostics.

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `docs/policies/development.md` contains the development policy.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `CHANGELOG.md` records changes under `Unreleased` before release.

The root solution exposes the documentation files under a `docs` solution folder in Visual Studio, preserving their subfolder structure. When adding documentation files, also add them as solution items; solution folders do not automatically include new files.

The root `global.json` selects stable .NET SDK 10.0 (minimum `10.0.103`, `rollForward: latestFeature`). CI and publishing install the SDK from this file. See the [SDK selection policy](policies/development.md#net-sdk-selection).

## Solution items

The root solution exposes repository-level documents and configuration under
`solutionItems`, GitHub files and maintenance scripts in matching subfolders, and
documentation under `docs/`. The list is explicit, not a filesystem glob; keep
links up to date when files change. See the solution items policy in the
[development policy](policies/development.md#solution-items).

## Repository validation

`scripts/Validate-Repository.ps1` enforces the required repository documents,
their navigation links, and complete, valid Solution Items. The
`.github/workflows/repository-policy.yml` workflow runs it for pushes and pull
requests. See the [development policy](policies/development.md#repository-validation).

## Formatting

The root `.editorconfig` defines the portable formatting baseline. See the
[development policy](policies/development.md#formatting-baseline).

## Testing

The automated test suite is in `tests/Toolkit.Wpf.DependencyProperties.Tests` and
verifies the generated source for instance and attached properties, read-only
properties, metadata options, default value expressions, both callback forms and
shared callbacks, and every reported diagnostic: a non-partial type, an attached
property outside a static class, an instance property in a static class, an
unsupported callback signature, a missing named callback, a nested type and the
attribute source itself.

## Versioning and release tags

The package uses MinVer 8 with stable tags in the `vMAJOR.MINOR.PATCH` format. The tag without its `v` prefix is the NuGet package version. Manual publishing accepts exactly one matching tag at HEAD and verifies the package ID and version before pushing to NuGet.

Bump component, applied to generator changes:

- **MAJOR** - a breaking change: generated code that no longer compiles for an existing declaration, a removed or renamed attribute argument, a diagnostic reclassified to error, or a move of the minimum supported Microsoft.CodeAnalysis/Visual Studio baseline.
- **MINOR** - a new capability: a new attribute argument, a new generated member form, a new diagnostic, or support for a declaration shape that previously reported an error.
- **PATCH** - a bug fix: a wrong generated member, a false positive or false negative diagnostic, a packaging fix, or documentation.

The tagged commit must already contain the release section in `CHANGELOG.md` with an empty `Unreleased` section above it; the publish workflow stops otherwise. See the [release changelog policy](policies/development.md#release-changelog).
