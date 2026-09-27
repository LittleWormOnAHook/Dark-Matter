# AI 3D Asset Pipeline — Stage 3 Model Options (12 GB)

**Status:** Research only. No models downloaded this stage. No pipeline built.  
**Date:** 2026-09-20  
**Constraint:** NVIDIA RTX 5070, **12227 MiB** total, Blackwell `sm_120`. Unity already held ~9 GB in Stage 1. Treat the card as a **single exclusive worker**.

**Requirement:** high-quality *game* assets (Unity 6 HDRP, FBX, PBR, LOD, collision), not demo GLBs.

**Honest ceiling:** no current open local model produces ship-ready high-quality game assets *entirely* on 12 GB. The models that do (TRELLIS.2 PBR, Hunyuan-Paint 2.1) need 21–24 GB+. On this machine, quality has to come from **DMAE refine** (CLEAN_HIGH → GAME_LOW → UV → bake → UCX → LOD → FBX). The generator only has to produce a usable `SOURCE_AI` silhouette.

Blackwell caveat for every official install recipe below: Hunyuan 2.1 tests **PyTorch 2.5.1+cu124**; InstantMesh tests **cu121**; TRELLIS.2 defaults to **2.6.0+cu124**. Those wheels do not include `sm_120`. A local torch install here must be **cu128** (or later) on Python **3.10–3.13**, not 3.14.

---

## Verdict (do not pick by popularity)

| Rank | Model | Why it wins or loses on *this* card | Action |
|------|--------|--------------------------------------|--------|
| 1 | **TripoSR** | MIT, ~6 GB, Windows CLI, already cloned at `C:\AI\TripoSR`, DMAE already calls it | **Primary generator** |
| 2 | **Stable Fast 3D** | ~6 GB, UV unwrap + delight + material params — closest *game-shaped* output that fits | Optional after **license review** |
| 3 | **SPAR3D** | SF3D successor; official default 10.5 GB, **~7 GB** low-vram; better backside | Optional after same license review |
| 4 | **Hunyuan3D-2mini / mini-Turbo** | Official **6 GB shape**; 0.6B; better geometry than TripoSR in published comparisons | Optional *shape-only* after license + cu128 proof |
| — | Hunyuan3D-2.0 full + Paint v2-0 | Shape 6 GB, **shape+texture 16 GB** | Paint **OOM** |
| — | Hunyuan3D-2.1 | Shape 10 GB, paint **21 GB**, both **29 GB** | Paint **OOM**; shape only if Unity is fully off and cu128 works |
| — | Hunyuan3D-2.5 | Paper + HF card only; same 6 / 16 GB claim as 2.0; not a separate 12 GB miracle | Do not treat as a new local stack |
| — | Hunyuan3D 3.x | Hosted / studio, not a clear open local weights drop | Out of scope |
| — | TRELLIS 1.0 | Official **16 GB**, Linux-tested | Unreliable on 12 GB |
| — | **TRELLIS.2-4B** | Official **24 GB**, Linux + CUDA compile; community peak ~26–27 GB | **Do not install** |
| — | InstantMesh-large | Community: ~24 GB; base maybe 16 GB | **Do not install** |
| — | Unique3D | Multi-view diffusion + ISOMER; Windows install is fragile; no honest 12 GB floor | Skip |

Text→mesh on this card: keep **text → local ComfyUI plate → image-to-3D**. Native text-to-3D (TRELLIS-text, Hunyuan text) is weaker and hungrier.

---

## Shared hardware law

| Fact | Number |
|------|--------|
| Card total | 12227 MiB |
| Hunyuan 2.1 official single-view *free* floor (RAC) | 12288 MiB — **61 MiB above this card** |
| Hunyuan 2.1 paint | 21504 MiB free |
| TRELLIS.2 official | 24576 MiB |
| Exclusive GPU | Unity Play / ComfyUI / Ollama must be off during 3D inference |

Quantization: **no official 8-bit weights** for Hunyuan-Paint, TRELLIS.2, SF3D, or TripoSR. Community **mmgp / Hunyuan3D-2GP** offloads Hunyuan-2 to ~6–9 GB (shape) but paint remains unofficial, slow, and not a production path.

---

## Dual RTX 5070 (12 GB + 12 GB) — what changes

