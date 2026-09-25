# Load the snapshot

Protocol for the SDD skill that starts or resumes work on a feature with `context-snapshot.md`. Format, header, and tiers are in `.agents/skills/sdd-snapshot/SKILL.md`.

1. Locate the snapshot as the calling skill says. Read the header and load map. If the file is under 8 KiB, read it whole in one call; otherwise read up to the end of the next step brief and use `Grep` for entries.
2. Validate the header:
   - `covers_through` against the stage source (manifest `State`, report folder `done/`, or map). If the source shows more progress, the snapshot is behind: keep entries, but treat the next step brief as stale and reselect.
   - `git_head` against `git rev-parse HEAD`, and `worktree` against `git status --porcelain`. When files changed since the snapshot, entries whose triggers or `src` match those paths are suspect: verify them before relying on them.
   - `superseded` means follow `superseded_by`. `closed` means the stage it describes is finished; report and do not resume from it.
   - An unparseable header makes the whole file a list of hints: re-derive state from the stage source and verify any entry before use.
3. **Independent stages.** A session running `sdd-review-code` loads only the header, next step brief, `Open threads`, and `on-run` entries (environment and command facts). It skips `Decisions`, `Code map`, and other `Learnings`, which carry the author's framing, and derives what it judges from the sources. If this session itself authored the code under review, stop and apply the independence rule of the session pause (`.agents/skills/sdd-orchestrate-tasks/references/session-continuity.md`).
4. Load the `now` tier. For open threads waiting on the user, ask before starting work those threads block.
5. After choosing the unit, load the `on-select` entries that match. Keep the `on-edit` and `on-run` triggers in mind and load those entries when the work reaches them.
6. Follow a `src:` pointer only when the gist leaves a question the work needs answered, reading only the cited section.

**Complete when:** the header was validated with suspect entries named, the `now` tier is loaded, and the triggers of the other tiers are in view; or the snapshot was downgraded to hints with the reason reported.
