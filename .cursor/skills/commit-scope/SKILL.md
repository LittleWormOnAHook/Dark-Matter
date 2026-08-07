---
name: commit-scope
description: >
  Apply when staging files, performing a version-control commit, finishing a task, preparing a PR, or when the user
  asks to commit. Commits must contain only changes for the current fix or implementation; do not
  bundle documentation, unrelated files, or drive-by edits unless the task explicitly included them.
---

# Commit scope

## Default rule

- Stage with **explicit paths** or **interactive chunks** (`git add -p`, per-file `git add`).
- Do **not** use blanket `git add .` or stage-all-then-commit (`-a`) unless the user clearly wants every local change in one commit.

## In scope for a single commit

- Files changed **for the stated task** only.
- Tests or fixtures that **directly validate** that same change, when they belong with the implementation.

## Out of scope by default (do not mix into the same commit)

- New or edited `*.md` and other documentation **unless** the task was explicitly documentation work.
- Editor or tooling config, formatting-only sweeps, unrelated refactors.
- Preference or meta logs (e.g. `learnings.md`-style files) unless the task asked for them.
- Generated or accidental artifacts.
- **Temporary debug / profiler / timing / measurement scaffolding** (probes, capture menus, one-off Editor helpers, investigative `Debug.Log`s, production hooks that only exist for evidence). **Do not stage or commit these unless the user directly tells you to.** A bare “commit” / “commit the PR” does **not** count as permission — exclude them and strip or leave unstaged.

## Mixed local changes

- **Group by Intent:** Always group commits by their specific type (e.g., all files for a single `feat`, `fix`, or `refactor`).
- **No Unrelated Bundles:** Never commit multiple unrelated features, fixes, or tasks together. If a task accidentally touched an unrelated file, leave it unstaged or split it into a separate commit.
- Tell the user what was excluded or split and why.

## Commit message

- One **logical** change per commit; the message should match that scope, not a list of unrelated topics.
- **Conventional Commits:** Use the `type: description` format (e.g., `feat: add object pooling`, `fix: resolve null ref in spawner`, `refactor: clean up input logic`). Common types: `feat`, `fix`, `refactor`, `chore`, `docs`, `perf`.

## Verification Requirement
- Never generate a commit until **Step 4 (Test)** and **Step 5 (Read Logs)** of the `task-workflow` have been completed and verified. A commit is the final signature of a successful cycle.
