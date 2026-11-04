; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
VDP0001 | Toolkit.Wpf.DependencyProperties | Error | The annotated type must be partial
VDP0002 | Toolkit.Wpf.DependencyProperties | Error | An attached dependency property requires a static type
VDP0003 | Toolkit.Wpf.DependencyProperties | Error | An instance dependency property requires an instantiable type
VDP0004 | Toolkit.Wpf.DependencyProperties | Error | The dependency property callback signature is not supported
VDP0005 | Toolkit.Wpf.DependencyProperties | Error | The named dependency property callback does not exist
VDP0006 | Toolkit.Wpf.DependencyProperties | Error | The declaring type shape is not supported
VDP0007 | Toolkit.Wpf.DependencyProperties | Error | The dependency property name is invalid
VDP0008 | Toolkit.Wpf.DependencyProperties | Error | The dependency property declares two change callbacks
