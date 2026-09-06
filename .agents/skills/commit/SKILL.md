---
name: commit
description: 'Create commits using conventional commits (title + bullet description), handling submodules with pending changes first and then the main repository. Use when the user asks to commit, save changes in git, generate a commit message, or use /commit. Push to the remote only when the `push`/`--push` argument is supplied.'
argument-hint: '[--push|--send] [optional context about what was done]'
disable-model-invocation: true
---

# Commit (submodules first)

Commit pending changes: each dirty submodule first, then the main repository.

Enable **push mode** whenever the invoking message contains `push`, `--push`, `send`, or `--send`, whether as a slash-command argument (`/commit --push`) or free text. Text after `/commit` is context attached to the message, so read the user's own message to decide. In push mode, send to the remote after committing (step 7). Without these words, stop after local commits.

## Inviolable rules

- Network access is allowed only in **push mode**, and only with `git push`, `git fetch`, and `git pull --rebase` (never `git merge` or pull with merge). Outside push mode, use no network.
- **NEVER** use `--no-verify` or `--no-gpg-sign`. If a hook fails, investigate and report; do not bypass it.
- Prefer a new commit to `--amend`.
- Do not create a branch, check out, or alter working-tree state beyond the agreed `git add`.

## Procedure

### 1. Inspect state

```bash
git submodule status
git status --porcelain
git symbolic-ref -q --short HEAD   # empty/error = detached HEAD
```

Without `.gitmodules`/submodules, skip directly to step 4 (main repository).

### 2. For each submodule with pending changes

Detect dirtiness in `<sub>` with `git -C <sub> status --porcelain`. Empty = skip.

For each dirty submodule, execute steps 3 and 4 **inside it** (`git -C <sub> ...`) before touching the main repository.

### 3. Decide what enters the stage

Compare `git status --porcelain` (column 1 = staged, column 2 = working tree):

| Situation | Action |
|---|---|
| Nothing staged | Run `git add -A` directly, without asking |
| Everything already staged | Proceed, without asking |
| The only unstaged item is the gitlink of a recently committed submodule (everything else staged) | Run `git add <submodule>` directly, without asking |
| Partially staged | Ask (see below) |

For a partial state, use the question tool (`AskUserQuestion`) showing what is staged and what is not, with options:

- **Yes** - run `git add -A` and commit everything
- **No** - commit only what is already staged
- **Abort** - stop without committing so the user can restage

### 4. Generate the message and commit

**Use the current session context as the primary source.** If pending changes came from this conversation, you already know *why* - the reported bug, decision, cited ticket, and rejected alternative. This is more valuable than the diff: write the message from that context and use the diff only to check coverage. If the diff contains changes **not** from this session, handle them from the diff normally.

Before writing, read what will be committed and the repository style:

```bash
git diff --staged --stat
git diff --staged
git log -8 --format="%s%n%b%n---"
```

Message format (GitHub / conventional commits):

```
type(scope): imperative, lowercase summary without a period

- Relevant change 1
- Relevant change 2
- Relevant change 3
```

- Title <= 72 characters. Types: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `build`, `ci`, `style`.
- **Language and scope follow the repository's `git log`**; a submodule may differ from the main repository.
- Bullets describe *what changed and why*, not file by file. A trivial commit may have only the title.
- No `Co-Authored-By` trailer.

Commit without prior confirmation. In PowerShell, use a literal here-string (the closing `'@` must be in column 0):

```powershell
git commit -m @'
type(scope): summary

- Bullet 1
- Bullet 2
'@
```

In Bash, use the equivalent heredoc (`git commit -F - <<'EOF'`).

### 5. Main repository

After submodules, return to the root and repeat steps 3 and 4. The submodule commit leaves the gitlink modified in the main repository, but it appears **unstaged** (second column of `git status --porcelain`), even if everything else was already staged. If the gitlink is the only change outside the stage, run `git add <submodule>` directly and proceed. If the **only** main-repository change is the gitlink, use `chore(lib): update submodule reference <name>`.

### 6. Push (push mode only)

Send **submodules first, then the main repository**; the main gitlink only makes sense remotely after the submodule commits are there.

For each repository with commits ahead of the remote:

```bash
git -C <repo> push
```

- **No upstream** (`no upstream branch`): `git -C <repo> push -u origin <current branch>`.
- **Rejected as non-fast-forward**: rebase and retry.

  ```bash
  git -C <repo> pull --rebase
  git -C <repo> push
  ```

- **Rebase conflict**: resolve file by file while preserving both intentions, `git add` resolved files, and run `git rebase --continue` until the queue is empty; then push. If the correct resolution is ambiguous, run `git -C <repo> rebase --abort`, leave the repository as it was, and report the conflict for the user to decide; never use `--force`, `--skip`, or `-X ours/theirs`.
- **Push rejected for another reason** (permission, server hook, protected branch): report the output and stop without bypassing it.

### 7. Report

One line per created commit: `<repo>: <short hash> <title>`. Then the destination of each push (`<repo> -> <remote>/<branch>`) or, outside push mode, a line saying that nothing was sent to the remote.

## Edge cases

- **Nothing to commit anywhere**: report and finish; do not force an empty commit. In push mode, still perform step 6 if local commits are ahead of the remote.
- **Pre-commit hook failed**: report the output; do not bypass it. If the hook reformatted files, restage and retry once.
- **Merge/rebase in progress** (`MERGE_HEAD`/`rebase-merge` present): stop and notify; do not commit over it.
- **Detached HEAD** (submodule or main - `git -C <repo> symbolic-ref -q --short HEAD` empty): **stop before committing**. Explain that the commit would be orphaned and leave the decision to the user. Proceed only if the user provides the target branch; then execute:

  ```bash
  git -C <repo> stash push -u -m "commit-skill"
  git -C <repo> checkout <branch>
  git -C <repo> stash pop
  ```

  If `stash pop` conflicts, stop and report; do not resolve or commit. Without a branch, finish without committing anything in that repository.
- **Heterogeneous changes** (several distinct features in one diff): make separate commits by topic using `git add` by path, instead of a generic commit.
