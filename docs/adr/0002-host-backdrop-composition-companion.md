# 0002: Acrylic via Host-Backdrop Composition Companion Window

The main HUD window (`NotchWindow`) must remain a layered WPF window (`AllowsTransparency="True"`) to preserve per-pixel click-through outside the contour, and WPF/DWM does not render system backdrops on layered windows. We therefore render the Windows 11 Acrylic material in a separate raw, input-transparent (`WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_NOREDIRECTIONBITMAP`), topmost companion `HWND` (`HudBackdropWindow` / `HudBackdropInterop`) using `Compositor.CreateHostBackdropBrush` through `CreateDesktopWindowTarget`, clipped by a polygon window region built from the flattened `HudContourGeometry` and synchronized directly below the main HUD in z-order (`HudBackdropController`). Rejected alternatives include undocumented `SetWindowCompositionAttribute` accent Acrylic, `DWMSBT_TRANSIENTWINDOW` with a 1-bit region, and self-drawn blur.

Origin: prd-17-hud-backdrop-effects, T01-ADR-01, artifacts up to d827da2
