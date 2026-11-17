# Testing policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: adding tests, choosing a test kind or running a suite.

Tests are the evidence that a change works. The repository separates them by what they need
to run: a normal process, or a person in front of the product.

## Test kinds

| Kind | Project suffix | Runs in | May touch |
| --- | --- | --- | --- |
| Headless | `*.Tests` | a normal process, including a hosted runner | pure code, temporary files, in-memory state |
| Manual smoke | - | Visual Studio and a WPF application, by a person | window, dialog, XAML designer, file system |

The suffix is a contract: CI selects tests by it, and a project without `*.Tests` is not a
test project. Prefer the product prefix as well (`<Product>.Tests`), but the suffix is what
matters.

## What needs a test

- A bug fix comes with a regression test that fails without the fix.
- A behaviour change comes with a test of the new behaviour, not only proof that nothing
  crashes.
- A generator is tested through the generated code and the reported diagnostics: drive it with
  `CSharpGeneratorDriver` over an in-memory compilation, then assert the generated source and the
  diagnostic ids.
- Every `VDP` diagnostic has a test that triggers it and, where the diagnostic is suppressible, one
  that shows the corrected declaration.
- A change to the emitted attribute source is tested like any other generated output, including the
  attribute's own XML documentation.

Documentation, formatting, dependency updates and purely visual changes do not need a test,
but the build must pass.

## Isolation and determinism

- Tests do not use the network and do not write into the repository; temporary data goes to a
  temporary directory.
- A generator test builds its own in-memory compilation. Do not reference the test project's own
  assembly from a compilation under test: assert against the source and the diagnostics instead.
- No dialogs and no waiting for input in any test.
- A test does not depend on execution order, locale, registry state or the machine it runs on.

## Running the tests

Headless: `dotnet test tests/Toolkit.Wpf.DependencyProperties.Tests/Toolkit.Wpf.DependencyProperties.Tests.csproj --configuration Release`,
or the built executable, which is what CI may use.

## Manual smoke check

For a change that affects the generated WPF code, add the generator to a small WPF project, write a
`[GenerateDependencyProperty<T>]` declaration, build, and confirm that the property binds: the
generated registration compiles, the CLR wrapper or the `Get`/`Set` accessors are present, and the
change callback fires. A change that only touches diagnostics or documentation has no manual step.

## Flaky and skipped tests

A test that fails intermittently is fixed or quarantined with an issue and a date. A skipped
test has a reason and an issue; an unexplained skip is a defect.

## Coverage

Every behaviour a consumer of the generator can observe has a test. No numeric threshold
is enforced: a percentage that moves without a behaviour change is not a goal.

The workflow around tests - branches, commit messages and the changelog - is defined by the
[development policy](development.md).
