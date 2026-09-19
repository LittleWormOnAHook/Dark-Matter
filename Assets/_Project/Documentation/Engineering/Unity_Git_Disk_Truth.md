# Unity + Git — Disk Is Truth

**Policy:** The live folder **`A:\Dark Matter Genesis`** is the single source of truth for Play and day-to-day editing. Git records snapshots of that disk when you **commit**. **Push does not revert Unity.** Work is lost when disk, Unity’s open scene, and git get out of sync—or when git **checkout/reset** replaces files without an explicit restore.

Authority: `.cursor/rules/unity-agent-workflow.mdc`, `cloud-agent-unity-safeguards.mdc`, `confirm-before-depot-restore.mdc`, `Tools/DmUnityGitSafeguard.ps1`.

Related: [Cloud_Agent_Unity_Safeguards.md](Cloud_Agent_Unity_Safeguards.md), [Vendor_Assets_And_Git_Policy.md](Vendor_Assets_And_Git_Policy.md).

---

## One live Unity root

| Use | Path |
|-----|------|
| **Unity Editor (always)** | `A:\Dark Matter Genesis` |
| **Playable scene (current)** | `Assets/_Project/Scenes/Dark Matter Genesis v1.6.4.unity` (use the latest numbered Genesis scene if renamed) |
| **Auto Refresh** | **Off** — after git or external file edits: **Ctrl+R** (Assets → Refresh) |
| **Git source of truth** | git (not Plastic CLI; Plastic check-in is manual in the UVCS window) |

Do **not** treat `.cursor/worktrees/*` as the live project unless Unity is deliberately opened on that worktree folder.

---

## Why work “disappears” (usually not the push)

| Cause | What happened |
|--------|----------------|
| **Partial commit** | Only staged files were committed; scene, prefabs, profiles, or agent scripts stayed `M` / `??` on disk and were never on GitHub. |
| **Unity Save over restored disk** | Agents fixed files on disk; Unity still had an **older scene in memory**. **Save** overwrote the restored `.unity` / prefabs. |
| **Checkout / reset / pull** on the live folder | Git replaced disk with another commit or branch. |
| **Agent git on live folder** | Cloud or local agent ran `checkout` / `reset` without merge (forbidden without explicit user OK). |
| **Wrong folder** | Edits in a **worktree** while Play uses **`A:\Dark Matter Genesis`**. |
| **Broken files on disk** | Merge/splice corruption (compile errors)—not a git revert; fix or restore that file. |
| **`external_changes_dirty` (MCP)** | Unity has not reimported disk yet. **Ctrl+R** syncs editor to disk; it does **not** checkout old git. |

---

## Disk-as-truth workflow

### Before you call a commit “saved”

1. **Scene vs disk**
   - **Disk updated by Cursor/agents:** Do not **Save** a stale open scene. **Ctrl+R** → if prompted, **Reload from disk**.
   - **You edited in Unity:** **Save** scene and prefabs **first**, then commit.

2. **Stage the whole change set** (same commit when they belong together):
   - Scripts under `Assets/_Project/`
   - Scene `.unity` + `.meta`
   - Prefabs, materials, terrain, ScriptableObjects, new assets + `.meta`
   - Do **not** ship script-only commits that leave matching scene/prefab wiring behind (see `unity-agent-workflow.mdc`).

3. **Inspect:** `git status` — critical paths should not still be unstaged if you intend them saved.

4. **Safeguard:** `.\Tools\DmUnityGitSafeguard.ps1` (use `-BeforePush` before push).

5. **Unity Console:** **Zero errors** before commit/push.

6. **Commit → push** on the feature branch (only when you asked for commit/push).

7. **After any git change on this PC:** Unity **Ctrl+R**; reopen the canonical Genesis scene if needed.

### After a good Unity day (local safety net)

```powershell
cd "A:\Dark Matter Genesis"
git branch -f backup/wip-pre-restore-20260907
# Or dated: git branch -f backup/wip-YYYYMMDD
```

That branch is a **pointer in git**, not a second disk copy—but it blocks silent checkouts from dropping unpushed commits.

Optional hooks: `.\Tools\install-unity-git-hooks.ps1`

---

## “Dirty” — two different things

| Signal | Meaning | Safe action |
|--------|---------|-------------|
| **Git dirty** (`git status` shows `M` / `??`) | Uncommitted work on disk | Commit a WIP snapshot on a branch, or leave uncommitted **on purpose**. Do **not** `reset --hard` to “clean” unless you explicitly want to discard work. |
| **Unity external dirty** (MCP / Auto Refresh off) | Disk changed; Asset Database not refreshed | **Ctrl+R** — imports **current disk** into Unity. |

A clean git tree is **not** required to Play Mode. It **is** required if GitHub should hold **everything** you care about on disk.

---

## On-disk backups (what exists vs what does not)

| Location | Full 1:1 project? | Notes |
|----------|-------------------|--------|
| **`A:\Dark Matter Genesis`** | **Live truth** | Unity must use this for normal work. |
| **GitHub / `git push`** | Snapshot of **committed** files only | Large ignored vendor files and omitted megabytes stay local. |
| **`backup/*` branches** | No (git refs only) | e.g. `backup/wip-pre-restore-20260907` |
| **`.cursor/worktrees/…`** | No | Other branches/commits; not a mirror of today’s live tip. |
| **`_agent_backups/`** | No | Occasional single-file `.bak` files, not full project. |
| **`Assets/_Recovery/`** | No | Crash/recovery scenes—forensic only; do not treat as canonical. |

For a **true disk clone**, add your own process (e.g. robocopy/zip to another drive). Exclude `Library/` for smaller copies; include it only if you need a full cached clone.

---

## Agent paste block (pre-commit / pre-push)

Copy into agent chats when saving work:

```text
Disk truth: A:\Dark Matter Genesis only. Unity scene: Dark Matter Genesis v1.6.4 (or current Genesis scene).

Before commit/push:
1. Unity: Save ONLY if editor edits are intentional; if agents restored disk, Ctrl+R and Reload scene from disk — do not Save stale scene over disk.
2. git status — stage scripts AND matching .unity, .prefab, .asset, .mat, terrain, new Assets/_Project files + .meta for this change set.
3. Do not git add -A; do not commit ignored vendor trees (see Vendor_Assets_And_Git_Policy.md).
4. Run Tools/DmUnityGitSafeguard.ps1 (-BeforePush before push).
5. Unity Console: zero errors.
6. No git checkout/reset/revert without explicit user confirmation in the current message.

After git on this machine: user Ctrl+R (Auto Refresh off). Push does not revert; partial commits and checkouts do.
```

---

## Human checklist (Anthony)

- [ ] Open scene is the canonical Genesis scene (not `_Recovery`).
- [ ] Decide: Save in Unity vs reload from disk after agent edits.
- [ ] `git status` — scene / Variant / profiles included if this push should capture them.
- [ ] `.\Tools\DmUnityGitSafeguard.ps1`
- [ ] Console 0 errors.
- [ ] Commit → push (when ready).
- [ ] **Ctrl+R** in Unity.
- [ ] Optional: `git branch -f backup/wip-…` on HEAD after a solid day.

---

## Large files and push timeouts

See **Vendor_Assets_And_Git_Policy.md**. Files over GitHub’s hard limit must stay out of history or use LFS by explicit decision. Partial pushes with huge packs may HTTP-timeout—fix by omitting megabytes from the commit, not by resetting Unity.
