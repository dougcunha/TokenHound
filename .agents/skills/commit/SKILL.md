---
name: commit
description: 'Create conventional commits with a title and bullet-point description, handling submodules with pending changes before the main repository, and offer to remove from Git the SDD artifacts (`tasks/prd-*/`) of completed features, moving their ADRs to `docs/adr/` first. Use when the user asks to commit, save changes to Git, generate a commit message, or use /commit. Push to a remote only when the invocation includes `push`, `--push`, `send`, or `--send`.'
---

# Commit (submodules first)

Commit pending changes in dirty submodules first, then commit the main repository. In repositories with SDD artifacts, also offer to remove the artifacts of completed features, in a commit of its own.

Invocation: `/commit [--push|--send] [optional context about the changes]`.

Enable **push mode** whenever the message that invoked the skill contains `push`, `--push`, `send`, or `--send`. This applies to slash-command arguments such as `/commit --push` and free-form text such as "commit and push" or "you can send it." Slash-command arguments are not substituted into a variable automatically. Treat the text after `/commit` as context attached to the user's message, and inspect that message directly to decide whether push mode is active. In push mode, push after committing as described in step 8. Without any of these words, stop after creating local commits.

## Non-negotiable rules

- Use the network only in **push mode**, and only through `git push`, `git fetch`, and `git pull --rebase`. Never use `git merge` or a pull that creates a merge.
- **NEVER** use `--no-verify` or `--no-gpg-sign`. If a hook fails, investigate and report the failure instead of bypassing it.
- Prefer a new commit over `--amend`.
- Do not create branches, check out another branch, or alter the working tree beyond the agreed `git add` operation and the removal of the SDD artifacts the user chooses in step 2.

## Procedure

### 1. Inspect repository state

```bash
git submodule status
git status --porcelain
git symbolic-ref -q --short HEAD   # empty/error means detached HEAD
```

If there is no `.gitmodules` file or no submodule, step 3 does not apply: after step 2, continue with step 6 for the main repository.

### 2. Completed SDD artifacts

Run this once, before any commit, over every repository with `tasks/prd-*/` folders: the main repository and each submodule (`git -C <sub>`). Skip it when none of them has such folders.

SDD artifacts (PRD, TechSpec, `tasks.md`, tasks, handoffs, reviews, `checkpoint.json`, `workflow.md`, `context-snapshot.md`) are the feature's contract while it is in progress, and versioning them during that time is expected. Once the feature is completed, the source of truth becomes the code, the tests, and durable documentation; the artifacts age with every later change, and keeping them in the repository invites agents to treat them as the current specification or to spend effort keeping them in sync. That is why cleanup happens here, at commit time, and not while another feature is being executed.

Classify each `tasks/prd-[slug]/` by the first row that decides:

| Evidence | Class |
|---|---|
| `checkpoint.json` with `status` or `phase` equal to `completed` | completed |
| `context-snapshot.md` with `status: closed` and `stage: acceptance` | completed |
| No `checkpoint.json`: no `task_*.md` at the folder root, tasks in `done/`, and `codereview_[highest num]/codereview.md` with status `APPROVED`, or `APPROVED WITH RESERVATIONS` with the user's decision on the reservations recorded, and no correction tasks at that review's root | completed |
| `checkpoint.json` in another phase, a pending task at the root, latest review `REJECTED`, or a pending correction | in progress |
| Any other combination (e.g. only `prd.md`, a planned slice not started yet, a folder with only a snapshot, reservations without a recorded decision) | undetermined |

A feature in progress is never offered. For each completed or undetermined one, gather before asking what removal would take away without a record elsewhere:

- the folder's ADRs: candidates in task and correction handoffs (`### ADR candidates` with content other than `None`) and loose ADR files in the folder, with ID and title; they are not lost, because step 7 moves them to `docs/adr/` before the removal, and the question states the number each one will get;
- accepted reservations and open items recorded in the latest review or in `workflow.md`;
- untracked files in the folder (`git status --porcelain -- tasks/prd-[slug]/`), which removal deletes for good;
- tracked files outside the folder that cite it (`git grep -l "prd-[slug]/" -- ":!tasks/prd-[slug]/"`), such as a README or docs, which would be left with broken links; recommend keeping the feature or first moving the cited content into the documentation;
- `checkpoint.json` with `mode: auto`: acceptance was automatic and no human saw the delivery; point to the decision log (`decision_log`) for review before removal.

