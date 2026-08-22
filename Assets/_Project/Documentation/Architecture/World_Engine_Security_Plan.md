# World Engine Security Plan

**Status:** Living document (planning)  
**Audit basis:** `World_Engine_Disk_Status.md` (July 22, 2026)  
**Authority:** HLA v1.0 + GDD 5.0 Appendix B  
**Related:** `Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md`, `Dark_Matter_Framework_Engineering_Standard.md`

---

## Purpose

Early security planning for **Dark Matter: Genesis** and the proprietary **Living World Engine** (World Operating System / WoOS). Focus areas:

1. **Anti-piracy** — unauthorized distribution of the retail game
2. **Engine IP secrecy** — protecting World Engine architecture, algorithms, and design assets

This document is planning guidance, not shipped runtime security. Revisit at each major milestone (Communications Run 2, persistent world, IAP, LLM Phase 9+).

---

## Rule

Do **not** treat this file as evidence that security controls are implemented. Check disk status and code for actual controls. Today: **100% client-authoritative, no DRM, no save integrity, no network boundary.**

---

## 1. Separate the two problems

| Threat | What attackers want | What you're protecting |
|--------|---------------------|-------------------------|
| **Piracy** | A playable copy without paying | Revenue, platform compliance |
| **IP / engine secrecy** | World Engine design, algorithms, content pipelines | Competitive moat, future licensing (HLA §1.5 "Framework export") |

They overlap but need different answers:

- **Piracy** is mostly solved at **distribution** (Steam, Epic, consoles) plus legal/process — not custom DRM in code.
- **Engine secrecy** is mostly solved by **what never ships** and **what runs server-side** — not obfuscation alone.

A shipped offline game **cannot** fully hide logic that must run on the player's machine. Plan around that constraint.

---

## 2. Current state (disk truth)

**Trust model today:** 100% client-authoritative.

| Area | Risk | Detail |
|------|------|--------|
| Saves | High | Plain JSON at `savegame_slot{N}.json` — AC, inventory, roster, quests all editable |
| Economy | High | `PioneerRosterManager` balance checks are local only |
| Networking | None yet | No authoritative server |
| Anti-tamper / DRM | None | No IL2CPP hardening, signing, or platform DRM wired in |
| World Engine | On disk | `GameState` → `WorldState` → `Directors` → (future) `Communications` |

**Strategic IP (worth protecting):**

- HLA / TDB architecture and snapshot contracts
- Director orchestration and intent/command model
- Communications context-pack pipeline (when built)
- `EchoGenerator`, procedural world/seed design, Io ecology roster
- Design docs and art reference under `Documentation/Design/`

**Lower strategic value in the binary:** individual gameplay scripts, Invector wrappers, UI palette code.

---

## 3. Recommended security posture by phase

Aligned with GDD B4 track: Communications → persistent world → living-world slice.

### Phase A — Now (architecture & process)

Cheap now; painful to retrofit later.

#### A1. Repo and build boundaries (engine vs game)

HLA targets optional "Framework export." Treat:

| Layer | Paths |
|-------|-------|
| **World Engine (proprietary)** | `Features/GameState`, `WorldState`, `Directors`, `Communications`, `Generation`, `Experience` |
| **Game adapters (Genesis)** | `Features/*/Adapters`, `Scripts/`, content |
| **Third-party** | Invector, vendor assets (never mixed into engine namespaces) |

Concrete steps:

- Private repo; restrict World Engine folders if sharing builds or tools
- CI builds from locked branches; no engine source in public artifact drops
- Formalize who can merge engine changes (see `tools/sync-world-engine-branch.ps1` pattern)

#### A2. Secrecy tier per subsystem