**Two 12 GB cards are not 24 GB.** Consumer GeForce 50-series has **no NVLink** and no VRAM pool. PyTorch / Hunyuan / TRELLIS / TripoSR load a model onto **one** `cuda:N` device. Hunyuan-Paint (21 GB) and TRELLIS.2 (24 GB) still do not fit on either card.

| Still OOM on each GPU | Unlocks with a dedicated inference GPU |
|-----------------------|----------------------------------------|
| TRELLIS.2-4B (≥24 GB) | Keep Unity / Play on GPU 0 (~9 GB) and run gen on GPU 1 |
| Hunyuan-Paint 2.1 (21 GB) | Hunyuan 2.1 **shape-only** (~10 GB) without closing the editor |
| Hunyuan 2.0 shape+paint (16 GB) | SPAR3D default 10.5 GB without fighting Unity |
| InstantMesh-large (~24 GB) | Parallel ComfyUI plate on one GPU, TripoSR/SF3D on the other |
| RAC Pixal3D 20 GB free floor | Exclusive-GPU law becomes `CUDA_VISIBLE_DEVICES=1`, not “quit Unity” |

InstantMesh’s official two-GPU demo **splits stages** (views on one, reconstruct on the other). That saves *some* peak, it does not add 12+12. Hunyuan paint cannot live on GPU 0 while shape lives on GPU 1 to “make 21 GB fit” — the paint weights themselves need ~21 GB on the paint device.

**System RAM** is the other limiter: this machine is **32 GB** and was already 1–4 GB free with Unity open. A second GPU adds another display/CUDA context. Dual-GPU does **not** fix RAM.

**Recommendation if a second 5070 is added:** pin Unity to GPU 0, pin DMAE/TripoSR (and later SF3D / Hunyuan-mini shape) to GPU 1. Ranked model list does **not** change — still no local full-PBR factory. A single **24 GB** card (4090 / 5090 32 GB) would change the list; a second 12 GB card changes *workflow isolation* only.

---

## 1. TripoSR (VAST + Stability)

| Field | Finding |
|-------|---------|
| Model | Feed-forward LRM-style image→3D. Official repo `VAST-AI-Research/TripoSR`. |
| License | **MIT** (code + weights). Cleanest commercial terms of anything that fits. |
| VRAM | Official default **~6 GB** per image. |
| Quantization | None official. Not needed. |
| Image → mesh | Yes (required). |
| Text → mesh | No. |
| Texture | Optional `--bake-texture` (albedo bake). Default is **vertex colors**. |
| PBR | No metallic/roughness prediction. |
| Output | Mesh in `output/`; bake path writes a texture. DMAE imports as `SOURCE_AI`. |
| Mesh quality | Fast, blob-prone, weak thin parts. Fine as a *blockout*. |
| Hard-surface | Weak (rounded edges, fused parts). DMAE hard-surface rebuild required. |
| Organic | Acceptable silhouettes; topology is not deformation-ready. |
| Local | Yes. `python run.py <image> --output-dir <dir>`. |
| Windows | Yes. Already present on this machine. |
| Python | ≥3.8. Use 3.10–3.13 here. |
| CUDA | Any current PyTorch CUDA that matches the driver. For 5070: **cu128**. `torchmcubes` must be rebuilt against that torch. |

**Fit:** best *primary* because it is legal, small, already wired, and DMAE was designed around “AI mesh is a candidate, not an asset.”

---

## 2. Stable Fast 3D (SF3D)

| Field | Finding |
|-------|---------|
| Model | TripoSR descendant. Official `Stability-AI/stable-fast-3d`. |
| License | **Stability AI Community License** (July 5, 2024). Research / personal free. Commercial free only while the org (and affiliates) make **< USD $1,000,000 / year**. Above that the license **terminates**; enterprise license required. Attribution (“Powered by Stability AI”) on distribution. Cannot use outputs to train another foundational generative model. |
| VRAM | Official **~6 GB** single image. Marketing: 0.5 s on a GPU with 7 GB. |
| Quantization | None official. |
| Image → mesh | Yes. |
| Text → mesh | No. |
| Texture | Yes — UV-unwrapped albedo, **delight** (lighting removed). |
| PBR | Predicts **material parameters** (not a full Hunyuan/TRELLIS.2 PBR stack, but game-integrable). |
| Output | **GLB**. |
| Mesh quality | Better artifact control than TripoSR; still single-view backside hallucination. |
| Hard-surface | Better than TripoSR, still not CAD-clean. |
| Organic | Good for props/creatures as a candidate. |
| Local | Yes. `python run.py …` |
| Windows | Supported in practice (same stack as TripoSR). |
| Python | ≥3.8. |
| CUDA | Same Blackwell cu128 rule. |