Ask a single question for all repositories, through the available question tool (`AskUserQuestion` with `multiSelect`) or, without it, as text with the same options. One option per feature: the repository when it is not the main one, the slug, the class with the evidence that decided it and, in the description, the items gathered above. An undetermined feature is never marked as recommended. With more features than the tool can hold, ask as text listing all of them. No selection, "none", or silence keeps everything. Keep the choice for step 7; the question happens now so the rest of the skill runs without another interruption.

### 3. Process every submodule with pending changes

Detect pending changes in `<sub>` with `git -C <sub> status --porcelain`. Skip the submodule when the output is empty.

For every dirty submodule, complete steps 4 and 5 **inside it** by using `git -C <sub> ...` before touching the main repository.

### 4. Decide what to stage

Compare the two columns from `git status --porcelain`: column 1 is the index, and column 2 is the working tree.

| Situation | Action |
|---|---|
| Nothing is staged | Run `git add -A` without asking |
| Everything is already staged | Continue without asking |
| The only unstaged item is the gitlink of a submodule that was just committed, while everything else is staged | Run `git add <submodule>` without asking |
| The changes are partially staged | Ask the user as described below |

For partially staged changes, use the question tool (`AskUserQuestion`). Show what is staged and what remains unstaged, then offer these choices:

- **Yes**: run `git add -A` and commit everything
- **No**: commit only what is already staged
- **Abort**: stop without committing so the user can redo the staging

### 5. Write the message and commit

**Use the current session context as the primary source.** If the pending changes came from work completed in this conversation, the reason is already known: the reported bug, the chosen decision, the referenced ticket, or the rejected alternative. That context is more valuable than the diff. Use the diff to confirm coverage, ensuring that no relevant change is missing and no unrelated change is included. When the diff contains changes that did not originate in this session, interpret them from the diff normally.

Before writing the message, inspect what will be committed and the repository's existing style:

```bash
git diff --staged --stat
git diff --staged
git log -8 --format="%s%n%b%n---"
```

Use this GitHub-style conventional commit format:

```
type(scope): imperative lowercase summary without a period

- Relevant change 1
- Relevant change 2
- Relevant change 3
```

- Keep the title at 72 characters or fewer. Allowed types are `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `build`, `ci`, and `style`.
- Match the language and scope conventions in the `git log` of the repository being committed. Each repository may use a different convention, and a submodule may differ from the main repository.
- Describe what changed and why instead of listing files. A trivial commit, such as a submodule reference bump, may contain only the title.
- Do not add a `Co-Authored-By` trailer.

Commit without requesting confirmation. In PowerShell, use a literal here-string. The closing `'@` must begin in column 0:

```powershell
git commit -m @'
type(scope): summary

- Bullet 1
- Bullet 2
'@
```

In Bash, use the equivalent heredoc form: `git commit -F - <<'EOF'`.

### 6. Commit the main repository

After committing the submodules, return to the repository root and repeat steps 4 and 5. A submodule commit modifies its gitlink in the main repository, but the gitlink appears as **unstaged** in the second column of `git status --porcelain`, even if everything else was already staged. In that case, apply the rule from step 4: if the gitlink is the only unstaged change, run `git add <submodule>` without asking and continue with the commit. If the gitlink is the **only** change in the main repository, use a message such as `chore(lib): update <name> submodule reference`.

### 7. Remove the chosen SDD artifacts

Only when step 2 had a chosen feature, after the normal commit of the repository that holds it. In a submodule, remove right after its commit, before returning to the main repository, so the gitlink already includes the removal. The removal goes into a commit of its own to keep the feature commit clean and leave the artifacts recoverable from history starting at the previous commit.

For each chosen feature, first move the ADRs, then remove the folder. An architectural decision stays valid after the feature completes, and the handoff that holds it leaves with the folder; moving it to `docs/adr/` is what keeps it as durable documentation.

