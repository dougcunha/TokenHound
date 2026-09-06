# WinForms and DevExpress

Read this reference only when the target uses WinForms or DevExpress.

- Map events, bindings, validation, UI thread, control lifecycle, and implicitly triggered effects.
- Reuse the architectural pattern and test framework already adopted. Introduce a Presenter, view interface, or new layer only when a dependency cannot be isolated with the existing structure and the trade-off is recorded.
- Extract pure logic only when it reduces observable coupling; keep control adaptation at the UI boundary.
- For grids and reports, prefer checking semantic data before rendering. Use a snapshot or Golden Master only with a reviewed baseline and stable format.
- Record a manual script for visual behavior or lifecycle that cannot be automated proportionally to risk.
- Treat automatic binding, event cascades, focus, selection, sorting, and cross-thread updates as explicit risks in the component and corresponding test cases.
