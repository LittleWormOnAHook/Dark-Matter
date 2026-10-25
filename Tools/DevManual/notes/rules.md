# Rules and Gotchas

- **Never edit:** FORGE3D, packages, NavMesh, BOTD/Gaia originals. Agents also don't edit scenes or player/enemy prefabs unless asked.
- **Agents:** no Play, no scene/prefab saves, no Unity or MCP-bridge restarts. Refresh only after confirming the editor isn't in Play. The console must end at 0/0 (the 5 `FORGE3D/Burnout` shader errors are known).
- **Backups:** `_AgentBackups/<relpath>.pre<date><suffix>` before every edit.
- **CHANGELOG:** `rc_deploy/savepoint_20261011_combat_slice0/files/CHANGELOG_20261021.txt`. Every entry ends with a **`Docs:`** line naming the Dev Manual page(s) updated (or `Docs: none`).
- **Git:** explicit paths only, through `Tools/DmUnityGitSafeguard.ps1`. No `git add -A`, no force push.
- **Bulk menus** (many materials or prefabs) need Anthony's OK.
- **New settings** go into Genesis Studio, with presets where sensible.
- **Shader defaults** don't change materials that already serialise the value.
