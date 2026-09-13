# Architectural Analysis Complete

**Solution**: [Solution.sln — N projects, target framework]

## Dead Code Found
- **X completely dead files** — can be deleted
- **Y unreferenced members** — can be removed
- **~Z,ZZZ lines** of dead code identified

## Top Dead Files
1. `src/Acme.Legacy/LegacyOrderProcessor.cs` — no references, not DI-registered
2. `src/Acme.Api/Services/TempSyncService.cs` — never registered as a hosted service
3. `src/Acme.Core/Helpers/UnusedStringHelper.cs` — public but never referenced

## Duplication Found
- **X duplication groups** identified
- **Most duplicated**: CPF validation (3 copies)
- **Y contract duplications** — the same DTO declared per layer
- **~Z,ZZZ lines** of duplicated code

## Architectural Issues
- **X god objects** doing too much
- **Y layer violations** (controllers touching `DbContext`, domain referencing ASP.NET)
- **Z dependency cycles / catch-all shared projects**
- **W service-locator and lifetime issues** (incl. captive dependencies)
- **V async misuses** (`async void`, `.Result`, missing `CancellationToken`)

## Type & Nullability Issues
- **X projects without `<Nullable>enable</Nullable>`** — no compiler null tracking
- **Y null-forgiving `!`** — asserting non-null without proof
- **Z unsafe casts** — no type check before the cast
- **W suppressions** (`#nullable disable`, `#pragma warning disable`)

## Code Smells
- **X long methods** (>50 lines)
- **Y complex conditionals** (3+ nesting)
- **Z magic numbers and repeated string keys**
- **W swallowed exceptions** (`catch { }`)

## Cleanup Potential
Removing dead code and consolidating duplication could eliminate **~X,XXX lines** (Y% of the codebase)

**Full Report**: `.audits/architectural-analysis-[timestamp].md`
