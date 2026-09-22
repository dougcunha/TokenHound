---
name: commit
description: 'Create conventional commits with a title and bullet-point description, handling submodules with pending changes before the main repository. Use when the user asks to commit, save changes to Git, generate a commit message, or use /commit. Push to a remote only when the invocation includes `push`, `--push`, `send`, or `--send`.'
---

# Commit (submodules first)

Commit pending changes in dirty submodules first, then commit the main repository.

Invocation: `/commit [--push|--send] [optional context about the changes]`.

Enable **push mode** whenever the message that invoked the skill contains `push`, `--push`, `send`, or `--send`. This applies to slash-command arguments such as `/commit --push` and free-form text such as "commit and push" or "you can send it." Slash-command arguments are not substituted into a variable automatically. Treat the text after `/commit` as context attached to the user's message, and inspect that message directly to decide whether push mode is active. In push mode, push after committing as described in step 6. Without any of these words, stop after creating local commits.

## Non-negotiable rules

- Use the network only in **push mode**, and only through `git push`, `git fetch`, and `git pull --rebase`. Never use `git merge` or a pull that creates a merge.
- **NEVER** use `--no-verify` or `--no-gpg-sign`. If a hook fails, investigate and report the failure instead of bypassing it.
- Prefer a new commit over `--amend`.
- Do not create branches, check out another branch, or alter the working tree beyond the agreed `git add` operation.

## Procedure

### 1. Inspect repository state

```bash
git submodule status
git status --porcelain
git symbolic-ref -q --short HEAD   # empty/error means detached HEAD
```

If there is no `.gitmodules` file or no submodule, continue with step 5 for the main repository.

### 2. Process every submodule with pending changes

Detect pending changes in `<sub>` with `git -C <sub> status --porcelain`. Skip the submodule when the output is empty.

For every dirty submodule, complete steps 3 and 4 **inside it** by using `git -C <sub> ...` before touching the main repository.

### 3. Decide what to stage

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

### 4. Write the message and commit

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

### 5. Commit the main repository

After committing the submodules, return to the repository root and repeat steps 3 and 4. A submodule commit modifies its gitlink in the main repository, but the gitlink appears as **unstaged** in the second column of `git status --porcelain`, even if everything else was already staged. In that case, apply the rule from step 3: if the gitlink is the only unstaged change, run `git add <submodule>` without asking and continue with the commit. If the gitlink is the **only** change in the main repository, use a message such as `chore(lib): update <name> submodule reference`.

### 6. Push (push mode only)

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

### 7. Report the result

Write one line for every created commit: `<repo>: <short-hash> <title>`. Then list each push destination as `<repo> -> <remote>/<branch>`. Outside push mode, state that nothing was sent to a remote.

## Edge cases

- **Nothing to commit anywhere**: report it and stop. Do not force an empty commit. In push mode, still complete step 6 when local commits are ahead of the remote.
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
