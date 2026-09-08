# Repository Instructions

Write code, comments, documentation, and repository-facing text in English. The rules below apply to `*.cs` files.

## Project architecture & invariants

- Keep `TokenHound.Core` pure: zero UI or OS dependencies; models, contracts, and policies only.
- Never invent limits or denominators: if an API reports only remaining count, set `usedFraction` to `null`.
- Never write or refresh credentials owned by other tools; borrow read-only with `FileShare.ReadWrite | FileShare.Delete`.
- Open SQLite WAL databases using `Mode=ReadOnly`; fall back to `immutable=1` when `-shm` sidecar is missing.
- Persist and respect 429 rate-limit deadlines before dispatching network calls; never retry immediately on `Retry-After: 0`.
- Hook `WM_MOUSEACTIVATE` returning `MA_NOACTIVATE` (3) on HUD windows to prevent stealing focus on click or drag.
- In `WindowStyles.EnableNonActivating`, use `SWP_NOZORDER` (0x0004); omit `SWP_SHOWWINDOW` to preserve WPF layered window composition.

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

## Running & validating desktop HUD

- Launch `TokenHound.App` via Windows MCP `App` tool (`mode="launch_executable"`); shell `Start-Process` runs in an isolated desktop and will not render to the user screen.
- Verify HUD positioning with Windows MCP `Screenshot` on the primary monitor (`display: [2]`).

## Skills to load first

- `dotnet-efficient-validation` before any .NET build, test, publish, or run command.
- `repository-cli-efficiency` before repository searches, grep operations, or diff inspections.

## Documentation to read on demand

- Read `ARCHITECTURE.md` before changing solution structure, technologies, or dependencies.
- Read the relevant spec in `docs/specs/` before implementing or modifying provider adapters, rate limits, or credentials.
- Read `docs/design/` before implementing or styling HUD geometry, animations, or tooltips.

<!-- graft:start -->
## Graft — repo context graph

This repo is indexed in `graft/`: small linked markdown nodes that explain each
system and carry exact file:line spans, kept in sync with the code through git.

For ANY task here — understanding how something works, finding where code lives,
or scoping a change — get context from the graph before grepping or opening
source files. Re-ask freely (it's cheap) and reuse literal identifiers you
already have (symbol, error string, file name) as the query. New to this repo?
Run `graft map` first — a token-budgeted orientation (dir clusters, hubs,
hotspots), no LLM, no key.

- Run `graft ask "<your question>" --source` → ranked nodes with the relevant
  code spans inlined (each hit's ≤8-line crux by default; `--full` for whole
  definitions when the crux isn't enough). Match the tool to the task shape:
  for understanding or editing, the top node IS the answer — cite its
  `covers:` file:line spans and edit straight from `--source`. For
  exhaustive tasks ("every occurrence / every caller of this pattern"), ranked
  results are top-N, not complete — run `graft grep "<literal>"` instead
  (exhaustive over indexed files, grouped by enclosing symbol), falling back
  to raw `grep -rn` only for unindexed files.
- `graft skeleton <file>` → every definition's signature + span, ~10× cheaper
  than reading the file; use it to skim an API surface.
- `graft callers <symbol>` gives precomputed, exact edges — who calls this.
  Add `--direction out` for what it calls, or `--depth N` to walk
  transitively for the full blast radius. For structural questions, skip
  ranking and use this directly.
- Or browse: `graft/INDEX.md` lists every node; follow the links.
- Monorepos and folders of multiple repos rank fairly across sub-projects —
  hits carry `[scope/]` labels naming which one they're from. Narrow with
  `graft ask "<task>" --in <scope>/` once you know where you're working.

If a returned span is truncated ("+N more lines"), open the file at that exact
range before finalizing. Only open source files when a node genuinely lacks a
needed detail, and then at the exact file:line the node points to — never
re-read whole files.

After big code changes, refresh the graph with `graft build` (deterministic,
no API key, $0).
<!-- graft:end -->
