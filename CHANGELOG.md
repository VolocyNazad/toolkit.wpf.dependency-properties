# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- A property that sets `Options` but not `DefaultUpdateSourceTrigger` failed at runtime: the generated
  `FrameworkPropertyMetadata` passed `UpdateSourceTrigger.Default`, which the WPF constructor rejects, so
  the type initializer of the declaring type threw. The registration now uses the five-argument
  constructor unless a trigger is specified explicitly.

## [1.0.0] - 2026-11-04

### Added

- Initial source generator: generates the `DependencyProperty` registration, the CLR wrapper or the
  `Get`/`Set` accessors, the XML documentation and the change callback wiring for
  `[GenerateDependencyProperty<T>]` and `[GenerateAttachedDependencyProperty<T>]`.
- Both callback forms are accepted: the canonical WPF
  `(DependencyObject, DependencyPropertyChangedEventArgs)` static method and the generated partial
  hooks; a method named for the property that matches neither form fails the build (`VDP0004`).
- Diagnostics `VDP0001`-`VDP0008` report an unusable declaration (a non-`partial` type, an attached
  property outside a static class, a generic or nested declaring type, an invalid property name, a
  missing named callback, a conflicting callback declaration) at compile time.
- Attribute arguments for the default value, read-only metadata, `FrameworkPropertyMetadataOptions`,
  the default update source trigger, `Category`, `Description`, `TypeConverter`, a shared callback
  name, the browsable-for type and raw XML documentation.
