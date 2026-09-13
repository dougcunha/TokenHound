# Foreign Stack Adaptation

Reached from the stack gate in step 1 when the detected stack is not .NET/C# and the user chose to continue. The six detection dimensions still hold — dead code, duplication, anti-patterns, type safety, code smells, and the report — but their evidence, their exclusions, and their thresholds are stack-specific. Build that calibration first, run the audit with it, then offer to keep it.

## 1. Pin the dialect

Name the stack down to the level that changes the commands: not "JavaScript" but "TypeScript / React / pnpm monorepo"; not "Delphi" but "Delphi VCL with DUnitX tests". Read the build manifests the step 1 sweep returned plus any repository instruction file (`CLAUDE.md`, `AGENTS.md`, `.github/copilot-instructions.md`) before deciding — they usually name the architecture the project believes it has.

**Done when** the stack, its framework, its test framework, and its package manager are recorded.

## 2. Build the strategy

Fill this table for the detected stack. Every row is what a step of the audit needs in order to run at all.

| Row | What to determine |
| --- | --- |
| Source extensions | Which files are compilable source, and which are generated, vendored, or build output (the exclusion globs) |
| Manifests | Build and dependency files that define module boundaries (`package.json`, `*.dpr`/`*.dproj`, `go.mod`, `pyproject.toml`, `pom.xml`) |
| Entry points | What anchors usage tracing: app entry, HTTP routes, DI or container registration, scheduled jobs, public module exports, CLI verbs, tests |
| Declaration evidence | What syntax declares a reusable symbol in this stack |
| Usage evidence | What proves a symbol is used — including the indirect paths that make a symbol look dead when it is not (reflection, string keys, markup, forms/designers, DI containers, dynamic imports, macro or annotation processing) |
| Duplication hiding places | Where this stack invites copies: utility units, mixins, partial classes, generated clients, per-module helpers |
| Live anti-patterns | The anti-patterns this stack actually suffers, and which of them do not apply (e.g. `ProjectReference` cycles are impossible in C# but routine in Delphi unit `uses` clauses) |
| Type-safety escape hatches | The stack's equivalents of `dynamic`, unchecked casts, and warning suppression — `any`/`as`/`@ts-ignore`, `Variant`/`Pointer`/`absolute`, `interface{}`/`Any`, raw pointers, `# type: ignore` |
| Smell thresholds | Line and parameter counts, nesting depth, and naming convention for this language's norms |
| Toolchain | The stack's own analyzers worth running read-only (`tsc --noEmit`, `ruff`, `go vet`, `staticcheck`, Delphi's compiler hints/warnings) |

Write every sweep as a PowerShell command driven by `ripgrep`, in the same shape as the .NET commands in `SKILL.md`: redefine the `$src` exclusion set for this stack's build output and generated code, then splat it into every sweep.

**Done when** every row is filled and each of steps 1–6 has at least one concrete command.

## 3. Confirm and run

Show the user the filled table and the commands before touching source, so a wrong exclusion glob or a missed indirect-usage path is caught while it is still cheap. Then run steps 1–6 of `SKILL.md` with these commands substituted, and steps 7–8 unchanged — the report and summary templates are stack-neutral apart from their examples.

**Done when** the audit has run to a written report.

## 4. Offer to keep it

Ask the user whether to save the strategy as a project skill named `architectural-analysis-<stack>` — `architectural-analysis-delphi`, `architectural-analysis-typescript`, `architectural-analysis-python`. On a no, stop here; the report is the deliverable.

On a yes, create `SKILLS/architectural-analysis-<stack>/` (or the path this repository already uses for skills) with:

- `SKILL.md` — this skill's step sequence with the stack's commands in place of the .NET ones, `name` and directory matching, and the step 1 stack gate inverted to detect that stack and hand a foreign stack back to `architectural-analysis`.
- `references/detection-catalog.md` — the `detection-catalog.md` sections rewritten from the strategy table: that stack's "Not dead" list, its duplication classes, its live anti-patterns, its escape hatches, its thresholds.
- `assets/report-template.md` and `assets/summary-template.md` — copied, with the example paths and section names redone in that stack's idiom.

If the repository carries a `bundles.yaml`, register the new skill under the bundle for that stack, creating the bundle when the stack has none.

**Done when** the new skill directory exists with all four files, its `name` matches its directory, and it is registered wherever this repository tracks skills.
