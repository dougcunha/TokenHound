# Architectural Analysis Report
**Date**: [timestamp]
**Solution / Projects**: [Solution.sln — N projects]
**Target framework(s)**: [net8.0]
**Files Analyzed**: X
**Dead Code Files**: Y
**Duplication Groups**: Z

---

## Executive Summary
- **Dead Code**: X files, Y members completely unreferenced
- **Duplicated Functionality**: Z duplication groups
- **Architectural Anti-Patterns**: W issues
- **Type & Nullability Issues**: V problematic usages
- **Code Smells**: U instances

**Estimated Cleanup**: Remove ~X lines of dead code, consolidate Y duplications

---

## Nullable & Analyzer Posture

| Project | `<Nullable>` | `<TreatWarningsAsErrors>` | `<LangVersion>` |
|---------|--------------|---------------------------|-----------------|
| `src/Acme.Api/Acme.Api.csproj` | enable | true | latest |
| `src/Acme.Legacy/Acme.Legacy.csproj` | *absent* | *absent* | default |

**Issue**: projects without `<Nullable>enable</Nullable>` have no null tracking; every finding in Type & Nullability below is unverified by the compiler there.

---

## Dead Code

### Completely Dead Files (DELETE)
| File | Reason | Confidence |
|------|--------|------------|
| `src/Acme.Legacy/LegacyOrderProcessor.cs` | No references; not DI-registered | HIGH |
| `src/Acme.Core/Helpers/UnusedStringHelper.cs` | Public but never referenced | HIGH |
| `src/Acme.Api/Services/TempSyncService.cs` | Left behind; no `AddHostedService` registration | HIGH |

**Total Lines**: X,XXX lines can be deleted

### Dead Members (REMOVE)
| File | Member | Reason |
|------|--------|--------|
| `src/Acme.Core/Formatting/DateFormatter.cs` | `FormatLegacyDate()` | Replaced by `FormatDate()`, no references |
| `src/Acme.Api/Services/AuthService.cs` | `LoginWithBasicAsync()` | Deprecated, no references in code or markup |
| `src/Acme.Core/Contracts/IOrderArchiver.cs` | whole interface | No implementation, no consumer |

### Possibly Dead (VERIFY)
| File | Member | Reason | Verification Needed |
|------|--------|--------|---------------------|
| `src/Acme.Core/Http/LegacyApiClient.cs` | `FetchLegacyAsync()` | Referenced only from commented-out code | Confirm the endpoint is retired |
| `src/Acme.Core/Options/ImportOptions.cs` | `BatchSize` | Bound by name only | Grep `appsettings*.json` for the key |

### Internal Dead Code
- `src/Acme.Core/Users/UserService.cs:125` — private method `ValidateLegacyDocument()` never called
- `src/Acme.Api/Endpoints/OrderEndpoints.cs:89` — local `tempPayload` assigned but never read
- `src/Acme.Core/Import/CsvReader.cs:47` — parameter `culture` accepted but never used

---

## Duplicated Functionality

### CRITICAL: Exact Duplicates

#### Duplication Group 1: Document (CPF) validation
**Instances**: 3
**Files**:
- `src/Acme.Core/Validation/DocumentValidator.cs:42` — `IsValidCpf(string cpf)`
- `src/Acme.Api/Filters/CpfFilter.cs:15` — `ValidateCpf(string value)`
- `src/Acme.Import/Rules/CpfRule.cs:67` — `CheckCpf(string input)`

**Analysis**: identical check-digit loop, same `Regex` for stripping punctuation
**Lines Duplicated**: ~22 lines × 3 = 66 lines
**Recommendation**:
- Keep: `src/Acme.Core/Validation/DocumentValidator.cs:IsValidCpf()`
- Remove: the other two
- Update: all call sites to the `Acme.Core` version

#### Duplication Group 2: HTTP error handling
**Instances**: 4
**Files**: [list]
**Analysis**: [similar]

### HIGH: Similar Logic

#### Duplication Group: Entity → DTO mapping
**Instances**: 2
**Files**:
- `src/Acme.Api/Mapping/OrderMapper.cs:30` — hand-written `ToDto()`
- `src/Acme.Api/Mapping/OrderProfile.cs:18` — AutoMapper `Profile` for the same pair

**Analysis**: two mapping mechanisms for one type pair; they already disagree on `Total` rounding
**Recommendation**: standardize on one mechanism, delete the other

### HIGH: Contract Duplication

