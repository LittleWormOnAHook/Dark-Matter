# AI 3D Asset Pipeline — Stage 1 Reconnaissance & Architecture

**Status:** Inspection only. No pipeline built. No models downloaded. No gameplay code changed.  
**Date:** 2026-09-20  
**Hardware target:** NVIDIA RTX 5070 12 GB (Blackwell, compute 12.0)  
**Product target:** Dark Matter: Genesis — Unity 6 HDRP, FBX + PBR + LOD + collision + prefab

This brief is the Unity-side Stage 1 record. A separate factory already exists: **Dark Matter Asset Engine (DMAE V5)**. Stage 2+ should extend that factory and add a thin Unity ingest layer — not invent a second generator.

---

## 1. Measured machine (this session)

| Item | Observed | Notes |
|------|----------|--------|
| OS | Windows 10 Pro 64-bit `10.0.19045` | Matches user_info |
| CPU | Intel Core i7-11700K, 8C / 16T, 3.60 GHz | Fine for Blender + Unity; not the bottleneck |
| System RAM | **31.9 GB** | Only **1.1–4.8 GB free** during inspect. Unity + browser already pressure RAM |
| GPU | **NVIDIA GeForce RTX 5070** | Confirmed via WMI + `nvidia-smi` |
| Dedicated VRAM | **12227 MiB (~12 GB)** | Driver `610.47`, compute capability **12.0** (Blackwell `sm_120`) |
| VRAM free at inspect | **3075 MiB** | ~9 GB already held (Unity / desktop). Local 3D gen cannot share this card with Play Mode |
| CUDA toolkit (`nvcc`) | **Not installed** | Driver CUDA works; no `C:\Program Files\NVIDIA GPU Computing Toolkit` |
| Git | `2.54.0.windows.1` | Present |
| Node | `v24.16.0` | Present |
| `uv` | `0.11.21` | Present (Blender MCP launcher) |
| Conda | **Not installed** | |
| Default Python | **3.14.6** (`C:\Python314\python.exe`) | Also 3.13.14 and a Store 3.12 |
| Python 3.13 packages | pip only | No torch / transformers |
| Python 3.14 packages | numpy, pillow, scipy, pypdf, markdown, imageio-ffmpeg | No torch |
| Ollama | Client **0.32.0**; daemon was **not running** | Models on disk (see §4) |
| LM Studio | **Not installed** | |
| Sir Thaddeus | **Not found** | No repo / tool / doc hit |

**VRAM law for this machine:** treat 12 GB as a *single exclusive worker*. Official Hunyuan3D-2.1 paint is ~21 GB. Full shape+texture is ~29 GB. Those stages will OOM here. Shape-only Hunyuan (~10 GB) can fit only if Unity is not holding the GPU.

Blackwell also rejects older PyTorch CUDA 12.4 wheels (`no kernel image`). Any future local torch install must be **cu128 / sm_120**, preferably Python **3.10–3.13**, not 3.14.

---

## 2. DCC / engine versions

