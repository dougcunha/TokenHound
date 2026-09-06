---
name: update-nuget-packages
description: 'tight NuGet: updates .NET dependencies with dotnet outdated, validates restore/build/tests, synchronizes submodules, and chooses between fixing an authorized incompatibility or rolling back. Use when a .NET solution needs package updates or tests break after a package update. Don''t use to migrate a test project to MTP without package updates (use update-mtp-tests), for code changes unrelated to dependencies, or for rollback without explicit confirmation.'
argument-hint: 'Provide the .sln or .slnx solution; without a path, the skill selects one solution at the root or requests a choice.'
disable-model-invocation: true
---

# Update NuGet packages

Keep a **tight** NuGet update: one solution at a time, explicit restore, build without restore, and authorized incompatibilities.

## Invariants

- Discover updates only with `dotnet outdated`.
- Keep one solution in focus until restore, build, and tests finish.
- Treat migration from VSTest to MTP as `update-mtp-tests` work; a solution already on MTP is updated and tested normally by this skill, using MTP test syntax.

## Steps

### 1. Plan

1. Resolve the supplied solution as an absolute or relative path and accept `.sln` and `.slnx`.
2. Without a supplied solution, list only `.sln` and `.slnx` directly at the repository root. If there is one, select it; if there are several, request a choice; if there are none, request the path.
3. Detect `_lib` as a submodule or Git directory and locate its solution. Use `_lib/CoLib.Library.sln` when it exists; otherwise record the solution found.
4. Record the order: `_lib` solution when applicable, then the main solution.
5. Inspect the working tree of planned repositories. Classify each pre-existing change by path and preserve it.
6. Run `git symbolic-ref --quiet --short HEAD` in each repository before its respective `git pull --ff-only`. Stop on detached HEAD or any pull failure.
7. Record current package versions, `global.json`, SDK, and workflows that run tests.
8. For each solution, locate the `global.json` closest to its directory and record whether it already contains MTP `test.runner`. A configuration in `_lib` governs execution started in `_lib`, but not execution started at the repository root.

*Done when:* every planned solution, processing order, branch, pre-existing change, and dependency baseline is recorded.

### 2. Prepare tools

1. Load `dotnet-efficient-validation` before any `build`, `test`, `publish`, or `run`.
2. Run `rtk dotnet tool list --global` and confirm the `dotnet-outdated` command.
3. If absent, run `rtk dotnet tool install --global dotnet-outdated-tool` and confirm again. Stop when installation fails.

*Done when:* `dotnet outdated` is available and the .NET validation strategy is loaded.

### 3. Update one solution

1. Process only the next solution in the recorded order.
2. Run `rtk dotnet outdated -u <solution> --no-restore`.
3. Record each changed package as `package: previous version => new version`.
4. Classify major and prerelease updates, runner changes, and dependency warnings as compatibility risks.
5. When `xunit.runner.visualstudio` crosses from 3.x to 4.x in a solution still on VSTest, record it as a possible MTP migration trigger in the final report; do not change coverage packages or call `update-mtp-tests` automatically here.
6. Stop before the next solution when `dotnet outdated` fails, cannot resolve a dependency, or indicates a version incompatibility.

*Done when:* the current solution updated or confirmed all available packages, every version change and risk is recorded, and any possible MTP migration trigger is noted for the final report.

### 4. Restore and build

1. Run `rtk dotnet restore <solution> --nologo --verbosity:minimal` after any reference change.
2. Run `rtk dotnet build <solution> --no-restore --nologo --verbosity:minimal`.
3. Analyze errors and warnings. Remove a warning only when the fix is limited to update files, preserves API and behavior, and does not hide the diagnostic.
4. On restore or build failure, repeat only the necessary step with normal output or `rtk proxy`, preserve the first error, and stop the flow.

*Done when:* restore passed and the current solution build passed without errors, with every warning classified as resolved, existing, or blocking.

### 5. Validate tests

1. When the solution's `global.json` (step 1.8) does not declare MTP, run `rtk dotnet test <solution> --no-build --no-restore --nologo --logger "console;verbosity=minimal"`.
2. When the solution's `global.json` already declares MTP, run `rtk dotnet test --solution <solution> --no-build --no-restore --nologo --verbosity:minimal`; do not use `--logger`, which is VSTest-only.
3. Migrating a solution from VSTest to MTP is not this skill's responsibility. When the user requests that migration, read `SKILLS/update-mtp-tests/SKILL.md` in full and follow its steps; return to this skill only after MTP validation finishes.
4. Consider validation complete only with success and a count greater than zero. Explain projects without tests.
5. If a test fails, repeat only that test or project with enough output to identify the cause. After fixing an authorized incompatibility, repeat restore when references changed, then build and tests.
6. Classify a failure that passes in isolation and on suite repetition as flakiness; record the test and result without changing behavior to silence the signal.

*Done when:* all executable tests for the solution passed (VSTest, direct MTP, or through `update-mtp-tests` when migration was requested), the total count was recorded, and every warning, flakiness, or project without tests has an explanation.

### 6. Fix authorized incompatibilities

1. When an update causes a break, report package, project, error, and affected step.
2. Request a choice between fixing the incompatibility and rolling back to a pinned version. Continue only with explicit authorization; rollback is never automatic.
3. Apply the minimum compatible fix in files belonging to the update. For a break occurring only during an in-progress VSTest-to-MTP migration (global.json, coverage, xUnit v4 CS0619, workflow), handle it in `update-mtp-tests` instead of duplicating logic here.
4. After any fix, rerun only affected restore, affected build, and affected tests; then repeat the solution suite.

*Done when:* the incompatibility has an authorized minimum fix or explicitly documented rollback, and the affected solution passes again.

### 7. Finish

1. Run `rtk dotnet outdated <solution> --no-restore` to confirm no updates remain.
2. Review `git diff --stat`, `git diff --check`, `git status --short --branch`, and submodule status. Separate `_lib` changes from main repository changes.
3. When the update involves `_lib`, read `references/commit-template.md` in full and generate two commit messages without running `git commit`.
4. Report processed solutions, packages, files, builds, tests and counts, warnings, incompatibilities, possible MTP migration triggers noted in step 3, pending decisions, and commit messages.

*Done when:* the post-scan shows no outdated dependencies, every changed file is explained, and the summary distinguishes success, warnings, and unresolved items.

## Safety rules

- Preserve unrelated changes and never use destructive rollback.
- Treat a restore, build, or test failure as a block until its cause and decision are recorded.
- Pin a package version only in an authorized rollback, never as a shortcut around an incompatibility.