#### Contract Group: Order payload
**Instances**: 3
**Files**:
- `src/Acme.Core/Contracts/OrderDto.cs` — `OrderDto`
- `src/Acme.Api/Models/OrderRequest.cs` — `OrderRequest` (identical members)
- `src/Acme.Import/Models/OrderData.cs` — `OrderData` (identical members)

**Recommendation**: one `OrderDto` in `Acme.Core.Contracts`, referenced by both consumers

---

## Architectural Anti-Patterns

### God Objects

#### `src/Acme.Api/Services/ApplicationService.cs` (850 lines, 11 injected dependencies)
**Responsibilities**: persistence, auth, configuration, logging, caching, validation
**Issue**: violates SRP; untestable without the whole container
**Recommendation**: split into `OrderService`, `AuthService`, `ImportService`, `CacheGateway`

### Dependency Cycles

#### Catch-all shared project: `Acme.Common`
- 7 of 9 projects reference `Acme.Common`
- `Acme.Common` holds contracts, helpers, EF entities, and HTTP clients together
**Issue**: the workaround for a cycle the compiler would reject — every project depends on every concern
**Recommendation**: split into `Acme.Contracts` (types only) and per-concern projects

#### Type cycle: `AuthService` ↔ `UserService`
- `AuthService` calls `UserService.GetByIdAsync`
- `UserService` calls `AuthService.ValidateToken`
**Recommendation**: extract the shared contract to an interface both depend on

### Tight Coupling

#### `src/Acme.Core/Import/ImportRunner.cs` → `System.Net.Http.HttpClient`
**Issue**: business logic constructing `new HttpClient()` and reading `DateTime.Now` directly
**Recommendation**: inject `IHttpClientFactory` and a `TimeProvider`

### Layer Violations

#### `src/Acme.Api/Controllers/OrdersController.cs` → `AcmeDbContext`
**Issue**: controller queries and calls `SaveChangesAsync` directly
**Recommendation**: route through the application service layer

#### `src/Acme.Domain/Acme.Domain.csproj` references `Microsoft.AspNetCore.App`
**Issue**: domain project depends on the web framework
**Recommendation**: remove the reference; move the HTTP-aware types out of the domain

### Service Locator / Lifetime Issues

| File | Line | Issue |
|------|------|-------|
| `src/Acme.Core/Import/ImportRunner.cs` | 61 | `serviceProvider.GetRequiredService<IOrderRepository>()` inside business logic |
| `src/Acme.Api/Program.cs` | 34 | `AddSingleton<CacheWarmer>` capturing scoped `AcmeDbContext` (captive dependency) |

### Async Misuse

| File | Line | Issue |
|------|------|-------|
| `src/Acme.Api/Services/NotificationService.cs` | 88 | `async void SendAsync(...)` — exceptions cannot be observed |
| `src/Acme.Import/Runner.cs` | 42 | `GetOrdersAsync().Result` — blocking on async, deadlock risk |
| `src/Acme.Core/Http/ApiClient.cs` | 27 | I/O method accepts no `CancellationToken` |

### Anemic Domain Model
- `src/Acme.Domain/Entities/Order.cs` — auto-properties only; all invariants enforced in `OrderService`

---

## Type & Nullability Issues

### Null-Forgiving `!` (X instances)

| File | Line | Context | Severity |
|------|------|---------|----------|
| `src/Acme.Api/Endpoints/OrderEndpoints.cs` | 45 | `order!.Customer!.Name` | HIGH |
| `src/Acme.Core/Import/CsvReader.cs` | 23 | `= record!` | HIGH |

**Recommendation**: replace each with a real null check or a non-nullable contract

### `dynamic` / `object` Usage (Y instances)

| File | Line | Context | Severity |
|------|------|---------|----------|
| `src/Acme.Core/Http/ApiClient.cs` | 45 | `dynamic response` | HIGH |
| `src/Acme.Import/Parser.cs` | 23 | `Parse(object data)` | MEDIUM |

### Unsafe Casts (Z instances)

| File | Line | Cast | Issue |
|------|------|------|-------|
| `src/Acme.Core/Http/ApiClient.cs` | 67 | `(User)payload` | No type check before cast |
| `src/Acme.Import/Parser.cs` | 89 | `row as OrderRow` | Null result never handled |

### Suppressions (W instances)