**Fit:** strongest *quality* upgrade that still fits 12 GB. Blocked on **license review** for a commercial ship (GDD is a commercial PC/console title). DMAE already catalogs it as `LICENSE_REVIEW`.

---

## 3. SPAR3D (Stable Point-Aware 3D)

| Field | Finding |
|-------|---------|
| Model | SF3D + point-cloud conditioning for the occluded side. `Stability-AI/stable-point-aware-3d`. |
| License | Same **Stability Community** family (gated HF weights; request access + token). |
| VRAM | Official: **10.5 GB** default; **`SPAR3D_LOW_VRAM=1` / `--low-vram-mode` → ~7 GB**. The README’s “default ~6 GB” line is a leftover from SF3D — trust the Low VRAM section. |
| Quantization | None; low-vram is offload/slower, not 8-bit weights. |
| Image → mesh | Yes. Point cloud can be edited. |
| Text → mesh | No. |
| Texture | UV + improved material prediction vs SF3D. |
| PBR | Material parameters; remesh options (none / triangle / Instant Meshes quads). |
| Output | GLB. |
| Mesh quality | Best *feed-forward* backside of the 12 GB class. |
| Hard-surface | Quad remesh option helps; still a candidate. |
| Organic | Stronger than SF3D on tails / backs. |
| Local | Yes. HF gated. |
| Windows | **Experimental**; needs VS 2022. Quality/perf not guaranteed vs Linux. |
| Python | ≥3.8 (often >3.9 depending on torch). |
| CUDA | cu128 for 5070; VS C++ for extensions. |

**Fit:** optional #3 if SF3D license is accepted and backside quality matters (creatures). 7 GB low-vram fits; 10.5 GB default is tight with any desktop leftover.

---

## 4. Hunyuan3D family

Tencent **Hunyuan 3D 2.0 Community License** (read in full this session):

- Territory **excludes EU, UK, South Korea**
- Outputs belong to the user
- If all products using the model exceed **1 million MAU** as of the 2.0 release date, a separate Tencent license is required
- Cannot use outputs to improve *other* AI models
- AUP bans military use (among other items)
- “Powered by Tencent Hunyuan” encouraged on products

Same **LICENSE_REVIEW** status as DMAE already recorded. US indie under 1M MAU is *probably* in bounds — legal review, not an agent call.

### 4.1 Hunyuan3D-2.0 / 2mini / 2mv / Turbo

| Field | Finding |
|-------|---------|
| Model | Two-stage: Hunyuan3D-DiT (shape) + Hunyuan3D-Paint (texture). Mini is **0.6B**; full / mv are **1.1B**. Turbo + FlashVDM distill steps. |
| License | Tencent Hunyuan Community (above). |
| VRAM | Official **6 GB shape**, **16 GB shape+texture**. Mini + `--low_vram_mode` is the 12 GB shape path. |
| Quantization | Official: none. Community **Hunyuan3D-2GP / mmgp** profiles 3–4 target 9 GB / 6 GB via offload. |
| Image → mesh | Yes (primary). Multiview model if front/left/back exist. |
| Text → mesh | API/server claims text; official note and RAC practice: image is the honest path. |
| Texture | Paint v2-0 / Turbo — **16 GB combined**, so **not on this card**. |
| PBR | v2-0 paint is textured; **true PBR maps are the 2.1 paint model** (21 GB). |
| Output | trimesh → GLB/OBJ. Blender add-on talks to a local API server. |
| Mesh quality | Best *open shape* in the 12 GB class when mini/turbo runs. Still needs retopo. |
| Hard-surface | Better part separation than LRM/TripoSR; not boolean-clean. |
| Organic | Strong. |
| Local | Yes. Weights on Hugging Face. |
| Windows | Official yes; community WinPortable + ComfyUI-Hunyuan3DWrapper exist. |
| Python | 3.10 typical. |
| CUDA | Official recipes are older CUDA. **cu128 required here.** Custom rasterizer / differentiable renderer compile for paint (paint is OOM anyway). |

