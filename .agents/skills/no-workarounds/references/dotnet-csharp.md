# .NET / C# root-cause fixes

## Establish the project's actual constraints

Inspect the affected `.csproj`, `global.json`, applicable `Directory.Build.props` / `.targets`, `Directory.Packages.props`, `NuGet.Config`, and `.editorconfig` when present. Identify target frameworks, SDK and language version, nullable mode, package versions, analyzers, and the failing CI command. Distinguish modern .NET from .NET Framework; use the repository's build tools and test runner. Do not require an SDK upgrade or enable nullable across a legacy solution merely to fix a local defect.

## D-01: Nullable contracts and type erasure

Treat `value!`, `null!`, `default!`, `dynamic`, and disabling nullable warnings as signals when they hide an unproven invariant. In C#, `as` performs a runtime type check and can return null; it is not a TypeScript assertion. Use a checked conversion or pattern matching when conversion is part of the contract.

For CS8602 or CS8618, establish whether absence is valid. Model optional values as nullable and handle absence; initialize required state through constructors or supported `required` members. `required` enforces initialization at C# call sites, not general runtime validation of external input. Validate deserialized or bound data before domain use. Never substitute an empty string or new entity just to satisfy the compiler when that state is invalid.

```csharp
// Hides a missing user and risks NullReferenceException.
var user = await repository.FindAsync(id, cancellationToken);
return user!.Name;

// When this application's contract maps absence to a domain error:
var user = await repository.FindAsync(id, cancellationToken);
if (user is null)
    throw new UserNotFoundException(id);
return user.Name;
```

The snippets are alternative method-body fragments; repository and exception names stand for application contracts. Map the domain error at the existing API boundary when applicable. Preserve nullable returns if absence is instead a valid result.

EF Core may initialize properties or translate expressions in ways nullable analysis cannot prove. A localized `null!` can express a documented framework guarantee; verify initialization and navigation loading before retaining it. Changing entity nullability can change the model and generated migrations, so inspect schema consequences. See [EF Core nullable reference types](https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types).

## D-02: Roslyn and build warning suppression

Investigate the exact diagnostic before adding `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, `#nullable disable`, or disabling analyzers / warnings-as-errors. Fix the contract or implementation when the finding is real. For a verified analyzer false positive or generated-code constraint, use the narrowest supported scope and document the evidence. A local suppression can be more accurate than a solution-wide `.editorconfig` change; ordinary style policy changes are outside this skill's scope.

## D-03: Sync-over-async and unowned work

When `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` blocks asynchronous I/O, trace the caller chain and propagate `await` and `CancellationToken` where supported. Wrapping the wait in `Task.Run` or adding `ConfigureAwait(false)` does not establish an end-to-end asynchronous contract. In ASP.NET Core, blocking request paths can exhaust thread-pool capacity; synchronization-context applications can also deadlock.

```csharp
// Blocks a request thread while waiting for I/O.
var response = client.GetAsync(uri).Result;

// Alternative in an async method that consumes the response here:
using var response = await client.GetAsync(uri, cancellationToken);
response.EnsureSuccessStatusCode();
```

Use `Task`-returning methods so callers can observe completion and failure. Keep `async void` for event-handler contracts that require it, with explicit error handling. For work that must outlive a request, use the application's managed queue/worker with owned scopes and shutdown handling; detached `Task.Run` must not capture request services. CPU-bound work offloaded from a UI thread is a legitimate use of `Task.Run`. See [ASP.NET Core best practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices).

## D-04: Cancellation, exceptions, and retries

Preserve failure semantics: `catch (Exception)` returning `null`, `false`, an empty list, or HTTP 200 can turn an infrastructure failure into a domain result. Catch expected exceptions at the boundary that can handle them. Use `throw;` when rethrowing the current exception to preserve its stack, or retain it as the inner exception when translating it.

Treat `OperationCanceledException` as expected cancellation only when the relevant operation's cancellation contract supports that interpretation. Propagate the supplied token to supported I/O; replacing it with `CancellationToken.None` hides ownership and shutdown failures. Log once at the responsible boundary rather than requiring every layer to catch, log, and rethrow.

