# Detection Catalog — .NET / C#

Classification reference for `architectural-analysis`. Each SKILL step names the section to load when classifying that dimension's findings.

## Dead code

Categories:
- **Dead** — a `public`/`internal` type or member referenced nowhere; a class never instantiated and never registered in DI; an interface with no implementation or no consumer; a DTO or record never bound, mapped, or serialized; a constant never read; a file no other file references.
- **Possibly dead** (needs verification) — reached only from commented-out code, only from other dead code, only from another unused member, or only from tests covering a retired feature.
- **Internal dead code** — `private` method never called; field assigned and never read; parameter never used; unreachable `catch` or `switch` arm.

Not dead — treat as USED even with no compile-time reference:
- resolved from the DI container: `AddScoped`/`AddSingleton`/`AddTransient`/`AddHostedService`/`TryAdd*`, or registered by assembly scanning (Scrutor, `Assembly.GetTypes()`)
- instantiated by reflection: `Activator.CreateInstance`, `Assembly.Load`, `MakeGenericType`, `InvokeMember`, generic factories
- bound by name from configuration: `GetSection(...).Get<T>()`, `Configure<T>`, `IOptions<T>`, keys in `appsettings*.json`
- serialized: types and properties reached only by `System.Text.Json` or Newtonsoft, `[JsonPropertyName]`, `[JsonConstructor]`
- reached from markup: `.razor`, `.cshtml`, `.xaml`, `.axaml` bindings and `@inject`
- an interface implementation, `override`, or explicit interface member — the base declares the contract
- an attribute-driven entry point: `[Fact]`/`[Theory]`, `[HttpGet]`/`[HttpPost]`, `[ApiController]`, analyzer or source-generator attributes
- EF Core wiring: entity, `DbSet` property, migration, `IEntityTypeConfiguration<T>`, value converter
- the public surface of a class library published as a NuGet package, even when unused inside the solution
- half of a `partial` type whose other half is generated
- reachable because `InternalsVisibleTo` grants another assembly access

Confidence: **HIGH** (no reference anywhere, including markup and configuration), **MEDIUM** (only indirect or uncertain use), **LOW** (plausible reflective, DI-scanned, or serialization use).

## Duplication

Confirm by reading the implementations — same logic, same cases handled, one could replace the other — not by matching names. Generics, `partial` types, and extension methods hide duplication: the same algorithm under different type parameters still counts. Classify and rank:
- **Exact (CRITICAL)** — identical or near-identical code; a copy-pasted method, guard clause, or helper class. A bug fix means editing every copy.
- **Similar logic (HIGH)** — same algorithm, different implementation, parameters, or name. Inconsistency risk.
- **Conceptual (MEDIUM)** — competing ways to do one thing: two mappers, hand-rolled mapping beside AutoMapper/Mapperly, an `HttpClient` wrapper per feature, several `Result`/`Either` types, parallel validation approaches.
- **Contract (HIGH)** — the same shape declared repeatedly: a DTO, record, enum, or options class duplicated per layer or per project; types that should share a base or live in one shared project.

## Anti-patterns

- **God object** — file over 500 lines, type with 10+ public members, or a service or controller holding many responsibilities and constructor-injecting 6+ dependencies.
- **Circular dependency** — a `ProjectReference` cycle between projects; the compiler rejects these, so look for the workaround instead: a catch-all `Common`/`Shared` project everything depends on. Inside a project, look for type or namespace cycles A↔B.
- **Tight coupling** — high-level policy depending on a concrete infrastructure type instead of an abstraction; business logic constructing `HttpClient`, `SqlConnection`, `File`, `Environment`, or reading `DateTime.Now` directly instead of taking an injected abstraction.
- **Layer violation** — a controller, Razor page, or Blazor component touching `DbContext`/`SaveChanges`; an entity referencing a view model; a domain project referencing `Microsoft.AspNetCore.*` or a persistence package.
- **Service locator** — `IServiceProvider.GetService`/`GetRequiredService` inside business code where constructor injection belongs.
- **Captive dependency** — a singleton capturing a scoped or transient service; a `DbContext` held beyond its scope.
- **Static mutable state** — static properties with setters, static collections, static caches without synchronization.
- **Anemic domain model** — entities of auto-properties only, every rule living in a `*Service`.
- **Async misuse** — `async void` outside an event handler; `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` blocking on async work; I/O paths accepting no `CancellationToken`; a `Task` returned unawaited where exceptions must be observed.
- **Exception control flow** — `throw new Exception(...)` instead of a specific type, exceptions signalling expected outcomes, `catch { }` swallowing failures.
- **Shotgun surgery** — one feature change forces edits across many files (poor cohesion).
- **Feature envy** — a method using more of another type's data than its own.

## Type and nullability issues

- **Nullable posture** — `<Nullable>` absent or `disable` in a `.csproj` or `Directory.Build.props`: the project loses null tracking entirely. Report it once per project; it raises the severity of everything else in this section.
- **Null-forgiving `!`** — `x!.Y`, `= x!`: asserts non-null without proof and can hide a real `NullReferenceException`.
- **Suppression** — `#nullable disable`, `#pragma warning disable`, `[SuppressMessage]`: name the warning being silenced and say whether the underlying problem is fixed or merely hidden.
- **`dynamic` / `object`** — parameters, fields, or returns typed `dynamic` or `object`; decide whether a concrete type or a generic fits.
- **Unsafe cast** — `(T)value` with no preceding type check; `as T` whose null result is never handled.
- **Contract duplication** — the same DTO, enum, or options type across projects (cross-reference Duplication → Contract).
- **Missing precision** — primitive obsession where a value object fits (ids as `string`, money as bare `decimal`), string-typed enums, `Dictionary<string, object>` payloads, methods without an explicit return type contract.
- **Warnings not enforced** — `<TreatWarningsAsErrors>` absent, leaving nullability and analyzer findings advisory.

## Code smells

- **Long method** — over 50 lines; likely doing too much.
- **Long parameter list** — 4+ parameters; prefer a record or options object. Constructor-injecting 6+ services is the same smell at type level (see God object).
- **Complex conditional** — nesting 3+ deep, boolean expressions spanning lines, or a `switch` over 10+ cases that wants polymorphism or a pattern-matched expression.
- **Magic number/string** — unexplained literals, repeated string keys for configuration, claims, headers, or policy names; name them as `const` or reuse the framework's own constants.
- **Commented-out code** — delete it; git history preserves it.
- **`#region`** — hiding bulk that wants to be its own type.
- **Swallowed exception** — `catch { }` or `catch (Exception) { }` with no logging and no rethrow.
- **Poor naming** — context-free abbreviations (`usr`, `msg`, `tmp`), `Manager`/`Helper`/`Utils` types that name no responsibility, members that break C# convention (non-PascalCase members, private fields without `_camelCase`), async methods lacking the `Async` suffix.