| Tier | Runs where | Examples | Protection |
|------|------------|----------|------------|
| **Tier 0 — Public** | Client, reverse-engineerable | Combat, UI, most gameplay | Accept exposure |
| **Tier 1 — Obscured** | Client, hardened | Director eval, simulation tuning, seed→world rules | IL2CPP + strip debug + no dev symbols in release |
| **Tier 2 — Server-only** | Backend | LLM prompts, premium AC ledger, analytics | Never in client binary |
| **Tier 3 — Never ship** | Dev / pipeline only | HLA, TDB, ecology design, LifeSheets, authoring tools | Legal + access control |

#### A3. Design hooks now (stubs OK)

Add interfaces early so server authority can land without rewiring managers:

- `IAetherCreditsAuthority` — local now, server when IAP ships
- `ISaveIntegrityProvider` — noop now, HMAC/signing later
- `IWorldSeedAuthority` — local seed now, optional cloud attestation later
- `IConversationProvider` — already planned: Template (offline) vs OpenAI (remote)

Snapshots and DI (`IGameStateService`, `IWorldStateService`, directors read snapshots only) are the correct security perimeter.

#### A4. Legal layer

- Copyright on code + design docs
- Employee/contractor IP assignment
- NDA for engine repo access
- EULA: no reverse engineering, no redistribution
- Trademark "Dark Matter" / "World Engine" if licensing is planned

---

### Phase B — Before beta (ship hygiene)

#### B1. Release build hardening (PC + consoles)

| Control | Purpose |
|---------|---------|
| **IL2CPP** | Harder to decompile than Mono; required for consoles |
| Strip debug symbols from release | Slows reverse engineering |
| No `DarkMatterSmokeDriver` / F9–F11 in release | Removes debug attack surface |
| Selective managed obfuscation (optional) | Diminishing returns; Tier 1 assemblies only |

Avoid heavy obfuscation during active development — it fights debugging and AI-agent workflows.

#### B2. Save integrity (proportionate to stakes)

For **single-player offline**, perfect save protection is unrealistic.

| Level | Effort | Stops |
|-------|--------|-------|
| **L0** (today) | None | Casual JSON editing |
| **L1** | Compress + non-obvious field names | Casual users |
| **L2** | HMAC/sign with embedded key | Casual + some tools (key still in binary) |
| **L3** | Server-signed saves (cloud save) | Most local tampering |

**Recommendation:** L0–L1 until IAP or leaderboards matter. When AC is purchasable with real money, **economy must move to Tier 2 (server)**.

#### B3. Platform DRM (primary anti-piracy lever)

| Platform | Built-in |
|----------|----------|
| **Steam** | Steam DRM (light), Steamworks entitlement check |
| **Epic** | Epic Online Services entitlement |
| **PS5 / Xbox** | Platform DRM + signed packages |

Do **not** deploy BattlEye/EAC for offline single-player.

---

### Phase C — Communications & LLM (Run 2 + Phase 9+)

First real **network trust boundary**.

**Protect on server:**

- API keys (backend proxy only)
- Prompt templates and system instructions
- Rate limits, billing, abuse detection

**Client sends:** sanitized `CommunicationsContextPack` (snapshots), not raw manager state.

**Threats:**

- Prompt injection via player-named items / echo text
- Context pack exfiltration
- Replay / spoofed API calls → auth tokens tied to platform ID

Engineering Standard §8 ("AI never calls managers directly") is a **security boundary**.

---

### Phase D — If/when IAP for Aether Credits

GDD allows optional IAP later. Rules:

| Rule | Why |
|------|-----|
| Server is source of truth for AC balance | Client displays; server grants on verified purchase |
| Receipt validation | Steam/Epic/PSN/Xbox server-side |
| Idempotent grant endpoints | Prevent double-spend / replay |
| Offline play | Cache last known balance; reconcile on reconnect |

Without server authority, AC IAP will be cracked regardless of save encryption.

---

## 4. Living World Engine — what to keep secret

### What will leak if it runs offline

Anything required for offline play **will** be recovered by motivated reverse engineers:

- Director evaluation logic
- Weather/simulation rules
- Procedural generation from seed
- Echo generation algorithms

**Goal:** make extraction **expensive and incomplete**, not impossible.

