---
name: ai-ready-fix
description: AI Ready Fix corrects findings from AI-READY-SCORE.md and reassesses repository readiness for AI agents. Use to apply the audit or raise the score to 5. Do not use only to score (ai-ready-score) or to edit unrelated instructions.
---

# AI Ready Fix

Correct proven gaps in agent guidance while preserving project decisions. Aim for score 5 under the `ai-ready-score` rubric, without changing scope or inventing rules to obtain the score.

## 1. Establish the initial assessment

Read the `ai-ready-score` skill available in the environment in full and run its workflow on the target repository. It is the authority for the rubric and report; locate it in the catalog if needed. If unavailable, report the dependency and do not promise a validated score. Record the initial score, version, tools in scope, and findings by ID. Even with an existing report, check the current file state before planning fixes.

Check local changes and read existing instructions before replacing them. The result of this step is a list of findings that are still valid, each with evidence and affected criterion. If a confirmed score of 5 already exists, finish without rewriting files.

## 2. Resolve knowledge gaps

Explore only what the findings require: manifests/runtime, modules, commands and CI, code conventions, tests, and existing rules. Cite the paths supporting each conclusion. In large projects, use subagents for independent topics when available; in small projects or without delegation, perform the same investigation directly. Do not depend on specific tool names such as `Explore` or `AskUserQuestion`.

For each finding, define the smallest fix and its verification. Preserve valid specific rules, subtree differences, and tool adaptations. Ask questions only when a material decision cannot be inferred or is not already authorized; continue independent fixes while waiting. Leave unanswered gaps explicit, without turning them into facts.

Finish when every finding has an evidence-backed action or an identified dependency.

## 3. Correct content and distribution

Choose the existing authoritative source that best preserves valid rules; if none exists, prefer `AGENTS.md` at the root. Consolidate unique content before replacing duplicates. Use Delta/Frequency/Economy defined in `ai-ready-score`; when available, consult `writing-agents-md` for substantial writing or reduction.

Write commands with their working directory and relevant prerequisites. Distinguish commands checked in manifests/CI from those actually executed. Record validation applicable to the repository type. Compatibility, production, migration, and code-removal policies require evidence or an explicit user decision; do not insert a Greenfield Alpha block by default.

Move occasional lengthy procedures to the adopted on-demand mechanism. Subtree knowledge may live in local instructions; explanatory documentation may remain in a linked document. Create skills only for workflows with clear triggers and check their discovery in the tools in scope. If no mechanism exists, choose one supported by those tools without imposing another tool's exclusive directory. When creating skills, use `writing-skills` if available. In the resident source, leave only the necessary pointer with a reading condition.

Share common rules through a relative symlink or supported, verified import. Preserve specific adaptations in their scopes. Do not turn every nested instruction into a root alias. Do not confuse a file containing path text with a real symlink.

### File helpers

Replace `<skill-dir>` with this skill's absolute directory; provide absolute file and root paths, without `..` components. Both helpers require Python 3.9+ and use only the standard library.

Before any instruction write, run `python <skill-dir>/scripts/check_target.py <file> <repo>` (read-only). The helper checks the resolved destination, including parents that are links/junctions:

- `REAL`, `MISSING`, and `IN_REPO_SYMLINK`: the path remains in the repository; check content and consumers before editing it.
- `EXTERNAL_SYMLINK` or `EXTERNAL_PATH`: preserve the external target. A final link may be replaced with local content after preserving relevant rules; an external parent must be resolved before any write.
- `BROKEN_SYMLINK`, `INVALID_TARGET`, or an error: examine the target and fix the reference; do not write blindly through the path.

To create an alias, run `python <skill-dir>/scripts/symlink.py <link> <source> --repo-root <repo>` (mutation). The helper creates missing local parents, uses a relative target, is idempotent, and refuses to replace existing content without `--force`. Use `--force` only after consolidating unique content and checking the diff; authorization to fix already covers replacing identified duplicates. Conflicting local changes or content with unknown intent require clarification.

If the platform denies symlinks, use an import only when the tool has proven support. Otherwise, record the block and required alternative; do not elevate privileges or change system settings automatically. Link-creation failures preserve the existing file.

Finish with every fix implemented or blocked, external sources preserved, and all moved content accessible in the correct scope.

## 4. Verify and reassess

Review the diff to ensure valid rules were preserved and unsupported claims are absent. Check alias resolution, imports, document links, and skill discovery. Run validation proportional to the changes; do not install dependencies or run complete suites just to check instructions.

Run `ai-ready-score` again with the same rubric and tools. If fixable findings remain, return to the corresponding action and reassess after concrete changes. Finish with a proven score of 5 or explicit pending items when information, permission, or an unavailable resource is required. If the same finding reappears without progress, investigate the cause and report the limitation instead of repeating the cycle without changes. Never edit the score manually or loosen criteria to finish.

Deliver before/after score, changed files, validations performed, and pending items. Every ID from the initial assessment must be resolved with evidence or remain in the report with its reason and next step.

When modifying the helpers, run `python <skill-dir>/scripts/test_helpers.py` (tests in temporary directories).
