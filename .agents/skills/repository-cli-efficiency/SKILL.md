---
name: repository-cli-efficiency
description: "Perform repository searches, grep, diff, and inspections with controlled scope and low verbosity. Mandatory when using rg, Select-String, git grep, git diff, gh api, recursive searches, or reviewing many files."
argument-hint: "Provide the repository, directory, or pattern to search."
user-invocable: true
---

# Efficient repository inspection

## When to use

Use this skill before recursive searches, `rg`, `Select-String`, `git grep`, `git diff`, `gh api`, or inspections of many files.

## Procedure

1. Restrict the search to the relevant directory or project.
2. Prefer `rg` to `Get-ChildItem -Recurse | Select-String`:

   ```powershell
   rtk rg -n -g '*.cs' -g '!**/bin/**' -g '!**/obj/**' 'pattern' <directory>
   ```

3. Combine related patterns in one pass:

   ```powershell
   rtk git --no-pager grep -n -I -E "ServidorCis|PortaDConnect|Historico|Observacao" -- "*.cs"
   ```

4. If only file names are needed, use `-l`. If few results are needed, limit output without rereading the entire repository.
5. For diffs, start with `rtk git diff --stat`, `--name-status`, and `--check`. Read the complete diff only for relevant files or sections.
6. Use `view_range` for known files. Do not format and print an entire file line by line when a range is sufficient.
7. Do not reread a large temporary file with a full PowerShell pipeline. Use `rg` with error patterns and result limits.
8. For `gh api`, prefer a known path, code search, or local data. `--jq` reduces output delivered to the agent, but a recursive endpoint still downloads and processes the entire tree.
9. Use `rtk proxy` only when complete output is genuinely necessary for diagnosis.

## Rules

- Always prefix shell commands with `rtk`, as required by the global instructions.
- Exclude at least `bin`, `obj`, `.git`, `packages`, and generated artifacts when the search does not need them.
- Do not run multiple serial repository-wide searches for patterns that can be combined.
- Do not use `Select-Object -Last N` as a substitute for a scoped or appropriately quiet command.
- Do not hide errors; reduce noise without discarding necessary context.

## Completion criteria

- The search traversed only the necessary scope.
- Related patterns were combined when possible.
- The first diff inspection was statistical and concise.
- Complete output was loaded only for relevant files or failures.
