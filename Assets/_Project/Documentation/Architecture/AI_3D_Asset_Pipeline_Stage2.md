# AI 3D Asset Pipeline — Stage 2 RaydeStar Inspection

**Status:** Inspection only. No pipeline built. No models downloaded. No gameplay code changed.  
**Date:** 2026-09-20  
**Repos (current default tips this session):**

| Repo | Tip SHA | Role |
|------|---------|------|
| [raydeStar/sir-thaddeus](https://github.com/raydeStar/sir-thaddeus) | `3c452a4539ca91dd537f00ca45589cb52a2f027b` | Local Windows LLM workspace |
| [raydeStar/reference-asset-compiler](https://github.com/raydeStar/reference-asset-compiler) | `5b285a0bfe8b1739e1ceaee40daf300038e94681` | Image → gated UE5 character/prop compiler |

This brief is source-backed, not a README paraphrase. Stage 1 machine facts still stand: RTX 5070 **12227 MiB** total, Unity already holding most of the card, DMAE V5 already the Genesis factory.

**Verdict in one paragraph.** Sir Thaddeus is a permissioned chat/MCP *workspace*, not a 3D orchestrator — do not adopt it as the Genesis control plane. Reference Asset Compiler is a serious, hash-bound *Unreal* compiler whose Hunyuan/Pixal3D AI stages assume a **24 GB** class card. Steal its ledger, GPU-refuse, and Blender reduction/UV/bake scripts. Do not copy the UE5 gallery, cook, skeleton, material masters, or Hunyuan-Paint path.

---

## 1. Sir Thaddeus

### 1.1 What it actually does

It is an open-source **local-first Windows assistant**: hybrid .NET runtime + React workspace + stdio MCP child + optional voice sidecar. The user chats with a local OpenAI-compatible model; the model may call permissioned tools (files, wiki, web, system, memory). Persistence is local JSON + SQLite. Trust model: loopback-only, per-launch bearer token, visible permission modal, audit log, kill/stop.

It does **not** generate meshes, textures, LODs, or Unity prefabs. There is no Hunyuan, Blender, FBX, or DMAE integration in this repo.

Inspected topology (`docs/ARCHITECTURE.md` + `src/Thaddeus.Runtime/Chat/AssistantRouter.cs`):

```
User → Thaddeus.Shell → Thaddeus.Runtime (127.0.0.1 + bearer)
                         ├─ Workspace UI (React)
                         ├─ AssistantRouter → LmStudioAssistant → OpenAI-compat LLM
                         ├─ McpClientHost → apps/mcp-server (stdio child)
                         ├─ VoiceHost sidecar (optional)
                         └─ Local stores (threads, settings, wiki, SQLite memory)
```

### 1.2 Architecture (from code, not marketing)

Seven operational layers:

| Layer | Code | Role |
|-------|------|------|
| Shell | `src/Thaddeus.Shell/` | Start/attach runtime, tray, shortcuts, compact panel |
| Runtime | `src/Thaddeus.Runtime/` | Loopback REST + WebSocket, composition root |
| UI | `web/` | Chat, wiki, memory, routines, settings, diagnostics |
| Assistant | `src/Thaddeus.Runtime/Chat/`, `packages/agent/` | History, memory inject, footman, guardrails, repair |
| Model | `packages/llm-client/` | Provider-neutral factory over one HTTP (or Codex CLI) client |
| Tools | `apps/mcp-server/`, `packages/mcp-tools-*` | Manifest-driven MCP tools |
| Voice | `apps/voice-host/`, `apps/voice-backend/` | ASR/TTS sidecar |

Pinned SDK: `global.json` → **.NET 10.0.103**. Also needs Node/npm for the workspace build.

### 1.3 Supported model interfaces

`LlmClientFactory.Create` **always** constructs `LmStudioClient` (`packages/llm-client/SirThaddeus.LlmClient/LlmClientFactory.cs`). There is no separate Ollama or OpenAI class.

`LlmClientOptions.Provider` (`Models.cs`):

- `"lmstudio"` (default) — OpenAI-compatible HTTP at `http://localhost:1234`, path `/v1/chat/completions`
- `"codex"` / `"codex-cli"` — local Codex executable (`CodexCliLlmClient`)
- Settings also document **hosted OpenAI** and **custom OpenAI-compatible** by changing `baseUrl` + `apiKey`
- `"stub"` — `AssistantRouter` skips the LLM entirely when provider is stub or `baseUrl`/`modelId` is blank

`AssistantRouter` rebuilds the cached client when provider/url/model/key/token budgets change. On `HttpRequestException` it falls back to a locally authored failure message (not an echo stub), so the UI never hangs.

**Implication:** “Ollama support” is the OpenAI shim (`localhost:11434/v1`), not the native Ollama REST API. Same for any other OpenAI-compat server.

### 1.4 MCP architecture

Tools are **not** linked into the chat surface. The runtime spawns `SirThaddeus.McpServer` as a **stdio child** (`McpClientHost.cs`). The assistant is an MCP *client*. This is the inverse of Cursor’s setup (Cursor hosts Unity/Blender MCP servers).

Canonical contract: `packages/mcp-shared/SirThaddeus.McpShared/ToolManifest.cs`. Host always registers `mcp-tools-core`; Windows desktop tools are conditional.

Permission groups: safe, screen, files, system, web, memory-read, memory-write. Policy: `off` / `ask` / `once` / `session` / `always`. Wiki writes are call-scoped (session/always do not persist). If the child is missing, the turn degrades to text-only.

There is **no plugin slot** for Blender MCP, Unity MCP, or DMAE. Adding those would mean writing new tool assemblies and registering them in `apps/mcp-server/SirThaddeus.McpServer/Program.cs`.

### 1.5 Tool calling

Per-turn loop (`LmStudioAssistant` + pipeline):

1. `ListToolsAsync` → convert manifest to OpenAI function definitions
2. Optional **footman** (`HeuristicFootmanRouter` or `FastLlmFootmanRouter`) narrows tool *families* before the main model sees them
3. Model emits function calls
4. `ToolPermissionGate` may block and emit `permission.request` to the UI
5. Approved calls cross stdio into the MCP child
6. Results append to history until a final answer or the round-trip cap

`ForcedToolChoiceMode` (`Required` vs `Auto`) exists for certified models that cannot handle a large tool schema. Codex CLI is forced to heuristic-only gatekeeper (no second LLM on the same process).

### 1.6 Memory

Two stores, not one:

| Surface | Storage | Who uses it |
|---------|---------|-------------|
| Semantic memory | SQLite (`packages/memory-sqlite/`), path `ST_MEMORY_DB_PATH` or runtime root | `memory_retrieve` / `memory_store_facts` / update / list / delete; recall chips; reflection |
| Wiki | Markdown tree (`packages/wiki/`), default Documents | User-curated knowledge; page chat / draft / rewrite |
| Threads | JSON files | Conversation history |
| Legacy memos | JSON, read-only migrator | Retired |

`MemoryContextProvider` reads facts through MCP memory tools and a `SmartIntentClassifier`. In heuristic-only/off gatekeeper modes it **avoids a helper LLM call** so it cannot trigger a model swap on a shared endpoint.

### 1.7 File operations

MCP file tools are conservative:

- `file_read` / `file_read_preview` / `file_read_apply`
- `file_write` (atomic exact UTF-8) and `file_replace` (one unique span)
- `file_list*` plus document extraction (`pdf`, `docx`, …)
- Knowledge-store create/append/journal

Hard limits from the architecture inventory:

- Allowlisted roots only; reparse/traversal rejected
- Default write extensions: `.json`, `.yaml`, `.yml`, `.toml`, `.ini`, `.env`, `.md`, `.txt`
- Output cap **1 MiB**
- Post-state receipt after independent reread

This cannot author Unity `.cs` / `.prefab` / `.fbx` / `.mat` as a first-class path. Binary meshes are out of scope.

`system_execute` / `system_execute_preview` / `system_execute_apply` *could* launch PowerShell or DMAE CLI, but that is a generic shell, not an asset pipeline.

### 1.8 Agent workflow

Typed or voice turn:

1. Shell ensures runtime, injects token into `index.html`
2. `POST /api/threads/{id}/messages`
3. Router picks stub vs `LmStudioAssistant`
4. Pipeline may inject personality, memory, dialogue state, guardrails, search fallback, footman narrowing, completion validation, one repair pass
5. WebSocket streams deltas; thread store persists the final message
6. Activity + audit update in parallel

Routines exist as **manual checklists**. Architecture is explicit: *no background scheduler in the current hybrid runtime*. That alone disqualifies it as an unattended compile orchestrator.

### 1.9 LM Studio compatibility

First-class. Settings template:

```json
"llm": {
  "provider": "lmstudio",
  "baseUrl": "http://localhost:1234",
  "model": "replace-with-loaded-model-id"
}
```

`LmStudioClient` also knows LM Studio-specific paths (`/v1/models/load`), warmup/keep-warm, flash attention, KV-cache offload, and `reasoning_content`. Health snapshot field is literally `LmStudioReachable`. Stage 1 already measured **LM Studio is not installed** on this machine.

### 1.10 Ollama compatibility

Documented as an OpenAI-shim preset. Same `LmStudioClient`, different `baseUrl` (typically `http://localhost:11434`). No native `/api/generate` client. Stage 1: Ollama **0.32.0** client present; daemon was flaky; qwen3-coder 30B is **18 GB** — cannot share the 5070 with Unity or 3D gen.

### 1.11 Relevant dependencies

- .NET SDK 10.0.103
- Node.js + npm (`web/` React workspace)
- PowerShell 5.1+
- Optional: LM Studio or Ollama (or hosted OpenAI)
- Optional: Piper/Kokoro voice assets, SearxNG
- No PyTorch, no Hunyuan, no Blender, no Unity packages

### 1.12 Can it reasonably orchestrate Genesis?

**No — not as the control plane.**

| Test | Result |
|------|--------|
| Already installed here? | No (Stage 1) |
| Understands DMAE / Unity HDRP / `ProjectAssetPaths`? | No |
| Hosts Blender or Unity MCP? | No — it *is* an MCP client of its own server |
| Writes FBX / prefabs / HDRP materials? | File tools refuse binaries and most code extensions |
| Unattended compile? | Routines are manual; no scheduler |
| 12 GB VRAM? | Driving it with Ollama/LM Studio *consumes* the same card DMAE needs exclusively |
| Competing product? | Yes — a second chat workspace next to Cursor |

What *is* worth stealing as *ideas* (not a repo import):

- Visible permission gate before `system_execute` / file writes
- Footman-style tool-family narrowing so a small local model is not dumped 60 tools
- Bounded tool loops + repair pass
- Local audit JSONL
- Exclusive-GPU etiquette (RAC does this better; see §2)

Genesis orchestration stays: **Cursor + Blender MCP + Unity MCP + DMAE**. Sir Thaddeus would be a third agent competing for the same GPU and the same user’s attention.

---

## 2. Reference Asset Compiler

### 2.1 Actual workflow

It is a **ledger-gated compiler**, not a one-click mesher. `src/reference_asset_compiler/contracts.py` names the stages. Every workspace has `BASE_STAGES`; articulated assets add `ARTICULATED_STAGES`; static props add `STATIC_STAGES`.

```
intake → route → generate_candidates → modeling_approval (HUMAN)
      → semantic_cleanup → production_retopology (HUMAN)
      → unwrap_and_bake → texture_approval (HUMAN)
      → [articulated] rig_and_skin → deformation_validation
        → ue5_import → ue5_motion_review (HUMAN) → cook (HUMAN)
      → [static] collision_optional → static_validation
        → ue5_import → ue5_runtime_review (HUMAN) → cook (HUMAN)
```

Human gates refuse automation identities (`codex`, `claude`, `agent`, script names) unless a hash-bound `review-delegation.v1` authorization is present. `rac promote` cannot stamp empty evidence into `production_ready: true`. Crashes are recorded; **no silent retry** of inference.

`scripts/compile_from_image.py` is the one-image *static prop* chain. Its own docstring is explicit: it will not invent scale (`--height` + `--height-reason` required), will not call the result approved, and **will not compile a generated humanoid** (rig/UE gates are not wired into that chain).

Default geometry in *that* chain is **Pixal3D via WSL**, not Hunyuan (`generate_geometry.py` `choices=("pixal3d",)`). Hunyuan is a separate launcher (`scripts/run_hy3d_geometry.ps1` + `geometry_stage.py`).

### 2.2 3D generation models

Adapter registry (`configs/model-adapters.json`) — capabilities, not rankings; weights are **not** in the repo:

| Adapter | Role | Official source | Genesis 12 GB |
|---------|------|-----------------|---------------|
| `pixal3d` | Geometry + PBR candidate | TencentARC/Pixal3D | **OOM.** `generate_geometry.py` default `--min-free-vram` is **20000 MiB**. Comment: “Pixal3D at 1024 with low_vram still wants most of a 24 GB card.” WSL required. |
| `trellis2` | Geometry/PBR + existing-mesh texture | microsoft/TRELLIS.2 | **OOM** (Stage 1: official ~24 GB) |
| `hunyuan3d_2_1` | Geometry/PBR + existing-mesh texture | Tencent-Hunyuan/Hunyuan3D-2.1 | Shape maybe; **paint OOM** |
| `anigen` | Mesh + skeleton + weights challenger | VAST-AI-Research/AniGen | Out of scope for props; VRAM unknown, UE-oriented |
| `auto_rig_pro` | Licensed Blender humanoid | User-supplied add-on | Optional; UE5 FBX |
| `blender_custom_rig` / `blender_landmark_rig` | Free landmark fallback | Blender bpy | Reusable idea; output still `ue5_fbx` |

Hunyuan is split across **two isolated studio installs** (`docs/AI_STAGES_SETUP.md`, `geometry_stage.py`):

| Env | Upstream | Job | Free-VRAM floor |
|-----|----------|-----|-----------------|
| `$RAC_LEGACY_ROOT/.venv-hy3d` | `Hunyuan3D-2` | Shape (`hunyuan3d-dit-v2-0`) | Single-view **12288 MiB**; multiview **18432 MiB** |
| `$RAC_LEGACY_ROOT/.venv-hy3d21` | `Hunyuan3D-2.1` @ `82920d6` | Paint PBR | **21504 MiB** |

Verified hardware in their docs: **RTX 4090 24 GB**. Disk for a fresh one-image stack: **~32.3 GiB** (36.9 GiB with both shape models). Launchers **hash-pin** runners and **never kill** user GPU processes (ComfyUI / Blender / UE / Ollama).

**On this 5070:** total VRAM is **12227 MiB**. Hunyuan single-view asks for **12288 MiB free**. Even with Unity closed, the card is ~61 MiB short of their documented floor. Multiview and paint are impossible. Do not install the RAC studio tree on this machine.

### 2.3 Image / reference input

- `rac new` copies the approved image into `work/<asset>/references/primary.png` and records SHA-256. Replacing that file is a new intake, not an edit.
- `geometry_stage.prepare_single_view_request` accepts `.png/.jpg/.jpeg/.webp`, writes an immutable `intake.json`, and refuses a second image under the same workspace name.
- Pixal3D route additionally requires a **real RGBA cutout** (`generate_geometry.check_alpha`). Fully opaque alpha is refused.
- Multiview Hunyuan requires front/left/back guidance views bound by a derivation report. Default launcher mode is **multiview** if `mode` is omitted; single-view must be written explicitly.
- Scale is never inferred from the photo.

### 2.4 Geometry generation

`geometry_stage.py` does **no GPU work**. It builds the request JSON the Hunyuan launcher’s `validate_geometry_request` expects:

- Default parameters (known-good on *their* hardware): `seed=42`, `steps=30`, `octree_resolution=512` (256/384/512 only), `chunks=20000`
- Attempts numbered `hy3d-single-seed{N}-attempt{MMM}`; never overwrite
- Output: `candidate.glb` + `candidate-receipt.json` + `attempt.json`

`generate_geometry.py` (Pixal3D) writes `work/<asset>/candidates/<id>-pixal3d-<res>-seed<N>.glb` plus a `geometry-candidate.v1` lineage report. It names GPU tenants (`comfy`, `python`, `blender`, `unreal`, `ollama`, `koboldcpp`, `lmstudio`) and exits **2** (refused, nothing damaged) when free VRAM is below the floor.

A generated GLB is explicitly **not an asset**.

### 2.5 Texture generation

Two different things:

1. **AI paint** — Hunyuan3D-Paint 2.1 (`run_hy3d21_texture.ps1` / studio `run_hy3d21_pbr.py`). Existing-mesh texturing must restore exact geometry and face order. 21 GiB free. Windows paint process may exit `-1073741819` after writing valid outputs; the wrapper trusts the validation JSON, not the exit code. **Out of budget here.**
2. **Compiler bake** — after approved retopo, `scripts/blender/retopo_bake.py` (~112 KB) plus `bake_pbr.py`, `reunwrap_rebake.py`, `semantic_uv.py`. Texture approval requires `*_production.fbx`, baked PNGs, `retopo.json` hash-bound to source/maps/`gate-tex.json`, and four beauty views. Human gate.

`compile_from_image.py` unpacks whatever maps the *candidate* already carried (`describe_mesh.py`) and later bakes the dense authority onto the reduced mesh. That is transfer/bake, not Hunyuan-Paint.

### 2.6 Blender integration

First-class. `scripts/blender/` is a large `bpy` stage library launched with `blender --python` and `--python-exit-code 1`. Stages delete expected outputs first so a crash cannot be read as last run’s success (`compile_from_image.run_blender_stage`).

Representative stages:

| Area | Scripts |
|------|---------|
| Intake / measure | `stage_generated_mesh.py`, `describe_mesh.py`, `normalize_prop.py`, `normalize_ue5.py` |
| Cleanup | `semantic_cleanup.py`, `heal_mesh.py`, `audit_manifold.py` |
| Reduction / retopo | `reduce_voxel_qem.py`, `reduce_feature_qem.py`, `reduce_instant_meshes.py`, `reduce_quadriflow.py`, `reduce_voxel_quadriflow.py`, `reduce_autoremesher.py`, regional/semantic variants |
| UV / bake | `semantic_uv.py`, `prepare_texture_uv_transport.py`, `retopo_bake.py`, `bake_pbr.py`, `reunwrap_rebake.py` |
| Review | `render_turnaround.py`, `prepare_wireframe_review.py`, `render_uv_debug.py` |
| Rig | `derive_*_landmarks.py`, `rig_from_landmarks.py`, `rig_humanoid_arp.py`, `gate_rig.py`, `deform_test.py` |

This is the most valuable code in the repo for Genesis — **if** ported as ideas/snippets into DMAE, not as a UE-normalized drop-in.

### 2.7 Retopology

Not a single remesher. Production retopo is a **human-gated derivative** of semantic cleanup (`retopology.py`):

- Input mesh hash must equal cleanup output hash
- Report schema `production-retopology-candidate.v1`, status `mechanical_pass`
- Closed two-manifold (`boundary_edges == 0`, `nonmanifold_edges == 0`)
- Vertices/triangles under intake budgets
- Articulated kinds: **≥80% quads** + reviewer attestation `deformation_topology_reviewed`
- Four matcap views ≥640², distinct hashes; articulated also four wireframe views
- Automation reviewers refused without delegation

Reduction backends on disk: voxel QEM (the `compile_from_image` default when over budget), feature-aware QEM, Instant Meshes, QuadriFlow, AutoRemesher, joint-aware pairing. DMAE’s current `providers/retopo.py` is still a Wave A stub — these scripts are the reference implementation to learn from.

### 2.8 UV processing

- Texture only an approved topology
- `retopo.json` binds approved retopo (or UV transport) and every baked map by hash
- `prepare_texture_uv_transport.py` / `export_texture_transport.py` / `export_uv_regions.py` keep UV0 authority when doing region paint
- Neck/head character path preserves original UV0, weights, and skeleton by fingerprint (`CHARACTER_HEAD_AND_NECK.md`) — candidate adapter, not auto-approval
- Historical receipts with missing bindings fail audit; **no silent migration**

### 2.9 LOD generation

There is **no Unity `LODGroup` path**. LOD is an **Unreal native** concern:

- `docs/PIPELINE.md`: “UE can split vertices or change bounds during its own LOD build.”
- Static import verification measures built vertex/triangle/section counts **for every LOD**
- Runtime review can force **LOD0** camera pairs
- Optional reduced-LOD frames are retained as evidence

Do not copy this. Genesis already specified DMAE LOD1–3 + Unity `LODGroup` in Stage 1.

### 2.10 Export format

- Working/candidate: **GLB** from Hunyuan/Pixal3D
- Authority / production: **FBX** (`*_production.fbx`, `SM_<Name>` mesh names, `M_<Name>_Body` materials)
- Sidecar: `*.ue5import.json` (import settings, collision flag, production_reduction block)
- Ledger: `state.json`, hash-bound receipts, `intake.json`
- `compile_from_image` default triangle budget **20 000**, target **18 000** (higher than the Genesis ~8–12k GAME_LOW prop budget, which predates the Sep 30, 2026 full-scale AAA, semi-high-definition art lock — review TBD)

CLI package: `rac` (`pyproject.toml`, Python ≥3.11, deps numpy/Pillow/scipy only — AI is external).

### 2.11 Unreal-specific assumptions (do not pretend these are engine-neutral)

- Terminal proof is **UE 5.8** headless import (`import_and_verify.py`), gallery level, cooked runtime
- Humanoid skeleton must match a **declared UE skeleton contract** (chain names, fingers, max influences, Manny/retarget)
- Material master `M_RAC_CharacterMaster_v002` with `(0,0,1)` tangent normal fallback
- Collision for statics is `ue5_import.generate_collision` in the import JSON — **not** UCX_* convex meshes
- `normalize_ue5.py` exists as a dedicated Blender axis/scale pass
- Cook evidence and playable gallery are first-class stages
- GitHub code search for `Unity` / `HDRP` / `LODGroup` in this repo returned **no hits**

### 2.12 What can be adapted to Unity / DMAE

Adapt the *contract*, not the engine payload:

1. **Immutable intake** — hash the reference image; never overwrite in place
2. **Candidate ≠ asset** — lineage receipt (`geometry-candidate.v1`) bound to image + adapter + runner hash
3. **Human gates** at modeling, retopo, and texture, with four fixed views
4. **Refuse, don’t kill** — nvidia-smi free-VRAM floor; name tenants; exit without touching the attempt dir
5. **No silent retry** of crashed inference
6. **Numbered attempts** instead of overwrite
7. **Blender reduction library** — voxel QEM, Instant Meshes, QuadriFlow, manifold audit
8. **UV authority** — bake only after approved topology; bind maps by hash
9. **Required real-world scale** with a written reason
10. **workflow_doctor** style preflight (ledger / geometry / texture / engine as separate profiles)

Map RAC stages onto DMAE + Unity ingest:

| RAC | Genesis equivalent |
|-----|-------------------|
| intake / route | DMAE job folder + prompt/image hash |
| generate_candidates | ComfyUI plate → TripoSR → `SOURCE_AI` |
| modeling_approval | Human look at matcaps (optional later) |
| semantic_cleanup + production_retopology | DMAE CLEAN_HIGH → GAME_LOW (fill the retopo stub from RAC ideas) |
| unwrap_and_bake | DMAE UV + bake (`_BaseColor/_Roughness/_Metallic/_Normal`) |
| texture_approval | Human / later ingest check |
| collision_optional | **UCX_*** convex (DMAE), not UE generate_collision |
| ue5_import / cook | **Unity ingest**: FBX importer, HDRP Lit, LODGroup, prefab under `ProjectAssetPaths` |

### 2.13 What must NOT be copied

- The whole repo as a sibling factory next to DMAE
- Hunyuan-Paint 2.1 / Pixal3D 20 GB floors / TRELLIS.2
- `$RAC_LEGACY_ROOT` studio tree (~32–37 GiB) on this 12 GB machine
- UE 5.8 project, gallery, cook, `*.ue5import.json` as the ship artifact
- UE skeleton / Manny / Auto-Rig Pro UE export as a requirement for Io props
- `M_RAC_CharacterMaster_*` and UE tangent-normal policy
- Native UE LOD build as the LOD source of truth
- `ue5_import.generate_collision`
- WSL-only Pixal3D as the default Genesis generator
- Automation-stamped “production_ready” without a human (their own rule — keep it)
- Default 18–20k triangle budgets (Genesis sets its own budgets; review against the Sep 30, 2026 art lock — TBD)
- ComfyUI-Hunyuan3DWrapper graph as a hidden second path (RAC itself already bypassed it)

---

## 3. Combined recommendation for later stages

```
Cursor (this workspace)
  → DMAE in Blender 5.2 (generate + refine + FBX)
  → Thin Unity ingest (HDRP Lit, LODGroup, UCX, prefab)
```

| Source | Take | Leave |
|--------|------|-------|
| Sir Thaddeus | Permission UX, audit, tool-family narrowing *ideas* | The app, its MCP server, Ollama-on-the-game-GPU |
| RAC | Ledger / refuse-GPU / Blender retopo-UV-bake *ideas* | UE5 compiler, Hunyuan-Paint, Pixal3D-24GB, cook/gallery |
| DMAE (already here) | Factory of record | Do not replace it |

**Still do not install** Hunyuan, Pixal3D, TRELLIS.2, or Sir Thaddeus for Genesis work on the 5070.

Stage 1 brief: `AI_3D_Asset_Pipeline_Stage1.md`.
