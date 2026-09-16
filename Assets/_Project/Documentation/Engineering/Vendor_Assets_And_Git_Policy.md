# Vendor assets and Git policy

Large Asset Store / third-party folders stay **on disk in Unity** but are **not pushed to GitHub** unless we deliberately ship a slice under `Assets/_Project/`. This keeps pushes small and avoids HTTP timeouts on multi‑GB packs.

Authority: same list as `AGENTS.md` and `.cursor/rules/dark-matter-genesis-core.mdc` (agents must not `git add` ignored vendor trees).

---

## Default rule

| Layer | In git? | On disk? |
|--------|---------|----------|
| Full vendor import (entire pack folder) | **No** (`.gitignore`) | **Yes** (each dev installs locally) |
| Game-owned slice (`Assets/_Project/…`) | **Yes** (meshes, mats, prefabs, textures we ship) | **Yes** |
| `.unitypackage` installers | **No** (ignored) | Optional local backup only |
| Install notes | **Yes** (`Vendor_Install_Manifest.md`) | — |

Unity references assets by **GUID** (`.meta`). If a tracked prefab under `_Project` still points at an ignored vendor path, clones need that pack installed at the **same path** or you must **copy dependencies** into `_Project` and retarget references.

---

## Workflow: add a new vendor package

1. Import the pack locally (Asset Store or `.unitypackage`).
2. Add the root folder to **`.gitignore`** (and an entry in `Vendor_Install_Manifest.md`) before anyone runs `git add -A`.
3. Use assets in scenes/prefabs. For anything that must survive without the full pack on another machine:
   - **Move or duplicate** used assets + dependencies into `Assets/_Project/` (Art, Prefabs, Materials, etc.), or
   - Document **required install** in the manifest (team installs identical pack version).
4. Never commit single files from an ignored tree with `git add -f` unless leadership explicitly wants that asset in history.
5. Respect **license** (Asset Store redistribution rules).

---

## Workflow: “used slice” only

There is no automatic “export only referenced files” in Unity. Manual checklist:

1. Identify prefabs/scenes under `_Project` that reference the vendor folder.
2. For each dependency chain, copy **mesh, material, texture, shader, animator, audio** (whatever the reference needs) into `_Project`.
3. Fix broken references in Unity; save so `.meta` GUIDs on `_Project` copies are stable.
4. Prefer **_Project** paths in new work (`dm-naming-no-invector` for our identifiers).
5. Run Play Mode / build once to catch pink materials or missing meshes.

---

## Already tracked in git (legacy)

Some vendor paths were committed before this policy. **`.gitignore` does not remove them from history or from the index.**

| Path | Notes |
|------|--------|
| `Assets/PlanetPack02/` | Tracked (~128 files). Ignored for **new** untracked files only until removed from index. |
| `Assets/OlegWER/` | Tracked (~31 files). Same as above. |

To stop pushing a folder that is **already tracked** (team agreement required):

```powershell
cd "A:\Dark Matter Genesis"
git rm -r --cached "Assets/PlanetPack02"
git rm -r --cached "Assets/OlegWER"
# Commit with manifest update; each dev keeps local folder; clone runs install from manifest.
```

That does **not** shrink old commits; it only stops future diffs. History rewrite is a separate, explicit decision.

---

## Push size and LFS

- Prefer **ignore + local install** over committing multi‑GB texture sets.
- Project art under `_Project/Art/` that we own (e.g. Io terrain sets) **is** intended for git; expect large pushes and plan network time or split commits.
- Git LFS is optional for large **project** binaries; it is not a substitute for vendor ignore policy.

---

## Agents and hooks

- Cloud agents: see `Cloud_Agent_Unity_Safeguards.md` — no silent checkout; no `git add -A`.
- Optional: `Tools/install-unity-git-hooks.ps1` runs `DmUnityGitSafeguard.ps1` before commit/push.

---

## Related files

- Install list: `Vendor_Install_Manifest.md` (same folder)
- Ignore list: repository root `.gitignore` (section “Vendor Asset Store packs”)
