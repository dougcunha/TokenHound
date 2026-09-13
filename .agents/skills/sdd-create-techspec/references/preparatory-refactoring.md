# Preparatory refactoring

Measures the terrain where the feature will land and decides whether it needs preparing first. Fowler: *make the change easy, then make the easy change*. The question is not whether the existing code is bad — in a legacy codebase it almost always is — but whether **this** change becomes more expensive or riskier because of it. Ugly code the feature only reads is not this feature's problem.

The measurement also produces the **baseline**: the quality profile hits that already existed in the target files. Without it, every downstream gate blames the task for the debt it found, noise becomes routine, and the whole profile ends up ignored.

## Measures

Scoped to the files in the **Relevant files** section, never to the repository.

```powershell
$src = @('-g','!**/bin/**','-g','!**/obj/**','-g','!**/*.g.cs','-g','!**/*.Designer.cs')
$targets = @()   # files the feature will modify

rtk rg -c '^' --type cs @src $targets | Sort-Object { [int]($_ -split ':')[-1] } -Descending
rtk rg -c --type cs @src 'public (static |async |virtual |override |sealed )*[\w<>\[\], ]+ \w+\(' $targets
rtk rg -n --type cs @src 'public \w+\((?:[^),]+,){5,}[^)]*\)' $targets
rtk rg -c --type cs @src '^\s*case ' $targets
```

| Measure | Structural threshold |
| --- | --- |
| File lines | 500+ |
| Public members | 10+ |
| Constructor dependencies | 6+ |
| Cases in a `switch`/`if` chain over the same code | 10+ |

Also run the quality profile commands (`quality-dotnet.md`) over the same files: the result is the baseline, not a list of defects to fix.

## Decision

A crossed threshold is not enough. Debt matters when the feature **touches it**:

- **(a) Structural** — the target file crosses at least one threshold above.
- **(b) Contact** — the feature modifies that file in three or more distinct places, **or** extends exactly the saturated structure: one more case in the `switch` that already has ten, one more dependency in the constructor that already has six, one more method in the class that already has ten public ones.

| Situation | Destination |
| --- | --- |
| Only (a) | **Record** in the baseline and move on. The debt exists but does not hinder this change. |
| Only (b) | Nothing to do: intense contact with a healthy file is normal work. |
| (a) **and** (b) | **Recommend** preparatory refactoring, unless absorption fits. |

**Absorb** instead of recommending when the preparation is local and fits within the feature itself: extract a method, introduce a parameter object, isolate a dependency. The test is threefold — it does not change the public contract, does not require new characterization tests, and fits in one feature task. Record it as `DEC-NN` and handle it during implementation.

**Recommend** when the preparation changes a contract, requires characterization before mutating, or crosses several files. Name the minimal scope that makes the change easy — not the ideal refactoring of the file. A recommendation that rewrites the whole class is rightly refused; one that extracts the responsibility the feature will touch is accepted.

The recommendation is presented at the technical HIL and never blocks on its own. Approved, `sdd-plan-refactoring` generates the artifacts and the refactoring precedes the feature; refused, it becomes a risk recorded in the TechSpec and the baseline still applies.

## Recording

Fill in **Terrain baseline** in the template's Quality profile section: one item per target file, with measures, pre-existing hits, and destination. A target file without a row in the baseline is an unmeasured file — the downstream gate will treat every hit in it as new.

The baseline describes a state of the code, so it survives only as long as that state lasts. When a preparatory refactoring is approved and executed, remeasure the target files and rewrite the baseline before replanning the feature's tasks: keeping the old baseline would forgive hits the refactoring already eliminated. An external change to the target between the TechSpec and implementation has the same effect and calls for the same remeasurement.