Replace `Thread.Sleep` or arbitrary `Task.Delay` used for ordering with awaited completion or explicit coordination. Scheduled delays and bounded polling of an external readiness condition can be valid. Retry only classified transient failures, within a time/attempt budget, respecting cancellation and replay safety. Diagnose deterministic validation, authorization, lifetime, and query failures instead of retrying them.

## D-05: DI lifetime and disposal

For captive dependencies or `ObjectDisposedException`, trace who creates, owns, and disposes the service. Keep scope validation active; changing a scoped service to singleton or calling `BuildServiceProvider()` during registration can conceal the lifetime defect and create duplicate singleton instances.

Inject dependencies through constructors. In a singleton worker, create and dispose a scope per unit of work using `IServiceScopeFactory`, and await scoped work before disposing that scope. Resolve from that scope; do not cache its services after it ends. Let the container dispose instances it owns; dispose instances explicitly owned by the caller, including factory-created contexts. Scope/factory resolution at a lifecycle boundary is legitimate, unlike hiding arbitrary dependencies behind a global service locator. See [.NET DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines).

## D-06: EF Core concurrency, translation, and migrations

`DbContext` does not support concurrent operations on one instance. When a second operation starts before the first completes, await operations sequentially or give genuinely independent units of work separate contexts/scopes. Preserve transaction boundaries when splitting work. A global lock, disabled thread-safety checks, or catch-and-retry does not repair ownership. See [DbContext lifetime and threading](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/).

For LINQ translation failures, inspect the expression, provider, and generated SQL. Rewrite supported predicates/projections or use an appropriate mapped database operation. Moving `AsEnumerable()` / `ToList()` before filtering merely to stop translation errors may load an unbounded table. Explicit client evaluation is valid when the bounded result size and semantics are intentional.

For `DbUpdateConcurrencyException`, apply the domain's conflict policy: reject, reload and merge, or retry with refreshed state when replay is safe. For tracking conflicts, trace entity identity and context ownership; detaching everything or adding `AsNoTracking()` indiscriminately can discard intended updates. Read-only queries can legitimately use no tracking.

For schema drift, reconcile model, migration history, and actual schema. Review the migration and upgrade path on a disposable database. Replacing migrations with `EnsureCreated`, deleting applied migration history, or dropping a persistent database just to get a green run conceals the defect and can destroy data.

## D-07: Configuration, HTTP, and restore failures

- For missing settings, inspect provider precedence, environment, binding, and required-field validation. Use the project's options validation, including startup validation where supported, instead of a fabricated connection string or catch-and-default configuration.
- For HTTP exhaustion or stale DNS, inspect client/handler ownership. Use `IHttpClientFactory` or appropriately configured long-lived clients for the target runtime. Disabling TLS certificate validation hides trust failures; fix certificates and trust configuration.
- For restore/build failures, retain the diagnostic code and inspect SDK selection, target compatibility, package graph, feeds, and credentials without exposing secrets. Resolve the conflicting source/version instead of editing `obj/project.assets.json`, adding arbitrary DLL paths, suppressing NU1605, or changing the target framework just to pass. Clean generated artifacts only when evidence points to stale output; rerun the original command to verify the underlying problem is gone.

## D-08: Verification that exercises the defect

Use the existing xUnit, NUnit, MSTest, or other runner and CI configuration. Run the smallest relevant reproduction, then affected build/tests with the same target framework and configuration as the failure. Use `dotnet build` / `dotnet test` when supported; use the repository's MSBuild or runner for legacy projects. Verify discovered/executed test counts so a successful command with zero selected tests is not reported as regression coverage.

For SQL translation, constraints, transactions, or concurrency bugs, test against an isolated database using the production provider where feasible. EF Core InMemory, SQLite substitutes, and mocked `DbSet` queries do not prove another provider's behavior. Keep doubles for tests whose contract does not depend on provider semantics. See [EF Core testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy).

Coordinate asynchronous tests on observable completion; use the project's controllable clock (such as `TimeProvider` where supported) for time-dependent behavior. Fix shared fixture state instead of disabling all parallelism or skipping failing tests; serialization remains valid for inherently exclusive resources.

Report the cause, changed contract or ownership, and commands/results that verify it. If the required SDK, provider, or environment is unavailable, name the unverified behavior; do not switch to weaker assertions or claim the fix is verified.
