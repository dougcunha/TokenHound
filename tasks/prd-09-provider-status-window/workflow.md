# Provider status window workflow

## REC-01 — Initial state

- Git base: `53da1817ebfc7f76110762dec6da664d0943affa`.
- Pre-existing uncommitted changes, unrelated to this feature: `.agents/skills/sdd-orchestrate-flow/SKILL.md`, `.agents/skills/sdd-orchestrate-flow/assets/checkpoint.template.json`, `.agents/skills/sdd-orchestrate-flow/references/hil-state.md`, untracked `.agents/skills/sdd-jev/`, `.agents/skills/sdd-triage/`, and `tasks/triage-log.jsonl` (written by this triage).
- No earlier checkpoint, PRD, TechSpec, or task plan existed for this request.
- Jev probe (`jev_noul`) succeeded with provider `openrouter`.

## DEC-01 — Triage level and jev mode (HIL 0)

- Decision: `sdd-full`, jev mode `shadow`.
- Scope: new window showing the status of every configured provider, grouped by provider with one row per account/profile; quota columns follow each provider's existing quota windows (not fixed); style per the reference screenshot (dark Claude multi-account panel).
- Evidence: rubric `sdd-full` on S1 (plan tier/account label absent from `src/TokenHound.Core/Models/Snapshot.cs:8-59`), S4 (Core, Infrastructure, App), S5 (open product decisions), S6 (>3 obligations). Jev `jev_decide` recommended `sdd-lean` (0.85) before the image evidence; not re-called. Record: `tasks/triage-log.jsonl` line dated 2026-09-27.
- Provenance: user answers "sdd-full (Recomendado)" at HIL 0; `--jev shadow` on the `/sdd-triage` invocation; clarification "As colunas devem seguir as existentes para cada provedor, e não são fixas. Na imagem de exemplo só tinha claude code, mas para outros provedores muda."

## DEC-02 — Product decisions before PRD drafting