### 4.2 Hunyuan3D-2.1

| Field | Finding |
|-------|---------|
| Model | Shape-v2-1 **3.3B** + Paint-v2-1 **2B** PBR. Training code open. |
| VRAM | Official: **10 GB** shape, **21 GB** texture, **29 GB** both. RAC launcher free floors: 12288 / 18432 / 21504 MiB. |
| Quantization | Issue #15: community mmgp 8-bit *paint* offload on 12 GB cards is unofficial, slow, and not a ship path. |
| Texture / PBR | Yes — the quality people mean when they say “Hunyuan.” **OOM here.** |
| Windows | Official yes. RAC paint rasterizer is Windows-painful and still 21 GB. |
| Python | Official test: 3.10 + torch 2.5.1+**cu124** (wrong for Blackwell). |

**Do not install 2.1 paint or the RAC `$RAC_LEGACY_ROOT` studio tree (~32–37 GiB) on this machine.**

### 4.3 Hunyuan3D-2.5 / 3.x / Part

- **2.5:** technical report (arXiv 2506.16504) and HF card text. Same published 6 / 16 GB line as 2.0. Not a new 12 GB paint solution.
- **3.x:** hosted Hunyuan3D Studio / API, not a clear open local weights drop.
- **Hunyuan3D-Part (P3-SAM / X-Part):** part segmentation, not image-to-3D. Irrelevant as a generator.

---

## 5. TRELLIS / TRELLIS.2

### 5.1 TRELLIS 1.0 (`microsoft/TRELLIS`)

| Field | Finding |
|-------|---------|
| License | **MIT** (code + models). Submodules: diffoctreerast, Flexicubes have their own licenses. |
| VRAM | Official **≥16 GB**. Verified A100 / A6000; 3090/4090 community reports exist. |
| Quantization | None official. |
| Image → mesh | Yes (`TRELLIS-image-large`, 1.2B). Recommended path. |
| Text → mesh | Yes (base/large/xlarge) — authors say **use image models instead**; text is less detailed. |
| Texture | Baked into GLB from Gaussian + mesh (`texture_size=1024`). Not a full metallic/roughness stack like TRELLIS.2. |
| Output | Mesh, 3D Gaussians, radiance field, GLB, PLY. |
| Quality | High for 2024; sharp features better than LRM. |
| Local | Yes, but **Linux-tested**. Windows = community issue #3, “not fully tested.” |
| Python | ≥3.8; conda + compile CUDA extensions (flash-attn, nvdiffrast, kaolin, spconv). |
| CUDA | Tested 11.8 / 12.2. Blackwell would need a full extension rebuild on cu128. |

**12 GB:** below official floor. Do not plan on it.

### 5.2 TRELLIS.2-4B (`microsoft/TRELLIS.2`)

| Field | Finding |
|-------|---------|
| License | **MIT**. nvdiffrast / nvdiffrec have their own licenses. |
| VRAM | Official **≥24 GB**, verified A100/H100. Community 5090 peak **26–27 GB**. 16 GB Blackwell (5070) **cannot load** the pipeline. |
| Quantization | None official. |
| Image → mesh | Yes. 4B, O-Voxel, 512³–1536³. |
| Text → mesh | Not the released 4B path. |
| Texture / PBR | **Base color, roughness, metallic, opacity.** Shape-conditioned texturing exists. This is the quality target — and it does not fit. |
| Output | GLB via `o_voxel.postprocess.to_glb` (remesh, 4K textures). |
| Local | Linux + conda + CUDA 12.4 toolkit to compile o-voxel / flexgemm / cumesh / nvdiffrast. |
| Windows | Not officially tested. |

**Do not install. Do not put a ComfyUI “low VRAM TRELLIS.2” fork on the critical path.**

---

## 6. Other current open image-to-3D systems

### InstantMesh (TencentARC)

- Apache 2.0. Image → Zero123++ views → sparse-view LRM mesh.
- Official recipe: Python ≥3.10, torch 2.1+**cu121**.
- Community: `instant-mesh-base` on 16 GB T4; **large needs ~24 GB**. One Windows 3090 report: **23.3 GB**.
- Texture via `--export_texmap` (slow UV).
- **Reject for 12 GB.**

