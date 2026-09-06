---
name: ai-ready-score
description: AI Ready assesses repository readiness for AI agents with a score from 0 to 5 and verifiable findings. Use to audit instructions and skill loading or reassess ai-ready-fix corrections. Do not use to correct files or for code, security, or performance review.
---

# AI Ready Score

Assess the quality of the instructions an agent actually receives. The score measures guidance available in the repository, not software quality or guaranteed agent performance.

## 1. Define scope and inventory

Resolve the repository path and identify tools in scope from an explicit request or project configuration/documentation. Without sufficient evidence, use the interoperability profile with Claude Code, Copilot, Codex, and OpenCode and state that assumption. Keep all four tools in the report; tools outside scope are not applicable and do not lower the score. A shared file does not count as multiple independent contents.

Run `python <skill-dir>/scripts/discover.py <repo>` (read-only), replacing `<skill-dir>` with this skill's absolute directory. The inventory is evidence of files, not proof of loading. Read resident instructions and valid local targets once per source; open skills only as needed to check candidates and references. Inspect settings, imports, and subtree instructions that change effective content. External links are dependencies to record, not content to read automatically.

Check manifests, scripts, CI, and samples of relevant modules to validate commands and rules. Distinguish a command found in configuration from a command executed successfully. Do not run build/test or install dependencies just to assign a score. Record excluded directories and read errors; expand the directed search if an exclusion hides project code. If evidence is insufficient to decide a criterion, mark the assessment provisional.

Finish with declared scope, instruction inputs, identified effective sources, and explicit limitations.

## 2. Judge effective content

Use these criteria for each source and scope:

- **Delta:** does the rule change a project-based decision? Commands, paths, and conventions must match local evidence. Universal advice or invented claims do not qualify.
- **Frequency:** recurring rules belong in the narrowest resident scope that needs them. Occasional lengthy procedures belong in on-demand skills or references; a short critical exception may remain resident.
- **Economy:** conceptually remove repetitions and easily derived information. Do not require a number of lines, sections, or skills.
- **Single source:** common rules have one authoritative location. Valid symlinks or supported imports may share them; intentional tool/subtree differences are not duplication. An equal hash is an indication, not a verdict. A normal Markdown link does not prove automatic loading.
- **Operability:** inputs and references resolve, commands include relevant directory/prerequisites, and overlapping scopes do not contradict each other. The existence of `SKILL.md` content does not prove the tool discovers it.

Classify physical state (missing, regular, link, broken, external, unreadable) separately from quality. Record each issue with stable ID, `file:line` evidence, impact, affected criterion, and concrete action. Suggest extraction only when it provides benefit; choose subtree instruction, document, or skill according to the knowledge type.

Finish with each criterion supported by evidence or marked unverified.

## 3. Score and report

Rubric **v2**, cumulative from 1: choose the highest level whose requirements from 1 through that level are met. Score 0 applies when level 1 is not met. Apply the same scope in the initial assessment and reassessment.

| Score | Additional requirement |
|---|---|
| 0 | No usable instruction reaches the tools in scope (missing, empty, unreadable, or broken). |
| 1 | At least one tool receives non-empty instructions; content may still be generic. |
| 2 | At least one source contains project-specific, verifiable guidance. |
| 3 | All tools in scope receive specific guidance, with a single source for common rules and no known conflicts between scopes. |
| 4 | Guidance covers what is needed to work: available validation commands, a useful module map, and conventions that change decisions. Inputs, commands, and essential references were checked against the repository. |
| 5 | Resident context passes Delta/Frequency/Economy; relevant occasional procedures are available on demand, with triggers and references checked. There may be zero skills if none are needed. |

Missing build/test in a documentation repository is not automatically a defect: record the applicable validation method or the proven absence of automation. An unverified criterion cannot be declared met; present the proven score as provisional and state what remains to verify. Reports predating v2 must be recalculated before comparing scores.

Read [assets/report.template.md](assets/report.template.md) in full when producing the report. Fill the template and save `AI-READY-SCORE.md` at the root, unless the user requests conversation-only output or another path. This is the audit's only mutation. Before writing, check that the destination and its parents resolve inside the root; preserve external targets. If it cannot be written safely, deliver the report in the conversation and explain the limitation. Never alter instructions during the assessment.

Finish with score, rubric version, scope, evidence, limitations, and the first unmet requirement. A score of 5 requires every criterion to be verified, not merely an absence of findings.

## Helper failures and validation

The helper requires Python 3.9+ and uses only the standard library. An invalid path or access error produces an error and non-zero exit code; use the partial inventory, but do not treat it as complete. Correct the path or perform directed inspection with available tools. Do not turn a read error into an absence of instructions.

When modifying the inventory, run `python <skill-dir>/scripts/test_discover.py` (tests in temporary directories).
