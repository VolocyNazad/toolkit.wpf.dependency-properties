# Toolkit.Wpf.DependencyProperties

[![NuGet](https://img.shields.io/nuget/vpre/VolocyNazad.Wpf.DependencyPropertyGenerator?logo=nuget)](https://www.nuget.org/packages/VolocyNazad.Wpf.DependencyPropertyGenerator/)
[![GitHub release](https://img.shields.io/github/release/VolocyNazad/toolkit.wpf.dependency-properties.svg?logo=github)](https://github.com/VolocyNazad/toolkit.wpf.dependency-properties/releases/latest)
[![GitHub license](https://img.shields.io/github/license/VolocyNazad/toolkit.wpf.dependency-properties)](https://raw.githubusercontent.com/VolocyNazad/toolkit.wpf.dependency-properties/main/LICENSE)
![build-and-test.yml](https://github.com/VolocyNazad/toolkit.wpf.dependency-properties/workflows/.github/workflows/build-and-test.yml/badge.svg)

A Roslyn source generator that removes the `DependencyProperty` boilerplate from
WPF code: registration, the CLR wrapper or the `Get`/`Set` accessors, the XML
documentation and the change callback wiring are generated from an attribute.
The generated members are ordinary WPF code - no runtime library, no reflection
and no base class is involved.

```csharp
using VolocyNazad.Wpf.DependencyProperties;

[GenerateDependencyProperty<bool>("IsSpinning", DefaultValueExpression = "true")]
public sealed partial class MyControl : UserControl
{
    partial void OnIsSpinningChanged(bool oldValue, bool newValue) { /* ... */ }
}
```

```csharp
[GenerateAttachedDependencyProperty<ICommand>(
    "HelpCommand",
    IsNullable = true,
    Options = FrameworkPropertyMetadataOptions.Inherits)]
public static partial class HelpToolTip
{
    private static void OnHelpCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) { /* ... */ }
}
```

Both callback forms are supported: the canonical WPF signature
`(DependencyObject, DependencyPropertyChangedEventArgs)` declared as a normal
static method, and the generated partial hooks `On<Name>Changed(newValue)` /
`On<Name>Changed(oldValue, newValue)`. A method named after the property that
matches neither form fails the build instead of silently producing a `null`
callback.

## Installation

- Grab the latest package on [NuGet](https://www.nuget.org/packages/VolocyNazad.Wpf.DependencyPropertyGenerator/).
- Reference it as an analyzer-only, build-time dependency:

```xml
<ItemGroup>
  <PackageReference Include="VolocyNazad.Wpf.DependencyPropertyGenerator" Version="x.y.z">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

No further setup is required: the attributes are generated into the consuming
assembly, so they never leak into its public API and no extra package or
project reference is needed.

## Usage

Annotate a `partial` class for an instance property, or a `static partial` class
for an attached property. The generated member set:

- `public static readonly DependencyProperty <Name>Property` (`Register` or
  `RegisterAttached`), or a `DependencyPropertyKey` plus property for
  `IsReadOnly = true`;
- `public <T> <Name> { get; set; }` on an instance property, or
  `public static T Get<Name>(DependencyObject)` and
  `public static void Set<Name>(DependencyObject, T)` for an attached one;
- XML documentation for every generated member;
- the change callback wiring described above;
- `Coerce<Name>(DependencyObject, object)` and `Validate<Name>(object)` methods
  are wired as the coerce and validate callbacks when they exist.

Attribute arguments: `DefaultValueExpression` (source of the default value, for
example `"string.Empty"`, `"1.0"`, `"default(MyEnum)"`), `IsNullable` (for
reference-type properties), `IsReadOnly`, `Options`
(`FrameworkPropertyMetadataOptions`), `DefaultUpdateSourceTrigger`, `Category`,
`Description`, `TypeConverter`, `OnChanged` (name of the callback method, so
several properties can share one), `BrowsableForType` (attached only) and the
`XmlDocumentation` / `PropertyXmlDocumentation` / `GetterXmlDocumentation` /
`SetterXmlDocumentation` raw documentation strings.

## Diagnostics

| Id | Severity | Meaning |
| --- | --- | --- |
| `VDP0001` | Error | The annotated type is not `partial`. |
| `VDP0002` | Error | An attached property is declared outside a `static` class. |
| `VDP0003` | Error | An instance property is declared in a `static` class. |
| `VDP0004` | Error | A method named for the property has no supported callback signature. |
| `VDP0005` | Error | The callback named through `OnChanged` does not exist. |
| `VDP0006` | Error | The declaring type is generic or nested. |
| `VDP0007` | Error | The property name is not a valid identifier. |
| `VDP0008` | Error | A property declares both a static callback and a partial hook. |

## What stays hand-written

- `AddOwner` registrations, for example re-exposing an inherited property.
- `OverrideMetadata` calls.
- Private properties that only hold state for another property.
- An `OnPropertyChanged` override of the owning type.
- A type that XAML in the same project references: WPF markup compilation
  recompiles such a type in a temporary project that carries no analyzers, so
  the generated members would be missing there. Keep that registration in code.

## Development documentation

- [Development policy](docs/policies/development.md)
- [Repository guide and technology stack](docs/repository.md)
- [Branching policy](docs/policies/branching.md)
- [Commit message policy](docs/policies/commits.md)
- [Changelog policy](docs/policies/changelog.md)
- [Testing policy](docs/policies/testing.md)
- [Benchmark policy](docs/policies/benchmarks.md)

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.
