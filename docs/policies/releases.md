# Release policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: preparing a release tag or publishing the package.

A release turns a tagged commit into a NuGet package on nuget.org. The workflow is
`.github/workflows/publish-nuget.yml`; it consumes the tag and the release section of
`CHANGELOG.md`. The [changelog policy](changelog.md) owns that section's format, the
[branching policy](branching.md) owns release and hotfix branches, and the
[testing policy](testing.md) owns what must be green before the tag.

## Release channels

- A **stable release** is a `vMAJOR.MINOR.PATCH` tag and the only channel the automation publishes.
  This repository prefixes the tag with `v`: `v1.0.1`, not `1.0.1`. The package version is the tag
  without the prefix.
- A **pre-release** is a `vMAJOR.MINOR.PATCH-<channel>.<number>` tag such as `v1.1.0-beta.1`. NuGet
  accepts it as a pre-release and package managers do not select it for a plain version range.
  Current automation publishes stable releases only: create a pre-release manually with
  `dotnet nuget push` after the same verification, or extend the workflow first.
- A package version that was published is never reused, not even for a pre-release with the same
  number. `--skip-duplicate` protects a retried workflow run; it is not permission to republish.

## Versioning

- `MinVer` 8 derives the version from the tag on the released commit. A stable tag such as `v1.0.1`
  is the package version `1.0.1`; commits after a tag receive a `dev.<count>` pre-release suffix and
  are not published.
- The tag is the version source; the package version is not coupled to another repository's version.
- Version the generator by SemVer. **MAJOR** is a breaking change: generated code that no longer
  compiles for an existing declaration, a removed or renamed attribute argument, a diagnostic
  reclassified to error, or a move of the minimum supported Microsoft.CodeAnalysis or Visual Studio
  baseline. **MINOR** is a new capability: a new attribute argument, a new generated member form, a
  new diagnostic, or support for a declaration shape that previously reported an error. **PATCH** is
  a bug fix: a wrong generated member, a false positive or false negative diagnostic, a packaging
  fix, or documentation.
- A generator that emits different code for the same declaration is a consumer-visible change even
  when the attribute surface is unchanged; the release notes must say what a regenerated project
  will see.

## Preparing a release

Before creating the tag:

1. Confirm `build-and-test` and `repository-policy` are green on `dev` and on the release branch.
2. Compose the pending fragments with `./scripts/Update-Changelog.ps1 -Compose` and edit the collected
   entries under `## [Unreleased]`: merge related entries, drop what a consumer cannot observe and fix
   the wording. Commit the result.
3. Close the section with `./scripts/Update-Changelog.ps1 -Finalize <version>`, where `<version>` is
   the tag without its `v` prefix, and commit the result together with the deleted fragments.
4. Confirm `## [Unreleased]` is empty and `changelog.d/` holds no fragment.
5. Merge the release branch into `main` with a merge commit, create the tag on that merge commit and
   push the tag.

The tagged commit already contains the release section in `CHANGELOG.md` with an empty
`## [Unreleased]` above it and no pending fragment; the workflow refuses to publish otherwise.

## Release metadata rules

- Never mark a stable version as a pre-release, and never give a pre-release a stable version number.
- Never delete, move or force-push a pushed release tag. A pushed tag is immutable; publish the next
  version instead. If a mistake is found before the tag is pushed and nothing was published, fix the
  commit, move the local tag and push it once.
- A package that was already published with a wrong version stays published; correct the content in
  the next version and explain it in the changelog.

## Package artifacts

- The publish workflow packs the packaging shell into `artifacts/`: one
  `VolocyNazad.Wpf.DependencyPropertyGenerator.<version>.nupkg`.
- The package carries `README.md` and the generator assembly under `analyzers/dotnet/cs`; the
  package metadata is declared in `src/Toolkit.Wpf.DependencyProperties/Toolkit.Wpf.DependencyProperties.csproj`.
  It declares no `lib/` dependency, because the attributes are generated into the consuming assembly.
- The workflow verifies the `.nuspec` package id, the version and the analyzer entry before pushing;
  a mismatch fails the release.

## Publish automation

The workflow runs on `workflow_dispatch`; a tag push alone does not publish. It:

- checks out with full history, because `MinVer` reads the tags;
- runs `./scripts/Verify-Release.ps1 -FromHeadTag -AncestorOf main -TagPrefix v`: `HEAD` must carry
  exactly one stable tag, `CHANGELOG.md` must have an empty `## [Unreleased]` and exactly one
  `## [VERSION] - YYYY-MM-DD` section for the tag directly below it, and `HEAD` must be an ancestor
  of `origin/main`;
- builds the solution in `Release` with warnings as errors and runs the headless tests;
- verifies the package id, the version and the analyzer entry, then pushes it to nuget.org.

After the pipeline, inspect the package on nuget.org: the version, the README and the
`analyzers/dotnet/cs` entry.

The rest of the contributor workflow is defined by the [development policy](development.md).
