---
name: conflict-resolution
description: >
  Systematic process for resolving merge conflicts while maintaining code integrity.
  Triggers when git status reports unmerged paths, when conflict markers are detected,
  or when a rebase/merge operation is in progress.
---

# Conflict Resolution Process

This skill provides a systematic process for handling merge conflicts to maintain code integrity. Use it whenever `git status` reports unmerged paths or when conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`) are detected in the codebase.

## 1. Understand the Conflict

Before attempting to fix anything, identify what is conflicting and why.
- Use `git status` to get a list of all unmerged files.
- Use `git diff` or `cat` (on the specific lines) to see the conflicting blocks.

## 2. Analyze the Intent

Review both versions of the conflicting code:
- **Ours (Current Branch):** What was the goal of our local change?
- **Theirs (Incoming Change):** What was the goal of the change we are pulling/rebasing?
- **Determine the Outcome:** Can we keep one side entirely, or do we need to combine the logic?

## 3. Resolve the Conflict

Choose the appropriate strategy for each file:

### Keep Theirs
Use this when the incoming changes are strictly better or if our local changes were redundant/wrong.
```bash
git checkout --theirs <file_path>
```

### Keep Ours
Use this when our local changes must take precedence and the incoming ones are not applicable.
```bash
git checkout --ours <file_path>
```

### Manual Merge
The most common case. Manually edit the file to:
1. Combine the logic from both versions where necessary.
2. Remove all conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`).
3. Ensure the final code is clean, idiomatic, and correctly formatted.

## 4. Verify the Resolution

Resolving a conflict is not complete until the code is proven to be functional.
- **Syntax Check:** Run the project's compiler or linter (e.g., `tsc`, `npm run lint`, `dotnet build`).
- **Run Tests:** Execute relevant unit tests to ensure no logic was broken during the merge.
- **Visual Inspection:** Double-check that no stray conflict markers remain in the file.

## 5. Finalize the Operation

Once all conflicts in a file are resolved and verified:
1. Stage the file: `git add <file_path>`
2. Check `git status` to ensure no unmerged paths remain.
3. Complete the git operation:
   - For a merge: `git commit`
   - For a rebase: `git rebase --continue`

## Best Practices

- **Pull Frequently:** Minimize the size and complexity of conflicts by keeping your branch up to date with `main`.
- **Don't Blindly Choose:** Never use `--ours` or `--theirs` without first understanding what you are throwing away.
- **Verify Semantics:** A "clean merge" (no syntax errors) doesn't always mean a "correct merge" (logic is still sound). Always run tests.
- **Atomic Commits:** If resolving a massive conflict, consider committing the resolution alone before moving on to other work.

## Resolution Checklist

- [ ] All conflicting files identified via `git status`.
- [ ] Each conflict block analyzed for intent.
- [ ] Resolution strategy chosen (Ours / Theirs / Manual Merge).
- [ ] Manual merges are clean and markers are removed.
- [ ] Files staged with `git add`.
- [ ] Build/Compile is successful.
- [ ] Relevant tests pass.
- [ ] Git operation (merge/rebase) is finalized.