### What you can keep proprietary

| Asset | Strategy |
|-------|----------|
| Architecture docs (HLA, TDB, reasoning maps) | Never in player build |
| Authoring tools & editor extensions | Dev-only or licensed package |
| Tuning data & ScriptableObject libraries | Encrypted blobs for Tier 1 if worth it |
| LLM prompts & model orchestration | Server-only |
| Premium simulation features | Online-only or subscription backend |
| Framework export / licensing SDK | Separate package, legal license |

### Framework export licensing (HLA §1.5)

If licensing the World Engine:

- Ship **compiled assemblies** + documentation, not full source (unless enterprise tier)
- Genesis-specific adapters stay game-repo-only
- Versioned snapshot contracts as the public API surface
- Watermarked builds per licensee

---

## 5. Threat model (condensed)

```
                    ATTACKER GOALS
    ┌─────────────────────────────────────────────┐
    │ Piracy          │ Cheat/IAP fraud │ IP theft │
    └────────┬────────┴────────┬────────┴────┬─────┘
             │                 │             │
    Platform DRM          Server AC      Tier 3 docs
    + legal               authority      never ship
             │                 │             │
    IL2CPP release        Signed saves     IL2CPP on
    builds                (if needed)      Tier 1 only
             │                 │             │
             └────────┬────────┴─────────────┘
                      │
              OFFLINE CORE LOOP
         (must ship → partially exposed)
```

**Accept:** offline Genesis will be pirated; saves will be edited.  
**Invest:** platform distribution, server authority for money, server for LLM/secrets, legal + repo hygiene for engine IP.

---

## 6. What not to do (yet)

| Avoid | Reason |
|-------|--------|
| Custom kernel anti-cheat for SP | Cost, friction, no benefit for offline |
| Blockchain / on-chain AC | GDD rejects; doesn't stop piracy |
| Encrypting everything in client | Keys live in the binary |
| Delaying snapshot/service boundaries | Harder to add server authority later |
| Shipping HLA/TDB/ecology docs in retail builds | Accidental IP leak |

---

## 7. Backlog (prioritized)

| Priority | Item | When |
|----------|------|------|
| P0 | Document Tier 0–3 classification per Features module | Now |
| P0 | `IAetherCreditsAuthority` + save integrity interfaces (stubs) | Next engine touch |
| P0 | Release vs Dev defines (`#if DEVELOPMENT_BUILD`) for smoke/debug | Before external build |
| P1 | IL2CPP + symbol strip on release CI | First Steam/playtest |
| P1 | Communications proxy design (keys server-side) | Run 2 planning |
| P2 | Save signing (L2) if achievement integrity matters | Pre-1.0 |
| P2 | Server AC ledger + receipt validation | When IAP is real |
| P3 | Selective obfuscation on `Directors`, `Generation` | Late polish |
| P3 | Framework export legal + package boundary | Architecture 2.0 |

---

## 8. Architecture decisions to lock in

1. **Offline-first stays** — don't sacrifice it for security theater; put secrets server-side only where online is optional.
2. **Snapshots are the security perimeter** — client/server and AI boundaries use versioned DTOs, not managers.
3. **Money never trusts the client** — plan server authority before IAP, even if stubbed.
4. **Engine source ≠ game source** — asmdef/folder boundaries even in one repo.
5. **Platform stores are your DRM** — budget for Steam/console publishing, not homegrown copy protection.

---

## 9. Summary

| Concern | Primary mitigation |
|---------|-------------------|
| **Piracy** | Steam/console distribution, legal, release build hygiene |
| **Engine secrecy** | Tier 3 docs/tools never ship; IL2CPP for Tier 1; server for LLM/premium intelligence |
| **Save/cheat (SP)** | Accept for offline; sign saves only if achievements matter |
| **IAP / AC fraud** | Server-authoritative ledger + platform receipt validation |

---

*Last updated: August 2026 — planning pass aligned with World Engine disk audit.*