- Decision: percentages show **used** (same semantics as the HUD's "% Used"); the window opens from the **tray menu** and the **HUD right-click menu**; each account row is identified by the **existing profile display name** (no plan tier, no email, no Core change); the reference's hover **arrow action is out of scope**.
- Scope effect: S1 from DEC-01 (Core `Snapshot` change) no longer applies; the change is expected to stay within `TokenHound.App` plus tests.
- Provenance: user answers "Usado (como o HUD)", "Menu da bandeja (Recomendado), Menu do HUD (clique direito)", "Nome do perfil (Recomendado)", "Fora do escopo (Recomendado)".

## DEC-03 — Product approval (HIL 1)

- Decision: The user approved `prd.md` at HIL 1 and chose to continue in this session.
- Scope: FR-01..FR-12, NFR-01..NFR-07 as written; assumptions on "configured provider", "exhausted", and culture date format accepted with the PRD.
- Approved SHA-256: `prd.md` `0922887f894bf851164f315ce9d348fafb2b5e612f1aea1bed78be0bfc1e1886`.
- Provenance: user answers "Aprovar" and "Continuar nesta sessão (Recomendado)".

## REC-02 — Technical plan prepared

- `techspec.md` (sha256 `9d385cda…55b5`), `tasks.md` (`2e575b71…3ab2`), `task_01.md` (`b014d7c9…a7b`), `task_02.md` (`e88db0da…6ea`) written from the approved PRD. No implementation code changed.
- Terrain measured over the target files. No structural threshold crossed; preparatory refactoring not recommended.
- Open item for HIL 2: TechSpec DEC-03 deviates from the NFR-07 literal by one line in `App.xaml.cs`.
- Jev shadow: J1 flagged NFR-07 `contradicted` (matches DEC-03) and US-01..US-05 `review`; J2 flagged FR-08 `review` (0.86 supports). Recorded in `jev-log.jsonl`; no effect in shadow.

## DEC-04 — Technical plan and execution approval (HIL 2)

- Decision: The user approved the TechSpec, the T01→T02 DAG, implementation, and corrections within those contracts, including TechSpec DEC-03 (`App.xaml.cs` grows by exactly one line, accepted deviation from the NFR-07 literal). The user chose to continue in this session.
- Approved SHA-256: `techspec.md` `9d385cda9abe8157bf84c7823fc0ea5d4cad589b997485d7b215be3d5d6355b5`; `tasks.md` `2e575b713f6f82a2e8a108274f2cb2140f7cd92c9e683d4d16490922f6403ab2`; `task_01.md` `b014d7c909a7337e88688f7e7318f1e348b9cfd258e1b28eb528f6a3b3951a7b`; `task_02.md` `e88db0da33768cf4774b4da5d5aa73a468344b76bdce7cb362afa1fad514d6ea`.
- Scope: T01 then T02, focused .NET tests and manual desktop acceptance via Windows MCP. No commit, push, or external publication authorized.
- Provenance: user answers "Aprovar" and "Continuar nesta sessão (Recomendado)".

## REC-03 — Implementation complete

- T01 and T02 are in `done/`, each with a handoff (`done/task_01.md`, `done/task_02.md`). Integrated state: App build 0 warnings, `tests/TokenHound.Infrastructure.Tests` 819 passed, `tests/TokenHound.Core.Tests` 92 passed.
- Manual acceptance by the coordinator via Windows MCP: MA-01..MA-04 pass. MA-05 is pending (no exhausted account available). MA-06 is partial (wrapping and scrolling pass; 150% scaling not verified). Both are open for HIL 3.
- During the test the user authorized closing the installed TokenHound (`D:/Apps/TokenHound`, PID 27980). It was relaunched afterwards (PID 8628), and the Codex enablement toggled in MA-04 was restored.
- This session authored all code. Next is `sdd-review-code` in a session that did not write it (independence rule).

## DEC-05 — End session before review

- Decision: The user chose to end this session and run `sdd-review-code` in a new session, which keeps the review independent.
- Provenance: user answer "Encerrar e revisar em nova sessão (Recomendado)".

## REV-01 — First review (`codereview_1`)

- Status: `REJECTED` (literal, `codereview_1/codereview.md`).
- Findings: CR-01 blocking (TC-05 and TC-09 lack the Copilot and Cline scenarios; T01 incomplete). CR-02 reservation (PRD UX alert colour for blocked accounts not carried into the TechSpec). CR-03 informational (DEC-06 earliest vs latest reset; product decision for HIL 3).
- Validations in the reviewing session: App and test builds 0 warnings; Infrastructure.Tests 819 passed; Core.Tests 92 passed; quality profile with no unjustified hit.
- Independence limitation: the review ran after `/clear` under the same host session ID as the implementation (`session_018B5BxLe8ZPhppy5EJH7b43`, DEC-05). The reviewing context held none of the authoring conversation and loaded only the snapshot sections permitted to an independent stage. Recorded in the report's limitations.
- Destination: correction round 1. CR-01 falls within DEC-04 (corrections within the HIL 2 contracts) and proceeds without a new gate. CR-02 and CR-03 are not planned and go to HIL 3 as decisions.

## REC-04 — Correction round 1 executed

- `codereview_1/done/task_03.md` (CR-01): Copilot and Cline scenarios added for TC-05 and TC-09; tests only, no `src/` change. Infrastructure.Tests 821 passed, Core.Tests 92 passed.
- The same context that issued `codereview_1` executed the correction. Under the independence rule, the re-review (`codereview_2`) must run in a new session.
- Jev shadow: J4, J5, J6, and J3 recorded in `jev-log.jsonl`; no effect.

## DEC-06 — End session before re-review

- Decision: The user chose to end this session and run the re-review (`codereview_2`) in a new session, which keeps it independent of the correction author.
- Provenance: user answer "End and re-review in new session (Recommended)".

## REV-02 — Re-review (`codereview_2`)

- Status: `APPROVED WITH RESERVATIONS` (literal, `codereview_2/codereview.md`).
- Previous findings: `codereview_1/CR-01` resolved by T03; `codereview_1/CR-02` persists as `codereview_2/CR-01` (reservation); `codereview_1/CR-03` persists as `codereview_2/CR-02` (informational, extended with the fraction-less Cline block observation).
- Validations in the reviewing session: App and test builds 0 warnings; Infrastructure.Tests 821 passed; Core.Tests 92 passed; quality profile with no unjustified hit; no escalation trigger.
- Independence limitation: after `/clear`, under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that authored T01/T02 and T03 (DEC-05, DEC-06). The reviewing context held none of those conversations. Recorded in the report's limitations.
- Destination: reservations HIL for `codereview_2/CR-01`; `CR-02`, MA-05, and MA-06 (150%, ≥ 10 rows) go to HIL 3.
- Jev shadow: J4, J5, and J7 recorded in `jev-log.jsonl`; no effect.

## DEC-07 — Reservations HIL: correct CR-01 (`codereview_2`)

- Decision: correct `codereview_2/CR-01` in correction round 2, limited to that finding; continue in this session. `codereview_2/CR-02` and the manual items stay for HIL 3.
- Scope: an account-level blocked flag and the alert colour for its status message, in `ProviderStatusAccount`, `ProviderStatusProjection`, and `ProviderStatusWindow.xaml`, plus unit tests. This is new scope beyond the HIL 2 contract (a PRD UX bullet), authorized by this decision.
- Independence: this session issued `codereview_2` and will execute the correction, so `codereview_3` must run in another session.
- Jev shadow: J7 recommended `correct-cr01` (0.60), with a warning about a second change if DEC-06 changes at HIL 3; recorded, no effect.
- Provenance: user answers "Correct CR-01 now" and "Continue in this session (Recommended)".

## REC-05 — Correction round 2 executed

- `codereview_2/done/task_04.md` (CR-01): `ProviderStatusAccount.IsBlocked` (`RateLimited`/`AccessDenied` or `ActiveBlock.IsBlocked`) and an alert-colour trigger on the status message. App build 0 warnings; Infrastructure.Tests 828 passed; Core.Tests 92 passed.
- The same context that issued `codereview_2` executed this correction. Under the independence rule, `codereview_3` must run in a new session.
- New HIL 3 manual item: see the alert-coloured status of a rate-limited or access-denied account on screen.
- Jev shadow: J6 and J3 recorded in `jev-log.jsonl`; no effect.

## DEC-08 — End session before re-review

- Decision: The user chose to end this session and run the re-review (`codereview_3`) in a new session, keeping it independent of the T04 author.
- Provenance: user answer "End and re-review in new session (Recommended)".

## REV-03 — Re-review (`codereview_3`)

- Status: `APPROVED WITH RESERVATIONS` (literal, `codereview_3/codereview.md`).
- Previous findings: `codereview_2/CR-01` resolved by T04; `codereview_2/CR-02` persists as `codereview_3/CR-01` (informational: DEC-06 earliest reset, fraction-less block not exhausted).
- Validations in the reviewing session: App and test builds 0 warnings; Infrastructure.Tests 828 passed; focused `*ProviderStatus*` 42 passed; Core.Tests 92 passed; quality profile with no unjustified hit; no escalation trigger. Only the four T04 files changed after `codereview_2`.
- Reserved items: `codereview_3/CR-01` and the manual items not verifiable by review (MA-05, MA-06 150% and ≥ 10 rows, the T04 alert-colour visual).
- Independence limitation: after `/clear`, under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that authored T01–T04 and issued `codereview_1`/`codereview_2` (DEC-05, DEC-06, DEC-08). The reviewing context held none of those conversations. Recorded in the report's limitations.
- Destination: reservations HIL for `codereview_3/CR-01`; the manual items go to HIL 3.
- Jev shadow: J4 flagged NFR-04 and NFR-06 `contradicted` (the report has them `not verifiable`), and FR-12 and NFR-07 `review`. J5 classified CR-01 `informational` (auto, matching the report). J7 recommended `finalize-keep-dec06` (0.67), warning that it does not meet "comeback never earlier than actual". Recorded in `jev-log.jsonl`; no effect.

## DEC-09 — Reservations HIL: finalize (`codereview_3`)

- Decision: finalize the review cycle. `codereview_3/CR-01` is accepted as an open item: TechSpec DEC-06 stays as approved (earliest reset among exhausted windows; a fraction-less block is not exhausted). Proceed to step 6 acceptance in this session.
- Scope: no correction round 3; the manual items (MA-05, MA-06 150% and ≥ 10 rows, the T04 alert visual) go to HIL 3.
- Provenance: user answers "Finalize, keep DEC-06 (Recommended)" and "Continue in this session (Recommended)".

## DEC-10 — HIL 3 not accepted: visual changes to the status window (exception HIL)

- Decision: The user did not accept the delivery at HIL 3 and asked for visual changes to `ProviderStatusWindow`:
  1. Title bar in the body colour. Chosen: keep the native title bar, painted with the body colour and light text through the Windows 11 DWM caption attributes. Windows 10 falls back to the existing dark title bar.
  2. More prominent provider group titles without a larger font. Chosen: a bold family name with a thin blue accent bar on the left, and a subtle separator line under the header.
  3. A better count next to the name. Chosen: a small rounded grey badge, vertically centred.
- Scope: new scope beyond the HIL 2 contract (PRD NFR-04 visual style), authorized by this decision. Planned as correction T05 in `codereview_3/task_05.md` (correction round 3). Out of scope: other layout, data, or behaviour changes.
- Independence: this session issued `codereview_3` and will implement T05, so the re-review `codereview_4` must run in another session.
- Provenance: user answer at HIL 3: "Melhorar a parte visual da janela. Ajustar a barra de titulo para ficar da mesma cor do corpo ou tirar a barra e colocar um botão para fechar. Melhorar o titulo os provedores para dar mais destaque (sem aumentar o tamanho). Não gostei de como ficou o numero na frente, melhore isso também." Then at the exception HIL, answers "Native bar, body colour (Recommended)" and "Accent bar + count badge (Recommended)", and "Continue in this session (Recommended)".

## REC-06 — Correction round 3 executed

- `codereview_3/done/task_05.md` (DEC-10): the status window's native caption and border are painted with the body colour and light text via DWM (Windows 11), with the dark-mode fallback. Group headers get a blue accent bar, a bold name at the same 16 px, a rounded count badge, and a separator. App build 0 warnings; Infrastructure.Tests 828 passed; Core.Tests 92 passed; QA clean.
- Manual MA-07 seen on screen via Windows MCP: the caption and body pixels are both #18181B. The user authorized closing the installed instance; it was relaunched afterwards (PID 4060).
- Observation for HIL 3, outside DEC-10: the vertical scrollbar keeps the default light WPF style.
- The same context that issued `codereview_3` executed this correction. Under the independence rule, `codereview_4` must run in a new session.
- Jev shadow: J6 and J3 recorded in `jev-log.jsonl`; no effect.

## DEC-11 — End session before re-review

- Decision: The user chose to end this session and run the re-review (`codereview_4`) in a new session, keeping it independent of the T05 author.
- Provenance: user answer "End and re-review in new session (Recommended)".

## REV-04 — Re-review (`codereview_4`)

- Status: `APPROVED WITH RESERVATIONS` (literal, `codereview_4/codereview.md`).
- Previous findings: `codereview_3/CR-01` persists as `codereview_4/CR-01` (informational, accepted in DEC-09, DEC-06 unchanged). New `codereview_4/CR-02` (informational): the status window's `ScrollViewer` uses the default light WPF scrollbar on the dark body (`ProviderStatusWindow.xaml:111-112`; no scrollbar style in `DialogResources.xaml` or `App.xaml`); outside the T05/DEC-10 boundary, a visual decision for the human.
- T05 verified: only its three files changed after `codereview_3`; DWM caption/border/text attributes 35/34/36 with the `EnableDarkMode` fallback; header accent bar, bold 16 px name, count badge, separator; T04 trigger intact.
- Validations in the reviewing session: App and Infrastructure.Tests rebuilt non-incrementally with 0 warnings; Infrastructure.Tests 828 passed; focused `*ProviderStatus*` 42 passed; Core.Tests 92 passed; quality profile with no unjustified hit; no escalation trigger.
- Reserved items: `codereview_4/CR-02`; manual items not verifiable by review (MA-05, MA-06 150% and ≥ 10 rows, the T04 alert-colour visual, the T05 Windows 10 fallback).
- Independence limitation: after `/clear`, under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that authored T01–T05 and issued `codereview_1`..`codereview_3` (DEC-05, DEC-06, DEC-08, DEC-11). The reviewing context held none of those conversations. Recorded in the report's limitations.
- Destination: reservations HIL for `codereview_4/CR-02` (CR-01 already decided in DEC-09); the manual items go to HIL 3.
- Jev shadow: J4 verified 17 of 22 rows; its `contradicted` rows (FR-10 and T04 on-screen visuals, NFR-04, NFR-06, TC-11) match the report's `not verifiable`/`pending` states, and FR-09 came back `review`. J5 classified CR-02 `informational` (auto) and CR-01 `informational` in a 0.50/0.48 tie with `reservation` (review). Recorded in `jev-log.jsonl`; no effect.

## DEC-12 — Reservations HIL: correct CR-02 and extend to Settings (`codereview_4`)

- Decision: correct `codereview_4/CR-02` in correction round 4: a slim dark scrollbar for the provider status window. The user extended the scope: the Settings window gets the same dark scrollbar. Continue in this session.
- Scope: a shared dark `ScrollBar` style in the dialog palette, applied to `ProviderStatusWindow` and `SettingsWindow`. New scope beyond the HIL 2 contract (PRD NFR-04 polish, and Settings is outside this PRD), authorized by this decision. Out of scope: the HUD, tooltips, About, other controls, layout, data, and behaviour. `codereview_4/CR-01` stays accepted (DEC-09).
- Independence: this session issued `codereview_4` and will make the correction, so the re-review `codereview_5` must run in another session.
- Provenance: user answers "Correct CR-02 (Recommended)" and "Continue in this session and do the same to the settings windows".

## REC-07 — Correction round 4 executed

- `codereview_4/done/task_06.md` (CR-02 + Settings, DEC-12): a keyed `DialogScrollBarStyle` in `DialogResources.xaml` (8 px, no arrows, transparent track, rounded thumb #3F3F46 / hover #52525B / drag #71717A, horizontal template). `ProviderStatusWindow.xaml` and `SettingsWindow.xaml` opt in with an implicit style based on it. XAML only. App build 0 warnings; Infrastructure.Tests 828 passed; Core.Tests 92 passed.
- Manual MA-08 seen on screen via Windows MCP: both windows show the slim dark scrollbar, and wheel and drag scrolling work. The user authorized closing the installed instance; it was relaunched afterwards (PID 22344). The T04 alert-coloured status was also seen on screen: a real `Claude OAuth API rate limit exceeded (HTTP 429)` on the `Claude Code` row.
- The same context that issued `codereview_4` executed this correction. Under the independence rule, `codereview_5` must run in a new session.
- Jev shadow: J7, J6, and J3 recorded in `jev-log.jsonl`; no effect.

## DEC-13 — End session before re-review

- Decision: The user chose to end this session and run the re-review (`codereview_5`) in a new session, keeping it independent of the T06 author.
- Provenance: user answer "End and re-review in new session (Recommended)".

## REV-05 — Re-review (`codereview_5`)

- Status: `APPROVED WITH RESERVATIONS` (literal, `codereview_5/codereview.md`).
- Previous findings: `codereview_4/CR-02` resolved by T06 (`DialogScrollBarStyle`, opt-in in the status window and Settings). `codereview_4/CR-01` persists as `codereview_5/CR-01` (informational, accepted in DEC-09, DEC-06 unchanged).
- T06 verified: only its three XAML files changed after `codereview_4`, and no `.cs` changed. Vertical and horizontal track directions and page commands are correct, the drag state takes precedence over hover, all brushes resolve, and the only implicit `ScrollBar` styles are the two opt-ins. The T04 trigger is intact.
- Validations in the reviewing session: App and Infrastructure.Tests rebuilt non-incrementally with 0 warnings; Infrastructure.Tests 828 passed; focused `*ProviderStatus*` 42 passed; Core.Tests 92 passed; `git diff --check` clean; quality profile with no unjustified hit; no escalation trigger.
- Reserved items: only `codereview_5/CR-01`, already decided in DEC-09, so no new reservations question applies (DEC-09 reused). Manual items not verifiable by review go to HIL 3: MA-05, MA-06 (150% scaling and ≥ 10 rows), the T05 Windows 10 fallback, and the T06 horizontal scrollbar and Settings `Cadence & Rate Limits` tab.
- Independence limitation: after `/clear`, under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that authored T01–T06 and issued `codereview_1`..`codereview_4` (DEC-05, DEC-06, DEC-08, DEC-11, DEC-13). The reviewing context held none of those conversations. Recorded in the report's limitations.
- Destination: step 6 acceptance, HIL 3.
- Jev shadow: J4 verified 19 of 25 rows. Its `contradicted` rows (FR-10, the DEC-10 Windows 10 fallback, NFR-04, NFR-06, TC-11) match the report's `not verifiable`/`pending` states. FR-08 came back `unsupported` (0.40), where the report has `conformant`. J5 classified CR-01 `informational` (auto 0.99), matching the report. Recorded in `jev-log.jsonl`; no effect. `jev-summary.md` was rewritten for HIL 3.

## DEC-14 — HIL 3: delivery accepted

- Decision: The user accepted the delivery at HIL 3 on `codereview_5` (`APPROVED WITH RESERVATIONS`). The feature is complete.
- Accepted open items: `codereview_5/CR-01` (DEC-06 earliest reset, per DEC-09). Manual items not run: MA-05 (exhausted account visual), MA-06 150% scaling and ≥ 10 rows, the T05 Windows 10 title-bar fallback, and the T06 horizontal scrollbar and Settings `Cadence & Rate Limits` tab.
- Not authorized: commit, push, or publication (DEC-04). No ADR candidate.
- Provenance: user answers "Accept (Recommended)" and "Continue in this session (Recommended)".