1. **Move ADRs.** For each ADR listed in step 2, except a candidate that `workflow.md` or the review records as rejected:
   - Follow the repository's `docs/adr/` convention (numbering, title, sections) by reading an existing ADR; without the folder or any ADR, create it with sequential `NNNN-slug.md` numbering from the highest existing number, a short title, and one paragraph with context, decision, and reason.
   - Carry the candidate's content over: alternatives and consequences only when they add something; evidence as a reference to the code, not to the artifact being removed.
   - End with an origin line: `Origin: prd-<slug>, <TXX-ADR-NN>, artifacts up to <short-hash>`.
   - A decision that already has an ADR in `docs/adr/` (same decision or same origin) is not duplicated: cite the existing one in the report.
   - `git -C <repo> add docs/adr/`.
2. **Remove the folder.**

   ```bash
   git -C <repo> rm -r -q tasks/prd-<slug>/
   ```

- If `git rm` refuses because of a local change (the user committed only part of the stage in step 4), skip that feature and report it; never use `-f`.
- Untracked files left in the folder were announced in the question: delete the remaining folder.
- A folder with no tracked file: just delete it from disk.

Commit with title and language matching the `git log`, and one bullet per feature citing the short hash of the last commit that still contained the artifacts (the `HEAD` before this commit), for recovery with `git checkout <hash> -- tasks/prd-<slug>/`:

```
chore(sdd): remove artifacts of completed features

- prd-<slug>: artifacts up to <short-hash>
- docs/adr/NNNN-slug.md: ADR <TXX-ADR-NN> from prd-<slug>
```

The ADRs go into the same commit as the removal so the change is a move: the decision is never out of the repository between two commits.

Never offer or remove `tasks/triage-log.jsonl`: it is the triage calibration log, not a feature artifact.

### 8. Push (push mode only)

Push **submodules first, then the main repository**. The main repository's gitlink is only valid on the remote after the corresponding submodule commits are available there.

For every repository with local commits ahead of its remote:

```bash
git -C <repo> push
```

- **No upstream branch** (`no upstream branch`): run `git -C <repo> push -u origin <current-branch>`.
- **Rejected as non-fast-forward**: integrate with rebase and retry.

  ```bash
  git -C <repo> pull --rebase
  git -C <repo> push
  ```

- **Conflict during rebase**: resolve each file while preserving both intentions, the remote commit and the local commit. Stage resolved files and run `git rebase --continue` until the queue is empty, then push. If the correct resolution is ambiguous, such as incompatible changes on the same line, a binary file, or a migration/lockfile conflict, run `git -C <repo> rebase --abort`, leave the repository as it was, and report the conflict so the user can decide. Never use `--force`, `--skip`, `-X ours`, or `-X theirs`.
- **Push rejected for another reason**, such as permissions, a server-side hook, or a protected branch: report the output and stop without bypassing the restriction.

### 9. Report the result

Write one line for every created commit: `<repo>: <short-hash> <title>`, including the SDD artifact removal commit; features offered and kept need not be listed. Then list each push destination as `<repo> -> <remote>/<branch>`. Outside push mode, state that nothing was sent to a remote.

## Edge cases

- **Nothing to commit anywhere**: report it and stop. Do not force an empty commit. The artifact removal chosen in step 2 still happens and may be the only commit. In push mode, still complete step 8 when local commits are ahead of the remote.
- **Pre-commit hook failed**: report the output and do not bypass the hook. If the hook reformatted files, restage them and retry the commit once.
- **Merge or rebase in progress**, indicated by `MERGE_HEAD` or `rebase-merge`: stop and report it. Do not commit over an active operation.
- **Detached HEAD** in a submodule or the main repository, indicated by an empty result from `git -C <repo> symbolic-ref -q --short HEAD`: **stop before committing**. Explain that the commit would be orphaned and let the user choose. Continue only after the user names the destination branch, then run:

  ```bash
  git -C <repo> stash push -u -m "commit-skill"
  git -C <repo> checkout <branch>
  git -C <repo> stash pop
  ```

  If `stash pop` produces a conflict, stop and report it. Do not resolve or commit the conflict. Without a user-provided branch, exit without committing in that repository.
- **Unrelated groups of changes**, such as several distinct features in one diff: create separate commits by subject and stage each group by path instead of creating one generic commit.
