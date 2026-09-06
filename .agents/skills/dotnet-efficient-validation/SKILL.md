---
name: dotnet-efficient-validation
description: "Efficient .NET validation with low noise and no redundant work. Mandatory when running dotnet build, publish, or run, or discovering, listing, or running tests with VSTest or Microsoft.Testing.Platform (MTP). Do not use to migrate test frameworks."
argument-hint: "Provide the .NET solution, project, or test to validate."
user-invocable: true
---

# Efficient .NET validation

## When to use

Use this skill before any execution of:

- `dotnet build`;
- test discovery or execution with VSTest or Microsoft.Testing.Platform (MTP);
- `dotnet publish`;
- `dotnet run`;
- validation of a C# solution, project, or test.

## Procedure

1. Identify the directly affected project or solution. For tests, determine the runner under **Runner detection** before choosing arguments.
2. Prefer the most specific test. In VSTest, use `--filter`; for MTP, read [references/mtp.md](references/mtp.md) in full before listing or running tests.
3. If restore assets are missing or package references changed, restore once:

   ```powershell
   rtk dotnet restore <project-or-solution> --nologo --verbosity:minimal
   ```

4. With updated assets, build without restore only when a valid build does not already exist:

   ```powershell
   rtk dotnet build <project-or-solution> --no-restore --nologo --verbosity:minimal
   ```

5. Reuse the build only for the same code, configuration, framework, and runtime. In **VSTest**, run without rebuilding or restoring:

   ```powershell
   rtk dotnet test <project-or-solution> --no-build --no-restore --nologo --logger "console;verbosity=minimal"
   ```

   To discover names or investigate a filter with no matches, add `--list-tests` and check the listed tests. Listing does not replace execution. In MTP, use the command for the mode identified in the reference.

6. Run `dotnet publish` only when the published artifact or packaging validation is needed. Prefer `--no-restore`; use `--no-build` only when the corresponding build is already valid.
7. For `dotnet run`, use `--no-build --no-restore` when a valid build exists. Never use `Out-Null` as the primary diagnostic.
8. In PowerShell, preserve the exit code. After pipes or compound commands, check `$LASTEXITCODE` and propagate failures.
9. Use concise output options accepted by the runner. `--verbosity:minimal` and `--nologo` serve build and VSTest commands; MTP options are in the reference.
10. `Select-Object -Last N` reduces context received by the agent, but does not reduce process work and can hide a failure's cause. Use it only for supplemental inspection after preserving the command result.
11. If minimum validation fails, repeat only the necessary step with normal output or `rtk proxy`, preserving the first error. Do not rerun the entire suite without a reason.

## Runner detection

- Check the selected SDK with `rtk dotnet --version`, the effective `global.json`, and test projects in scope. Consider `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, and applicable imports.
- Look for `test.runner: "Microsoft.Testing.Platform"` in `global.json`, SDKs such as `MSTest.Sdk`, packages `Microsoft.Testing.Platform`/`.MSBuild`, `TUnit`, or `xunit.v3`, and properties `IsTestingPlatformApplication`, `EnableMSTestRunner`, `EnableNUnitRunner`, `UseMicrosoftTestingPlatformRunner`, and `TestingPlatformDotnetTestSupport`. Combine this evidence with effective versions and values; a package alone does not define the execution mode.
- Include MTP projects even without `Microsoft.NET.Test.Sdk`, a VSTest adapter, or explicit `IsTestProject=true`. The presence of those elements also does not exclude MTP.
- If imports or conditions leave doubt, query evaluated properties without running targets, preserving the target's configuration and framework:

  ```powershell
  rtk dotnet msbuild <test-project> -getProperty:IsTestProject,IsTestingPlatformApplication,TestingPlatformDotnetTestSupport,EnableMSTestRunner,EnableNUnitRunner,UseMicrosoftTestingPlatformRunner,TargetFrameworks
  ```

- Record the runner and route per project: VSTest, native MTP through `dotnet test`, MTP through legacy integration, or an MTP executable. In mixed solutions, run per project with compatible arguments. Preserve the existing configuration; validating tests does not require migrating the runner.

## Rules

- Always prefix shell commands with `rtk`, as required by the global instructions.
- Do not run the full suite when a specific project or filter meets the goal.
- Do not combine build and test so that the test recompiles the same code unnecessarily.
- Do not silence `build`, `test`, `publish`, or `run` with `Out-Null`.
- Do not treat reduced output as reduced compile, restore, test, or publish time.

## Completion criteria

- The executed scope corresponds to the changed behavior.
- The runner was identified and expected tests were executed. Zero tests, build-only, or listing-only do not prove validation; investigate discovery, filter, and execution mode before concluding.
- Restore, build, and tests were not repeated unnecessarily.
- Standard output is concise, while errors and exit codes remain observable.
- Publish or run were executed only when necessary.