| Tool | Version / path | Live status |
|------|----------------|-------------|
| Unity (this project) | **6000.4.11f1** HDRP | MCP `unityMCP` connected; `mcpforunity://project/info` confirms `A:/Dark Matter Genesis` |
| Unity Hub leftover editors | 2019.2.2f1, 2022.3.25f1, 2023.2.8f1 | Not the live editor. 6000.4.11f1 is running but not under the usual Hub `Editor` folder probed |
| Playable scenes on disk | `v1.6.2` … **`v1.6.5`** newest | **`v1.6.5` is the main scene** (confirmed 2026-09-30; agent rules updated) |
| Blender | **5.2.0 LTS** (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`) | Installed |
| Blender MCP | `uv run --directory C:\Users\Teabagger\.cursor\tools\blender-mcp blender-mcp` | **Disconnected** (127.0.0.1:9876) |
| 3ds Max | **2022** | Installed; Cursor MCP namespace **error / tools unavailable** |
| ProBuilder | `com.unity.probuilder` 6.1.2 | Installed (blockout / collision helpers) |
| glTFast | `com.unity.cloud.gltfast` 6.19.0 | Installed (GLB ingest if needed) |
| Addressables | 2.9.1 | Installed |
| Unity AI Inference | `com.unity.ai.inference` 2.6.1 | Package present; not an asset factory |
| Unity AI Assistant | `com.unity.ai.assistant` 2.13.0-pre.2 | Editor assistant only |

---

## 3. Unity project structure (relevant to assets)

Canonical paths live in `Assets/_Project/Editor/ProjectAssetPaths.cs`.

| Folder | Role today |
|--------|------------|
| `Assets/_Project/Models/` | Source kits: Meshy FBX, Tripo convert FBX, jetpack, drill, weapons |
| `Assets/_Project/Prefabs/Models/` | **Duplicate** Meshy trees (same filenames; called out in `REPAIR_LOG_2026-08-22.md`) |
| `Assets/_Project/Prefabs/{Combat,Environment,Weapons,Players,Creatures}/` | Game-ready prefabs |
| `Assets/_Project/Meshes/` | Sparse mesh extras |
| `Assets/_Project/Materials/` | Project materials (creatures, etc.) |
| `Assets/_Project/Art/Textures/` | Art textures |
| `Assets/_Project/Editor/` | **213** editor scripts — prefab builders, Genesis Studio, HDRP converters |
| `Assets/_Project/Documentation/Architecture/` | WoOS / HDRP / audits (this file) |

**~94 FBX** under `_Project` (counted via `.fbx.meta`). No dedicated Unity “AI import” folder yet.

GDD art lock (updated 2026-09-30): **full-scale AAA, semi-high-definition art**; modular pieces with attachments and material swaps where useful; HDRP, LODs/Addressables as needed.

---

## 4. Existing AI / 3D generation inventory

Do not assume these are wired into a single button. They exist as **separate islands**.

### 4.1 Dark Matter Asset Engine (DMAE V5) — already the factory

| Item | Path |
|------|------|
| Source of truth | `C:\Users\Teabagger\.claude\skills\unity-mcp-skill\Dark Matter Asset Engine` |
| Blender addon copy | `%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\dark_matter_asset_engine` (v **2.9.0**, Sidebar **DMAE**) |
| Marker | `project_root.txt` → the repo above |
| Policy | **LOCAL ONLY / CLOUD OFF**. No silent downloads. Honest fail if provider missing |

Designed (Aug 2026) for **this same RTX 5070 12 GB / 32 GB / Blender 5.2** machine.

Implemented stages:

1. Intent / route (hard-surface, organic, creature, plant, …)
2. Optional **ComfyUI** concept image (`127.0.0.1:8188`)
3. **TripoSR** Image→3D → `SOURCE_AI`
4. Rebuild constructed `CLEAN_HIGH` (not melted Tripo densify)
5. `GAME_LOW` (default ~10k tris, TRI or QUAD)
6. UV, bake (normal + AO), procedural PBR fillers
7. LOD1–3 + export **GLB + FBX**
8. Collision naming **`UCX_<asset>_<Type>`** (Box / Sphere / Capsule / Convex / Compound)
9. Scale validation in meters; FBX Y-up, −Z forward

Catalog (not all installed):

| Model | Registry | 12 GB fit | License |
|-------|----------|-----------|---------|
| **TripoSR** | Primary | Yes (~6 GB) | MIT |
| Hunyuan3D-2 | `LICENSE_REVIEW` | Shape maybe; paint **no** | Tencent Community |
| SF3D | `LICENSE_REVIEW` | ~7 GB | Stability Community (revenue cap) |
| TRELLIS.2 | `OPTIONAL` stub | Official ≥24 GB | MIT |
| ComfyUI local | Optional concept | Depends on checkpoint | Varies |

**Retopo provider is still a Wave A stub.** Real retopo is DMAE decimate / rebuild / QuadriFlow-style paths, not Instant Meshes.

### 4.2 Local installs under `C:\AI`

| Install | Status |
|---------|--------|
| `C:\AI\TripoSR` | **Present.** `run.py` exists, plus `.venv`, `tsr/`, `output_dmae_smoke` |
| `C:\AI\ComfyUI` | **Present.** Only checkpoint seen: `v1-5-pruned-emaonly.safetensors` (SD 1.5). custom_nodes folder is stock examples |

### 4.3 Ollama (LLM, not 3D)

| Model | Size | Role |
|-------|------|------|
| `qwen2.5-coder:latest` | 4.7 GB | Coding assist |
| `deepseek-coder-v2:16b` | 8.9 GB | Coding assist |
| `qwen3-coder:30b` | **18 GB** | Will not coexist with Unity or TripoSR on 12 GB VRAM |

Ollama is useful for prompt / naming help. It is **not** a mesh generator.

### 4.4 Cloud / imported 3D already in Genesis

The live game already ships **Meshy** and **Tripo** meshes:

- Meshy humanoids, weapons, ammo crate, Ember Skitter, Sulfur Hound / Cragscale, WebPlant, Neon Quantum, Aspid, Emberclad Dragon
- Prefab / combat wiring: `EnemyInvectorSetupUtility`, `PlayerPrefabVisualSetupUtility`, `DMICreaturePrefabBuilder`, Meshy aim-snap
- Tripo convert FBX on Player+v7 / Oriion / Plaver+v61
- Texture suffixes from Meshy: `_texture`, `_normal`, `_metallic`, `_roughness`, sometimes packed `_metallic_roughness`, `_emit`

Meshy is a **production ingest path**, not a local model. Keep it for characters until a local humanoid path is proven.

### 4.5 Not found

Hunyuan weights, InstantMesh, TRELLIS weights, ComfyUI 3D custom nodes, LM Studio, Sir Thaddeus, a project-local Python venv for 3D gen.

---

## 5. Existing conventions to obey

### Naming

| Layer | Convention |
|-------|------------|
| New Unity assets | `DM` / `DMI` / `Dm` — no new Invector names |
| Blender / Max blockout | `GEO_`, `CTRL_`, `HELP_`, `_high` / `_low` |
| DMAE stages | `SOURCE_AI` → `CLEAN_HIGH` → `GAME_LOW` → `LOD1..3` |
| Collision | `UCX_<asset>_<Type>[_##]` |
| Meshy dumps (legacy) | `Meshy_AI_<prompt>_<timestamp>_texture.fbx` — do not relocate casually |

### Textures / PBR (HDRP Lit)

Three dialects already on disk. **New pipeline output should use one:**

| Dialect | Suffixes | Where |
|---------|----------|--------|
| **DMAE / proposed canon** | `{Name}_BaseColor`, `_Roughness`, `_Metallic`, plus baked `_Normal` / `_AO` | DMAE `pipeline/texture.py`, `bake.py` |
| Meshy import | `_texture` (albedo), `_normal`, `_metallic`, `_roughness`, `_emit` | Most character/prop kits |
| Substance / authored | `_BaseColor_sRGB`, `_Normal_Raw`, `_metallic`, `_Height_Raw` | Jetpack, Replicator |

HDRP Lit slots: `_BaseColorMap`, `_MaskMap` (R=metallic, G=AO, B=detail, A=smoothness), `_NormalMap`, `_EmissiveColorMap`.  
Importer note: jetpack albedo already uses **streaming mipmaps**, max 2048. Keep 2K default; 4K only for hero props.

**Do not** put plated item icons on Hot Cross. Map POI colors are locked to the five map swatches.

### FBX / Unity ingest today

- No project-wide `ModelImporter` preset script.
- Scale bugs are real: Meshy / FBX often import at ~100; `DMICreaturePrefabBuilder` **ignores** that scale.
- Humanoid Meshy swaps keep stock VBOT hidden; never cull Meshy body SMRs.
- Creatures: Generic Mecanim for wolf-like; Humanoid only for bipeds.
- Collision used in gameplay prefers **box / sphere / capsule / convex** — non-convex MeshColliders break `ClosestPoint`.

### LOD today

- DMAE: Hero 80k / Near 20k / Mid 8k / Far 2.5k / Background 800 with LOD0–3 tables.
- Unity: terrain impostor baker (`BakeTlmImpostors`); VBOT `Mesh_LOD*` hidden after Meshy swap.
- New props should land with `LODGroup` + DMAE LOD meshes, not a second LOD scheme.

---

## 6. MCP / agent control plane (as configured)

Cursor `~/.cursor/mcp.json`:

| Server | Transport | Status this session |
|--------|-----------|---------------------|
| `unityMCP` | HTTP `127.0.0.1:8080/mcp` | **Ready** |
| `blender` | `uv run` blender-mcp | Server present; **Blender addon listener down** |
| `3dsmax` | HTTP `127.0.0.1:9765/mcp` | **Error** |

Also available in this Cursor session (not asset-factory): Figma, Notion, GitHub, Slack, Linear, AWS, Cloudflare, Datadog, browser.

Project rules already prefer **Blender 5.2 + Blender MCP** for new 3D; Max only when asked. Geometry-first unless the user asks for PBR.

---

## 7. Architecture proposal (build later — Stage 2+)

### Principle

**One factory, one ingest, exclusive GPU.**

```
Cursor agent
    │
    ├─ DMAE (Blender 5.2)          ← generate / refine / bake / LOD / UCX / FBX
    │     ├─ ComfyUI :8188         ← concept plate only (local)
    │     └─ TripoSR C:\AI\TripoSR ← Image→3D SOURCE_AI (~6 GB)
    │
    └─ Unity ingest (new, thin)    ← import FBX+maps, HDRP Lit, LODGroup, convex collider, prefab
          └─ Assets/_Project/…     ← never write into Prefabs/Models Meshy dumps
```

Do **not** start a third generator in the Unity repo. DMAE already owns generate/refine/export. Genesis owns prefab wiring, HDRP materials, and gameplay components.

### Layer A — Local generation (reuse DMAE)

| Decision | Choice | Why |
|----------|--------|-----|
| Primary Image→3D | **TripoSR** at `C:\AI\TripoSR` | Already cloned; MIT; ~6 GB; DMAE wired |
| Text→3D | **Text → ComfyUI plate → TripoSR** | No local text-to-mesh that fits 12 GB well |
| Hunyuan3D | **Do not install in Stage 2** | Paint 21 GB; license review; Blackwell torch pain |
| TRELLIS / TRELLIS.2 | Leave stub | Official VRAM over budget |
| Meshy / Tripo cloud | Keep as **character / hero import** | Already shipped; local humanoid quality unproven |
| Concept checkpoint | Upgrade later from SD 1.5 | Current plate quality will be weak; still no silent download |
| LLM | Ollama `qwen2.5-coder` only while generating | 30B model evicts the 3D job |

**GPU scheduler (required):** stop Play Mode / pause heavy Unity viewports before GENERATE. One process owns the 5070.

### Layer B — DCC refine (reuse DMAE + existing Blender rules)

- Treat Tripo/Meshy melt as **silhouette only**.
- Rebuild `CLEAN_HIGH` (hard-surface inset/bevel or organic remesh).
- `GAME_LOW` ~8–12k for props (budget predates the 2026-09-30 full-scale AAA, semi-high-definition art lock — review TBD); hero weapons up to DMAE Near/Hero tables.
- UVs: smart project / angle seams (already in `blender-aaa-scifi-pipeline.mdc`).
- Bake normal + AO; fill metallic/roughness; optional Comfy albedo.
- Collision: `UCX_*` convex only for gameplay volumes.
- Export FBX (+ optional GLB) with LOD meshes selected.

3ds Max remains a **manual** path for hard-surface artists. Do not block the pipeline on Max MCP.

### Layer C — Unity ingest (the actual missing piece)

New work should be a **small editor assembler**, not a generator:

1. Drop package into `Assets/_Project/Models/Generated/<DM_AssetName>/` (FBX + textures + sidecar JSON).
2. ModelImporter: meters, bake axis, generate lightmap UVs off unless static, mesh compression as existing props.
3. Create HDRP Lit (or existing project Lit template): BaseColor / Mask (pack metallic+AO+smoothness) / Normal / Emissive.
4. Build prefab under the **correct** `ProjectAssetPaths` category (Weapons, Environment, Combat, Items) — not `Prefabs/Models`.
5. Add `LODGroup` if LOD1–3 present.
6. Add convex `MeshCollider` or primitive colliders from `UCX_*`.
7. Stop. Do **not** auto-wire Invector, inventory, or creature brains unless the user asks.

Optional later: a Genesis Studio **Import** subtab that only points at the last DMAE package. Do not put generator sliders in Unity.

### Layer D — Quality gates

Fail closed (DMAE already does this on export):

- Scale in meters, origin on ground, no 100× FBX scale
- GAME_LOW under budget
- Non-manifold / missing UVs reported
- Convex collision present for interactables
- HDRP shader assigned (no URP Lit, no pink)
- Provenance JSON: prompt, provider, license, generation id

### What Stage 2 should be (smallest useful next)

1. Confirm DMAE TripoSR registry points at `C:\AI\TripoSR` (no download if already ready).
2. Start Blender with MCP **Start Server** so Cursor can drive DMAE operators.
3. Run **one** prop smoke: text or image → SOURCE_AI → rebuild → GAME_LOW → FBX (stay outside Unity Play).
4. Design the Unity ingest editor script against that one package.
5. Only then discuss Hunyuan shape-only as an optional second SOURCE provider.

---

## 8. Risks (do not paper over)

| Risk | Detail |
|------|--------|
| VRAM contention | Unity already held ~9 GB during inspect. GENERATE during Play will OOM or thrash |
| System RAM | 32 GB with 1–5 GB free — close browsers / extra Unity instances before Comfy+Tripo |
| Python 3.14 default | Do not install torch on 3.14. Use TripoSR `.venv` or a 3.11/3.13 env + cu128 |
| No CUDA toolkit | Fine for wheels; custom Hunyuan compile will fail until CUDA 12.8 is installed |
| SD 1.5 only | Concept plates will look generic; Io look needs a later local checkpoint (user-initiated) |
| Hunyuan license | Community license (MAU / region clauses) — keep `LICENSE_REVIEW` |
| Meshy duplicates | `Models/` vs `Prefabs/Models/` — ingest must not add a third copy |
| Scene version | Resolved 2026-09-30: v1.6.5 is the main scene |
| Blender MCP down | Factory cannot be agent-driven until addon listener is started |
| Retopo stub | Do not promise Instant Meshes / QuadriFlow quality until that provider is real |
| Character pipeline | Local Image→3D is a **prop/plant/hard-surface** path first. Meshy remains characters |

---

## 9. Explicit non-goals for the next stage

- Do not install Hunyuan / TRELLIS / SF3D weights
- Do not download new Comfy checkpoints unless asked
- Do not rewrite Meshy prefab builders
- Do not retune `Player_v7`
- Do not add NavMesh
- Do not create a second HUD or uGUI importer UI
- Do not delete Design art reference sheets
- Do not treat DMAE AppData copy as editable source — edit the DMAE repo, keep the junction/copy in sync

---

## 10. Sources

- Live inspect: `nvidia-smi`, WMI, `blender --version`, `ProjectSettings/ProjectVersion.txt`, Unity MCP `project/info`
- DMAE: `documentation/DMAE_V5_LOCAL_3D_RESEARCH.md`, `pipeline.md`, `phase14_lod_optimization.md`, `phase15_collision_export.md`
- Public VRAM notes: Hunyuan3D-2.1 README (10 / 21 / 29 GB); RTX 5070 shape-only writeup (cu128 required)
- Genesis: `ProjectAssetPaths.cs`, `DMICreaturePrefabBuilder.cs`, Meshy setup utilities, GDD 5.0 art line, `HDRP_Vendor_Material_Audit.md`
