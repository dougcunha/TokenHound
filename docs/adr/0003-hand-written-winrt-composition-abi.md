# 0003: Hand-Written WinRT ABI Interop for Windows.UI.Composition

Referencing the `Microsoft.Windows.SDK.NET` WinRT projection via a versioned Windows target framework adds approximately 25.4 MB to the published 7.1 MB single-file executable. We therefore keep the `net10.0-windows` target framework and invoke the required `Windows.UI.Composition` vtable slots (`ICompositor`, `IVisual`, `ICompositionTarget`, `DispatcherQueueController`) directly via `Marshal.GetDelegateForFunctionPointer` in `HudBackdropComposition`, falling back cleanly to the solid `#18181B` fill on any initialization failure. Rejected alternatives include adopting a versioned Windows TFM or adding a Win2D dependency.

Origin: prd-17-hud-backdrop-effects, T02-ADR-01, artifacts up to d827da2
