# Baselines — prd-15-settings-window-split

Taken from `git_base` `90d6748` with `git show`, so a rerun after later tasks still yields the base. At the base, `src/TokenHound.App/UI/Styles/` holds only `DialogResources.xaml` and `src/TokenHound.App/UI/Controls/Settings/` does not exist.

Run from the repository root in Git Bash.

## Generate (T01)

```bash
B=tasks/prd-15-settings-window-split/baseline
{ git show 90d6748:src/TokenHound.App/UI/Styles/DialogResources.xaml; git show 90d6748:src/TokenHound.App/UI/Windows/SettingsWindow.xaml; } | rg -o 'x:Key="[^"]+"' | sort -u > $B/keys.txt
git show 90d6748:src/TokenHound.App/UI/Windows/SettingsWindow.xaml | rg -o 'AutomationProperties\.(Name|HelpText)="[^"]+"' | sort > $B/automation-names.txt
```

Expected: `keys.txt` 66 lines (60 dialog + 6 Settings), `automation-names.txt` 52 lines (41 Name + 11 HelpText), `line-counts.txt` 1051 and 561.

## Compare the working tree (T02..T06)

QA-03 key set. Shared dictionaries are merged into several parts (DEC-05), so compare the unique set:

```bash
B=tasks/prd-15-settings-window-split/baseline
cat src/TokenHound.App/UI/Styles/*.xaml src/TokenHound.App/UI/Windows/SettingsWindow.xaml $(ls src/TokenHound.App/UI/Controls/Settings/*.xaml 2>/dev/null) | rg -o 'x:Key="[^"]+"' | sort -u | diff $B/keys.txt - && echo "QA-03 identical"
```

QA-04 automation names. `SettingsResources.xaml` is included because templates may carry names:

```bash
B=tasks/prd-15-settings-window-split/baseline
cat src/TokenHound.App/UI/Windows/SettingsWindow.xaml $(ls src/TokenHound.App/UI/Styles/SettingsResources.xaml src/TokenHound.App/UI/Controls/Settings/*.xaml 2>/dev/null) | rg -o 'AutomationProperties\.(Name|HelpText)="[^"]+"' | sort | diff $B/automation-names.txt - && echo "QA-04 identical"
```

QA-01/QA-02 line counts, QA-05, QA-07:

```bash
wc -l src/TokenHound.App/UI/Windows/SettingsWindow.xaml src/TokenHound.App/UI/Styles/*.xaml $(ls src/TokenHound.App/UI/Controls/Settings/*.xaml 2>/dev/null)
rg -c DynamicResource src/TokenHound.App || echo "QA-05 none"
git diff --quiet 90d6748 -- src/TokenHound.App/App.xaml && echo "QA-07 unchanged"
```
