# C#/.NET quality profile

Record the profile in the TechSpec so execution, orchestration, and review apply the same rules without rediscovering them. The profile is the feature's quality contract: the executor applies it while writing, and every downstream gate verifies it with the commands recorded here.

Select **only the rules the feature can violate**, typically five to eight. A feature without asynchrony does not carry the async rules; one that registers no services does not carry the DI rules. A profile that turns into a catalog is ignored from excess context; a short, relevant profile is followed.

## Classes

Two classes, with different destinations in review:

- **Blocking** — defect with a concrete failure path. A hit not justified in the TechSpec prevents task completion and rejects the review.
- **Reservation** — maintenance cost without a demonstrated failure. A hit goes to the report as an optional improvement and counts toward the escalation trigger; it never rejects on its own.

A rule may have a prior justification recorded in the TechSpec (`DEC-NN`): in that case the corresponding hit is expected and is not a finding. A justification applies to the specific spot, not to the whole file.

A hit already listed in the **Terrain baseline** is debt that predates the feature and is not a task finding: charging it to the executor punishes whoever touched the file last and turns the profile into noise. Only a hit the task introduced is a finding, or a pre-existing hit it aggravated — one more case in the saturated `switch`, one more dependency in the already large constructor. A target file absent from the baseline counts as unmeasured, and every hit in it is treated as new.

## Greppable set

Scoped to the files the task touched, never to the repository. Define the exclusions once:

```powershell
$src = @('-g','!**/bin/**','-g','!**/obj/**','-g','!**/*.g.cs','-g','!**/*.Designer.cs')
$files = @()   # the files in the task diff
```

**Blocking**

```powershell
rtk rg -n --type cs @src 'async void|\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' $files
rtk rg -n --type cs @src 'GetRequiredService<|GetService<|ServiceLocator' $files
rtk rg -n --type cs @src 'catch\s*\{\s*\}|catch \(Exception\w*\)\s*\{\s*\}' $files
rtk rg -n --type cs @src '#nullable disable|#pragma warning disable' $files
```

| Rule | Why it blocks |
| --- | --- |
| `async void` outside an event handler | the exception cannot be observed or awaited |
| `.Result` / `.Wait()` / `GetAwaiter().GetResult()` | deadlock in a synchronization context; the remedy is async all the way to the entry point |
| `GetRequiredService`/`GetService` in business code | the dependency disappears from the signature; the compiler stops flagging what the type needs |
| empty `catch { }` or `catch (Exception) { }` | the failure disappears from logs and behavior |
| `#nullable disable` / `#pragma warning disable` | silences a diagnostic the build would report; requires a `DEC-NN` naming the warning |

**Reservations**

```powershell
rtk rg -n --type cs @src 'throw new Exception\(|DateTime\.(Now|UtcNow)' $files
rtk rg -n --type cs @src '\w+\((?:[^),]+,){3,}[^)]*\)' $files
rtk rg -c '^' --type cs @src $files | Sort-Object { [int]($_ -split ':')[-1] } -Descending | Select-Object -First 5
```

| Rule | Threshold |
| --- | --- |
| `throw new Exception(` | generic type where a specific one expresses the failure |
| `DateTime.Now`/`UtcNow` in logic | an uninjected clock prevents deterministic tests; `TimeProvider` is the way out |
| Parameter list | 4+ parameters call for a `record`; a constructor with 6+ dependencies has too much responsibility |
| File size | above 500 lines |

The gate is asymmetric: a clean file returns empty output and consumes no context. Cost tracks the problems found, not the size of the code.

## Escalation

The profile never triggers a heavy audit inside the cycle. It accumulates counts so the review **suggests** one, with a concrete number, and the decision stays at the HIL. Objective triggers:

- eight or more reservation hits in the feature, or
- a touched file that crossed 500 lines, or
- the same symbol or block duplicated in three or more places in the diff.

Name the skill matching the signal when it is installed: duplication and coupling go to `refactoring-analysis`; dead code and cross-project dependencies go to `architectural-analysis`, whose report `sdd-plan-audit` turns into planned workstreams; full diff review goes to `deep-review`. With no trigger fired, suggest none.

## Recording in the TechSpec

Fill in the template's **Quality profile** section with the selected rules, each one's class, the command that verifies it, and the prior justifications. A rule absent from the profile is verified by no gate — the selection is the decision that matters. When `architectural-analysis` or `refactoring-analysis` are installed, their catalogs are the source of classification and severity; this file carries only the executable subset, so the TechSpec remains sufficient without them.
