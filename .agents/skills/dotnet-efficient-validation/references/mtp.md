# Discovery and execution with Microsoft.Testing.Platform

Choose the route from the effective configuration. Examples assume valid assets and a build; preserve `--configuration`, `--framework`, and `--runtime` when used in the build. Replace `<test-project>` with the project path.

## Native MTP via dotnet test

Use with .NET SDK 10+ and `test.runner` set to `Microsoft.Testing.Platform` in the effective `global.json`. Requires MTP 1.7+. Select a project with `--project` or a solution with `--solution`, instead of a positional argument.

```powershell
rtk dotnet test --project <test-project> --no-build --no-restore -- --list-tests
rtk dotnet test --project <test-project> --no-build --no-restore -- --minimum-expected-tests 1
```

The `--` separator is optional in this mode; in the examples, it delimits test application arguments. All selected projects must support MTP. [CLI reference](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-mtp).

## MTP through the legacy dotnet test integration

Use only when `dotnet test` is in VSTest mode and the MTP project has the `Microsoft.Testing.Platform.MSBuild` integration with `TestingPlatformDotnetTestSupport=true`. Forward MTP arguments **after `--`**. VSTest options such as `--logger` and `--filter` before that separator may be ignored.

```powershell
rtk dotnet test <test-project> --no-build --no-restore --nologo -v:minimal -p:TestingPlatformCaptureOutput=false -- --list-tests
rtk dotnet test <test-project> --no-build --no-restore --nologo -v:minimal -p:TestingPlatformCaptureOutput=false -- --minimum-expected-tests 1
```

`TestingPlatformCaptureOutput=false` makes the list and summary observable. This integration is not supported by the MTP 2 plus .NET 10+ combination; use the MTP executable if already configured, or report the incompatibility. [dotnet test modes and compatibility](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test).

## MTP executable

When the project already produces an MTP executable, run it without depending on the `dotnet test` integration:

```powershell
rtk dotnet run --project <test-project> --no-build --no-restore -- --list-tests
rtk dotnet run --project <test-project> --no-build --no-restore -- --minimum-expected-tests 1
```

You can also use `rtk dotnet <path/Tests.dll> --list-tests` and run the same DLL without `--list-tests`, provided it is the corresponding compiled MTP application. Build/restore arguments do not belong in this invocation.

With xUnit v3, `dotnet run` may use xUnit's own CLI. Confirm `UseMicrosoftTestingPlatformRunner=true` or the executable help before passing MTP options; merely referencing `xunit.v3` is insufficient. [xUnit integration with MTP](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform).

## Discovery, filters, and output

- Use `--list-tests` when you need to locate a test or diagnose discovery. If arguments are uncertain, consult the application's `--help` through the same route instead of `--list-tests`.
- Choose the filter accepted by the installed framework and version. MSTest/NUnit with VSTestBridge accept `--filter "FullyQualifiedName~Namespace.Class"`; xUnit v3 on MTP offers `--filter-class`, `--filter-method`, and `--filter-trait`; [TUnit uses `--treenode-filter`](https://tunit.dev/docs/execution/test-filters/). Confirm syntax in help, especially for parameterized names.
- Add the filter to application arguments in both discovery and execution. For example, in native MTP mode with xUnit:

  ```powershell
  rtk dotnet test --project <test-project> --no-build --no-restore -- --list-tests --filter-class "Namespace.Class"
  rtk dotnet test --project <test-project> --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "Namespace.Class"
  ```

- `--no-banner` reduces the MTP banner; use other output options only when announced by the installed version's help. `--logger "console;verbosity=minimal"` belongs to VSTest. Reports such as `--report-trx` depend on the corresponding installed extension.
- If RTK hides the list or summary, repeat only the affected discovery or command with `rtk proxy dotnet ...`. Preserve the exit code and check discovered, executed, failed, and skipped tests; all skipped tests do not validate behavior.
- Missing expected tests requires reviewing target, framework, build, filter, and runner. Keep `--minimum-expected-tests 1` during execution; do not use `--ignore-exit-code` or lower the minimum to turn missing tests into success. [MTP options](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-cli-options).
