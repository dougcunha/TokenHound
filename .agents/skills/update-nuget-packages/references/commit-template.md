# Commit messages

Read this reference when `_lib` and the main repository have separate changes. Generate the messages without running `git commit`.

Use one message per repository and keep only that repository's changed packages in the main block.

```text
chore(nuget): <title>

<summary of changes>

Updated packages

- <package>: <previous version> => <new version>

Validation

- <build command>: <result>
- <test command>: <result and count>
```

Include only packages actually changed in that repository. For a new package required by the MTP migration, use `new => <version>` and explain its purpose in the summary.