### Unique3D

- MIT (community reports). Multi-view + normal diffusion + ISOMER reconstruction.
- Windows: experimental bat + Triton wheel + VS Build Tools + cu121.
- No published 12 GB floor; multi-view diffusion typically exceeds it.
- **Skip.** Higher quality on paper, not a reliable 5070 worker.

### OpenLRM / LGM / Wonder3D / CRM / CraftsMan3D

- Older 2023–24 feed-forward or SDS stacks. Either superseded by TripoSR/SF3D (same quality class, worse tooling) or SDS-slow.
- Not worth a new env on this machine.

### Meta SAM 3D / HunyuanWorld

- SAM 3D Objects is reconstruction/segmentation, not a game-asset factory.
- HunyuanWorld-1.0 is *world* generation, VRAM-heavy, wrong problem.

---

## 7. Comparison matrix (12 GB game-asset test)

| Model | License | Official VRAM | 12 GB? | Img→mesh | Txt→mesh | Texture | PBR | Formats | Hard-surface | Organic | Local Win | Py / CUDA |
|-------|---------|---------------|--------|----------|----------|---------|-----|---------|--------------|---------|-----------|-----------|
| TripoSR | MIT | ~6 GB | **Yes** | Yes | No | Bake optional | No | mesh / tex | Weak | OK blockout | Yes | 3.8+ / cu128 |
| SF3D | Stability <$1M | ~6 GB | **Yes** | Yes | No | UV + delight | Material params | GLB | Fair | Good | Yes | 3.8+ / cu128 |
| SPAR3D | Stability gated | 10.5 / **7** low | **Yes** (low) | Yes | No | UV + mats | Material params | GLB | Fair + quad remesh | Better back | Experimental | 3.8+ / VS2022 / cu128 |
| Hy3D-2mini | Tencent community | 6 GB shape | **Shape only** | Yes | Weak | Paint = 16 GB | Paint 2.1 = 21 GB | GLB/OBJ | Better | Strong | Yes | 3.10 / cu128 |
| Hy3D-2.1 | Tencent community | 10 / 21 / 29 | Shape maybe | Yes | Weak | Yes | Yes | GLB | Strong | Strong | Yes | 3.10 / official cu124 **wrong** |
| TRELLIS 1 | MIT | ≥16 GB | No | Yes | Yes (worse) | Bake | Partial | GLB/PLY/GS | Good | Good | Community | ≥3.8 / compile |
| TRELLIS.2 | MIT | ≥24 GB | **No** | Yes | No | Yes | Full | GLB | Best open | Best open | Linux only | ≥3.8 / CUDA 12.4 compile |
| InstantMesh-L | Apache-2.0 | ~24 GB | **No** | Yes | No | UV optional | No | mesh | Fair | Fair | Fragile | 3.10 / cu121 |

---

## 8. What “high-quality game asset” means here

A Meshy/Tripo *cloud* FBX already in `_Project` is not the bar for local gen. Local open models on 12 GB will not match that textured quality in one shot.

The only architecture that satisfies the requirement:

```
concept image (ComfyUI or hand)
  → 12 GB generator (TripoSR now; SF3D/SPAR3D or Hy3D-mini later)
  → DMAE SOURCE_AI
  → CLEAN_HIGH / GAME_LOW / UV / bake / UCX / LOD / FBX
  → Unity HDRP ingest
```

Do **not** wait for TRELLIS.2 or Hunyuan-Paint to “fit” this card. They will not.

---

## 9. Recommended next install (when Stage 4 is authorized)

1. **Register TripoSR** in DMAE (`DMAE_TRIPOSR_ROOT` / Advanced panel). Dedicated Python 3.12 or 3.13 + **torch cu128**. Unity Play stopped.
2. License review: Stability Community vs Tencent Community vs MIT-only. Until legal sign-off, **do not download SF3D, SPAR3D, or Hunyuan weights**.
3. After review, SF3D is the first *quality* challenger (same VRAM class, UV+materials).
4. Still do not download TRELLIS.2, Hunyuan-Paint 2.1, Pixal3D, or InstantMesh-large.

Stage 1: `AI_3D_Asset_Pipeline_Stage1.md`  
Stage 2: `AI_3D_Asset_Pipeline_Stage2.md`
