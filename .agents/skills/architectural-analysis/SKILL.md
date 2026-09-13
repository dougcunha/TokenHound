---
name: architectural-analysis
description: Read-only architectural audit of a .NET/C# codebase — dead code, duplication, anti-patterns, nullability and type safety, code smells — written to a report, never edited. Use when a whole solution or a large module needs an architecture-level health check; it detects the stack first and offers a derived strategy when that stack is not .NET/C#. Don't use for style/formatting, performance profiling, security audits, or feature-level code review.
disable-model-invocation: true
metadata:
  author: Pedro Nauck
  github: https://github.com/pedronauck
  repository: https://github.com/pedronauck/skills
---
# Architectural Analysis

Read-only audit of a whole .NET/C# codebase. Report findings only — make no edits. Classification depth for every dimension lives in `references/detection-catalog.md`; each step below names its section — read that section in full before classifying findings in that dimension.

Commands are PowerShell driven by `ripgrep`, run from the repository root. Set the exclusion set once per session and splat it into every sweep, improvised ones included — without it the sweeps report compiler output and generated code as source.
```powershell
$src = @('-g','!**/bin/**','-g','!**/obj/**','-g','!**/*.g.cs','-g','!**/*.Designer.cs')
```

## Steps

### 1. Map the codebase
Start from the build manifests: one sweep both names the stack and lists the projects.
```powershell
rtk rg --files -g '*.sln' -g '*.slnx' -g '*.csproj' -g 'Directory.Build.props' -g 'global.json' -g '*.dproj' -g 'package.json' -g 'pyproject.toml' -g 'go.mod' -g 'Cargo.toml' -g 'pom.xml' -g 'build.gradle*' -g 'Gemfile' -g 'composer.json'
```
A `.sln`, `.slnx`, or `.csproj` alongside `.cs` sources means **.NET/C#** — carry on below. Any other stack — or .NET on a non-C# language such as F# or VB.NET — falls outside this skill's calibration: name the stack you found, name the mismatch, and ask the user whether to continue anyway. On a yes, read `references/foreign-stack-adaptation.md` in full and follow it; it rebuilds the sweeps in steps 1–6 around the detected stack and ends by offering to save that strategy as `architectural-analysis-<stack>`.

On .NET/C#, count the compilable sources and build a todo with one item per `.cs` file. Note the entry points that anchor usage tracing: `Program`/`Startup`, DI registration extensions, controllers and minimal-API endpoint maps, `BackgroundService`/`IHostedService`, each class library's public surface, CLI verbs, test projects.
```powershell
(rtk rg --files -g '*.cs' @src | Measure-Object).Count
rtk rg -n --type cs @src 'class Program|static.*Main\(|MapGet|MapPost|MapControllers|ControllerBase|BackgroundService|IHostedService|AddScoped|AddSingleton|AddTransient'
```
**Done when** the stack is named and either it is .NET/C# or the user has answered the continue question, every source file has a todo entry, and the entry-point list is recorded.

### 2. Detect dead code
For each file in the todo: list the types and members it declares, then search each name for references elsewhere.
```powershell
rtk rg -n --type cs @src '\bSymbolName\b'
rtk rg -n -g '*.razor' -g '*.cshtml' -g '*.xaml' -g '*.axaml' -g '*.json' -g '*.xml' 'SymbolName'
```
Run both sweeps: C# symbols are reached from markup, configuration binding, and DI by name, so a `.cs`-only search reports live code as dead.

Before recording anything as dead, clear it against the "Not dead" list in `references/detection-catalog.md → Dead code` — DI registration, reflection, serialization, markup binding, interface implementation, attribute-driven entry points, and EF Core wiring all count as USED. Record each finding as `file:line`, category, and confidence per that section, then mark the todo item complete.
**Done when** every todo file's declared members are usage-checked and categorized.