| File | Line | Suppression | Should Fix |
|------|------|-------------|------------|
| `src/Acme.Legacy/OldImporter.cs` | 1 | `#nullable disable` (whole file) | Enable and fix, or retire the file |
| `src/Acme.Api/Services/AuthService.cs` | 34 | `#pragma warning disable CS8618` | Initialize the field or make it nullable |

### Missing Precision
- `src/Acme.Domain/Entities/Order.cs:12` — `string CustomerId`; a `CustomerId` value object prevents mixing ids
- `src/Acme.Domain/Entities/Order.cs:18` — `decimal Total` with no currency
- `src/Acme.Import/Payload.cs:9` — `Dictionary<string, object>` payload instead of a typed record

---

## Code Smells

### Long Methods (>50 lines)

| File | Method | Lines | Issue |
|------|--------|-------|-------|
| `src/Acme.Import/ImportProcessor.cs` | `ProcessAsync()` | 127 | Does too much, hard to test |

**Recommendation**: extract smaller methods

### Long Parameter Lists (4+)

| File | Member | Params | Recommendation |
|------|--------|--------|----------------|
| `src/Acme.Core/Orders/OrderFactory.cs` | `Create(...)` | 7 | Group into a `CreateOrderCommand` record |
| `src/Acme.Api/Services/ApplicationService.cs` | constructor | 11 | Split the type |

### Complex Conditionals

| File | Line | Issue |
|------|------|-------|
| `src/Acme.Core/Validation/OrderValidator.cs` | 45 | Nested 4 levels deep |
| `src/Acme.Import/Parser.cs` | 89 | `switch` over 14 cases; wants polymorphism |

### Magic Numbers & String Keys

| File | Line | Magic Value | Should Be |
|------|------|-------------|-----------|
| `src/Acme.Core/Limits.cs` | 12 | `86400` | `const int SecondsPerDay` |
| `src/Acme.Api/Auth/PolicyNames.cs` | 34 | `"Admin"` repeated 9× | A `const` or existing policy constant |

### Swallowed Exceptions

| File | Line | Issue |
|------|------|-------|
| `src/Acme.Import/Runner.cs` | 96 | `catch (Exception) { }` — no logging, no rethrow |

### `#region` Blocks

| File | Line | Issue |
|------|------|-------|
| `src/Acme.Api/Services/ApplicationService.cs` | 120 | `#region Caching` hides 180 lines that want their own type |

### Commented-Out Code

**Files with commented code**: X
- `src/Acme.Legacy/OldImporter.cs` — 45 lines commented out
- `src/Acme.Api/Services/AuthService.cs` — previous implementation left in place

**Recommendation**: delete all commented-out code (git history preserves it)

### Naming

| File | Line | Issue | Should Be |
|------|------|-------|-----------|
| `src/Acme.Core/Helpers/Utils.cs` | 1 | `Utils` names no responsibility | Split by concern |
| `src/Acme.Core/Http/ApiClient.cs` | 27 | `GetOrders()` returns `Task` | `GetOrdersAsync()` |
| `src/Acme.Import/Runner.cs` | 15 | private field `count` | `_count` |

---

## Statistics

**Dead Code**:
- Files: X
- Members: Y
- Lines: Z (estimated)

**Duplication**:
- Groups: X
- Files affected: Y
- Duplicated lines: ~Z

**Architectural Issues**:
- God objects: X
- Dependency cycles / catch-all projects: Y
- Layer violations: Z
- Service locator + lifetime issues: W
- Async misuse: V

**Type & Nullability**:
- Projects without `<Nullable>enable</Nullable>`: X
- Null-forgiving `!`: Y
- `dynamic` / `object`: Z
- Unsafe casts: W
- Suppressions: V

**Code Smells**:
- Long methods: X
- Complex conditionals: Y
- Magic numbers / string keys: Z
- Swallowed exceptions: W

---

## Impact Assessment

### Code Cleanup Potential
- **Dead code removal**: ~X,XXX lines
- **Duplication consolidation**: ~Y,YYY lines
- **Total reduction**: ~Z,ZZZ lines (AA% of codebase)

### Maintainability Improvement
- Fewer places to update when fixing bugs
- Clearer project boundaries and responsibilities
- Compiler-enforced null safety instead of runtime surprises
- Reduced cognitive load

### Risk Areas
- High coupling in `src/Acme.Api/Services/`
- Null safety unenforced in `src/Acme.Legacy/`
- Layer violations between `Acme.Api` and `Acme.Domain`
- Deadlock risk on the blocking-async paths in `src/Acme.Import/`
