# Repository Instructions

Write code, comments, documentation, and repository-facing text in English. The rules below apply to `*.cs` files.

## Project architecture & invariants

- Keep `TokenHound.Core` pure: zero UI or OS dependencies; models, contracts, and policies only.
- Never invent limits or denominators: if an API reports only remaining count, set `usedFraction` to `null`.
- Never write or refresh credentials owned by other tools; borrow read-only with `FileShare.ReadWrite | FileShare.Delete`.
- Open SQLite WAL databases using `Mode=ReadOnly`; fall back to `immutable=1` when `-shm` sidecar is missing.
- Persist and respect 429 rate-limit deadlines before dispatching network calls; never retry immediately on `Retry-After: 0`.

## C# structure & style

- Keep one class per file, except nested types. Seal classes by default.
- Keep files and classes <= 300 lines, methods <= 30 lines, nesting <= 3 levels.
- Document public members with XML comments; use `<inheritdoc />` for inherited members.
- Use file-scoped namespaces, with alphabetized `using` directives above them.
- Name constants in `UPPER_CASE`. Use `nameof` instead of hard-coded strings. Do not use `#region`.
- Omit braces for single-line `if`/`else` bodies. Put `=>` on the next line for expression-bodied members.
- Separate members with one blank line; add one blank line immediately inside multi-line blocks and before control flow statements (`if`, `for`, `return`).
- Split calls with >= 4 arguments across lines: method name and opening parenthesis, one argument per line, closing parenthesis on its own line.

## C# practices

- Prefer `is null`, switch expressions, and collection expressions `[]`.
- Use records with `init` and `required` for DTOs and immutable messages.
- Mark anonymous functions `static` when they capture no state.
- Compare strings with `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`.
- Use structured logging with typed arguments. Persist configuration as JSON with `System.Text.Json`.
- Use `.ConfigureAwait(false)` in `Core` and `Infrastructure`; omit in UI sync contexts.
- Accept and propagate `CancellationToken` for asynchronous operations. Return tasks directly in pass-through methods.

## Running & reading MTP tests

- Run project-scoped tests using `rtk dotnet test --project <path> --no-build --no-restore -- --minimum-expected-tests 1` or `rtk dotnet run --project <path> --no-build --no-restore -- --minimum-expected-tests 1`.
- Pass MTP filter arguments after `--` (`--filter-class "*<Name>*"`, `--filter-method "*<Name>*"`, or `--list-tests`); do not use VSTest `--filter` or `--logger`.
- Always enforce `--minimum-expected-tests 1`; never treat zero executed tests as a pass.
- Inspect test failures directly from stdout; rerun with `rtk proxy` targeting the failing test if token filtering hides error details or stack traces.
- Preserve and verify `$LASTEXITCODE` after test commands; never suppress output with `Out-Null`.
- Follow the `dotnet-efficient-validation` skill (`references/mtp.md`) for runner detection, discovery flags, and extended options.

## Skills to load first

- `dotnet-efficient-validation` before any .NET build, test, publish, or run command.
- `repository-cli-efficiency` before repository searches, grep operations, or diff inspections.

## Documentation to read on demand

- Read `ARCHITECTURE.md` before changing solution structure, technologies, or dependencies.
- Read the relevant spec in `docs/specs/` before implementing or modifying provider adapters, rate limits, or credentials.
- Read `docs/design/` before implementing or styling HUD geometry, animations, or tooltips.
