# WPF

Read this reference only when the target includes WPF views, view models, bindings, or window interop.

- Map bindings, commands, property change notifications, dispatcher usage, window lifecycle, and implicitly triggered effects.
- Reuse the MVVM structure and test framework already adopted. Introduce a new view model, service interface, or layer only when a dependency cannot be isolated with the existing structure and the trade-off is recorded.
- Keep logic in view models and `TokenHound.Core`; keep code-behind limited to view and window interop. Cover view model behavior with tests in `tests/TokenHound.Infrastructure.Tests/ViewModels/` before moving it.
- Prefer checking semantic view model state over rendered output. Binding paths in XAML are not checked by the compiler: treat renamed properties as explicit risks.
- Record a manual script for visual behavior, layout, animation, and window lifecycle that cannot be automated proportionally to risk.
- Treat binding errors, event cascades, focus, cross-thread updates outside the dispatcher, and layered window composition as explicit risks in the component and corresponding test cases.