### 3. Detect duplication
Surface candidates by similar names, repeated blocks, and competing implementations of one concept — C# scatters these across extension-method classes, helpers, mappers, and a `Utils` per project.
```powershell
rtk rg -n --type cs @src 'static .*(Validate|Parse|Format|Map|Convert|Normalize|Sanitize)\w*\('
rtk rg -n --type cs @src '(record|class|struct|interface|enum) \w*(Dto|Request|Response|Options|Settings|Result)\b'
```
Confirm each candidate group by reading the implementations, then classify and rank it using `references/detection-catalog.md → Duplication`.
**Done when** every candidate group is read and classified.

### 4. Detect anti-patterns
```powershell
rtk rg -c '^' --type cs @src | Sort-Object { [int]($_ -split ':')[-1] } -Descending | Select-Object -First 20
rtk rg -n -g '*.csproj' 'ProjectReference'
rtk rg -n --type cs @src -g '**/*Controller.cs' -g '**/Pages/**' -g '**/Components/**' -g '**/*.razor.cs' 'DbContext|IDbConnection|SqlConnection|SaveChanges'
rtk rg -n --type cs @src 'async void|\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)|GetRequiredService<|GetService<|ServiceLocator'
```
Read the largest files for mixed responsibilities; trace the `ProjectReference` graph for assembly-level cycles and the namespace graph for type-level ones; judge each layer and lifetime hit. Check findings against the full set in `references/detection-catalog.md → Anti-patterns`.
**Done when** each of the largest files is judged, the ProjectReference graph is traced, and every hit from the layer and async/lifetime sweeps is classified.

### 5. Detect type and nullability issues
Start from each project's nullable posture: `<Nullable>` absent or `disable` strips null tracking from the whole project and raises the severity of every other finding here.
```powershell
rtk rg -n -g '*.csproj' -g 'Directory.Build.props' '<Nullable>|<TreatWarningsAsErrors>|<WarningsAsErrors>|<LangVersion>'
rtk rg -n --type cs @src '#nullable disable|#pragma warning disable|SuppressMessage|\bdynamic\b|\(object |, object '
rtk rg -n --type cs @src '!\.|!\)|!;|!,| as [A-Z]\w*'
```
For each hit decide whether a precise type, a generic, or a real null check is possible, or whether a genuine error is being suppressed; classify per `references/detection-catalog.md → Type and nullability issues`.
**Done when** every project's nullable posture is recorded and every hit is judged.

### 6. Detect code smells
```powershell
rtk rg -n --type cs @src '^\s*//\s*(public|private|internal|protected|var|if|foreach|return|await|using)'
rtk rg -n --type cs @src '#region|catch\s*\{|catch \(Exception\w*\)\s*\{\s*\}|throw new Exception\('
```
Sweep for long methods, long parameter lists, complex conditionals, magic numbers and string keys, commented-out code, swallowed exceptions, `#region` hiding bulk, and naming that fights C# convention. Thresholds for each smell are in `references/detection-catalog.md → Code smells`.
**Done when** every smell category in that section has been swept.

### 7. Write the report
Populate `assets/report-template.md` and write it to `.audits/architectural-analysis-[timestamp].md`, filling every placeholder from steps 2–6. Keep every section present; where a count is zero, write "None found" rather than deleting the heading.
**Done when** every placeholder is replaced and every section is present.

### 8. Summarize for the user
Populate `assets/summary-template.md` and emit it inline in chat, linking to the full report at the end.
**Done when** the summary is in chat and carries the report path.

## Bundled files
- `references/detection-catalog.md` — .NET/C# classification depth for the five detection dimensions; each of steps 2–6 names its section.
- `references/foreign-stack-adaptation.md` — the non-.NET branch: build a stack-specific strategy, run it, and offer to save it as a derived skill. Reached only from the stack gate in step 1.
- `assets/report-template.md` — the full report written in step 7.
- `assets/summary-template.md` — the chat summary emitted in step 8.
