## Reviewed notes (Combat)

- **Combat Studio** (separate window): `Tools/Dark Matter Genesis/Combat/Combat Studio` (`Assets/_Project/Editor/Combat/DMCombatStudioWindow.cs`).
- **Hit Marks / Blood FX**: profile `Resources/Combat/DM_EnemyHitMarkProfile.asset`; menus `Combat/Hit Marks/Blood FX - Build Android + Mech Variants` and `... - Recolor Variants From Profile`; `Combat/Build Laser Burn Mark Prefab`.
- **Carve**: settings `Resources/Carve/DMCarveSettings.asset`; menus `Tools/Dark Matter Genesis/Carve/Enable Read/Write For Auto-Carvable Tags` and `Carve/Rebuild Static Mesh Cache`.
- **Hit reactions**: `Combat/Build|Validate (Player) Hit Reaction Layers` (`Editor/Combat/DMHitReactionLayerBuilder.cs`).
- **Weapon placement**: `Combat/Weapon Placement/*` (bake window, apply profile, bake all enemy prefabs).
- **Audits** (Play only): `Combat/Enemy Obstacle Jitter|Rock Mesh|Thin Wall Endpoint Audit (Play)`; Audit Hub at `Tools/Dark Matter Genesis/Audit/Audit Hub`.
- Design roadmap: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` (v2.4). The engineering doc is `Assets/_Project/Features/Combat/Documentation/Dark_Matter_Combat_System.md`.

**Gotchas:** never edit FORGE3D (turrets are only *registered*, via `Buildings/Register ... turrets in Equipment`). Enemy/player prefabs are not saved by agents, and "Bake all enemy prefabs" touches many prefabs, so it needs Anthony's OK.
