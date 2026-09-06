---
name: no-workarounds
description: No workarounds. Use when debugging code or test failures, planning a fix, or reviewing changes that may hide a defect, including .NET/C# nullability, async, dependency injection, and EF Core issues. Not for formatting- or docs-only edits.
metadata:
  author: Pedro Nauck
  github: https://github.com/pedronauck
  repository: https://github.com/pedronauck/skills
---

# No Workarounds

A workaround makes a failure disappear while leaving its cause intact. **Fix the source, not the signal.** Judge the violated contract and the evidence, not syntax alone: casts, null checks, retries, and framework initialization can be legitimate. Apply this distinction when reading the catalogs too.

## The gate: run before any fix

```
1. Capture the failing behavior or diagnostic and the expected contract.
2. Trace the relevant data flow, lifecycle, or configuration to a cause supported by evidence.
3. Check that the proposed fix repairs that cause while preserving valid behavior.

Silencing a signal -> redesign the fix against the root cause.
Cause still unknown -> gather evidence; do not present a hypothesis as a verified fix.
Root cause is external -> evaluate the escape valve.
```

Complete the fix when the original reproduction passes, relevant regression checks preserve the contract, and no remaining suppression or fallback conceals the defect. For a plan or review, identify the causal evidence and the checks required to verify the proposed fix; distinguish these from checks actually run.

## The seven signals

Use these signals to investigate a possible workaround. Confirm the defect before changing a valid language or framework idiom.

| Category | Suspected hidden defect | Fix the source by |
|---|---|---|
| **TYPE**: forced assertions, `dynamic`, unjustified `!` | Types conceal invalid or absent data | Correcting the contract, initialization, or boundary validation |
| **LINT**: diagnostic suppression, disabled analyzers | A real warning is hidden | Fixing the finding; for a proven false positive, documenting a narrowly scoped suppression instead of disabling checks broadly |
| **SWALLOW**: empty catch, catch-and-default | Failure becomes apparent success | Handling expected errors explicitly and propagating unexpected failures; preserving context at the responsible error boundary |
| **TIMING**: arbitrary delay, blocking async, blind retries | Ordering or ownership is wrong | Awaiting completion or coordinating readiness; retrying only identified transient failures with bounded, cancellation-aware policies |
| **PATCH**: global or library-internal mutation | A missing API contract is bypassed | Using a supported extension point, adapter, or upstream correction |
| **SCATTER**: fallback chains for required data | Invalid input escapes its boundary | Validating required data at entry; retaining null handling for genuinely optional values |
| **CLONE**: copy-and-tweak | A forced abstraction spreads the defect | Sharing behavior with the same contract, or separating distinct responsibilities |

## Select the relevant reference

- **For .NET/C# fixes or reviews, read [references/dotnet-csharp.md](references/dotnet-csharp.md) in full before choosing the fix.** It covers runtime and SDK compatibility, nullable contracts, async, DI, EF Core, configuration, and verification.
- For JavaScript/TypeScript signals, read [references/workaround-catalog.md](references/workaround-catalog.md) in full before choosing the fix. It contains W-01 through W-30 with examples. In mixed repositories, load both only when the defect crosses those stacks.
- For other stacks, apply the gate and seven signals directly; use the ecosystem's supported contracts and diagnostics.

## The escape valve

Not every root cause is yours to fix. A workaround is allowed only when ALL hold:

```
1. The root cause is in external code the team does not control.
2. The proper fix needs upstream changes on an uncertain timeline.
3. The business cost of not shipping exceeds the debt incurred.
4. The workaround is isolated; it does not leak into other code.
```

When all four hold, contain it:

```
1. Mark it: // WORKAROUND: [reason]; see [issue-link or local tracking path]
2. Record an owner and removal condition locally; create an external issue only when authorized.
3. Add a test that pins the current behavior.
4. Add a focused upstream probe that detects when the workaround can be removed.
5. Set a review date (max 90 days).
```

If any condition fails, fix the root cause. No exceptions.

## Foundations & rationalizations

When discussing the rationale or pressure to skip a root-cause fix, read [references/philosophical-foundations.md](references/philosophical-foundations.md). Treat its analogies as rationale, not evidence for a particular defect.

For provenance and upstream maintenance, read [NOTICE.md](NOTICE.md).
