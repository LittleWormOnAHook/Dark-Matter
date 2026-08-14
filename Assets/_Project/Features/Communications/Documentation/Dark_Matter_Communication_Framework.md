# Dark Matter Communication Framework

**Dark Matter: Genesis** — authoritative roadmap for the radio / crew communications system.

> Build the phone network before you teach anyone to talk.

Primary GDD: `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt`  
Architecture: [HLA v1.0](../../../Documentation/Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md) · [TDB](../../../Documentation/Architecture/Dark_Matter_Technical_Design_Bible.md) · [Disk Status](../../../Documentation/Architecture/World_Engine_Disk_Status.md)  
Engineering contract: [Dark_Matter_Framework_Engineering_Standard.md](Dark_Matter_Framework_Engineering_Standard.md)

**WoOS layer:** Communications is **Presentation** (HLA §2.8) — it delivers what Intelligence and Experience authorize. It is not the world's brain.

**Disk status (July 22, 2026):** This folder contains **Documentation + Data/Audio READMEs only**. No Runtime / UI / Adapters / Tests `.cs`. Phases below are the **build target**. Phase 8.1 and Phase 9+ LLM are **deferred**; next Comms work is rule-based internal radio (GDD B4 Run 2) after World Engine spine (Run 1).


## Vision

The first goal is **not** “talk to an AI.”

The first goal is:

1. Get a **radio transmission from one companion to the player**.
2. Then add more companions that can communicate with the player and each other (small talk / banter).

Everything related to communications lives under:

```
Assets/_Project/Features/Communications/
  Runtime/
  UI/
  Data/
  Audio/
  Editor/
  Tests/
  Documentation/
```

## Locked product rules

- Platforms: **PC first + consoles** (no mobile-as-target, no WebGL).
- Economy: **Aether Credits (AC) only**.
- GDD: communications attachment module / Probe Uplink; **Kairos** is a future callsign / hub identity, not an LLM dependency.
- AI / LLM never reads Unity managers directly — only **Game State snapshots** / WorldState (planned) / context packs.
- **Silence is Experience design** (HLA §2.7) — ExperienceDirector may schedule radio silence; Communications obeys density intents.

---

## Phase roadmap

### Phase -1 — Dark Matter Coding Standards

Engineering rules every AI coding session must follow.  
See [Dark_Matter_Framework_Engineering_Standard.md](Dark_Matter_Framework_Engineering_Standard.md).

### Phase 0 — Architecture (no gameplay)

- Feature folder structure
- Assembly definitions
- Namespaces
- Interfaces and enums
- Empty Data / Audio / UI placeholders

**No UI. No gameplay. No AI.**

### Phase 1 — Game State API (backbone)

**Status: implemented (Phase 1).**

Every gameplay system exposes **read-only** information via `Project.Features.GameState`.

| Build | Notes |
|-------|--------|
| `IGameStateProvider` | Per-domain adapters under `Features/GameState/Adapters/` |
| `GameStateService` | Aggregates providers; `GetSnapshot()` |
| `GameStateSnapshot` | Immutable root |
| Domain snapshots | Player, Inventory, Mission, Weather, Power, Colony, Research, Crew, Buildings |

- Snapshots contain **data only** — no business logic.
- Never reference managers from AI / communications code.
- Lives under `Assets/_Project/Features/GameState/`.
- Does **not** replace `GameSaveData` / `GameSaveSystem` — persistence stays in `Project.Core`.
- Bootstrap: `GameStateBootstrap` via `CompanionSystemsBootstrap.EnsureGameplaySystems`.

Acceptance: `GameStateService.GetSnapshot()` / `GameStateBootstrap.Service.GetSnapshot()` returns a complete read-only game state.

### Phase 2 — Crew Database

**Status: implemented (Phase 2).**

Crew are **data** first (ScriptableObjects), not MonoBehaviours.

`CrewMember` fields (authored): Name, Role, Callsign, Portrait, VoiceId, Personality, Availability, Biography, Traits/Skills, Relationship Values, Radio Frequency, Current Status / Job.

**Bridge model:** `CrewMember` is the communications identity layer mapped to existing pioneer/companion IDs (`SkilledPioneerRecord`, `NamedPioneerDefinition`) via `linkedPioneerId`.

`CrewDatabase` loads all crew (`Resources/Communications/CrewDatabase` or LoadAll). No AI.

Menu: **Dark Matter: Genesis → Communications → Sync Crew Database From Companions**  
(builds from existing `NamedPioneerDefinition` assets; keeps **Kairos**; removes placeholder Harper/Patel/Reyes/Morgan).

### Phase 3 — Communications Framework

**Status: implemented (Phase 3).**

Heart of the radio network:

- `CommunicationsManager` — start / end / queue / prioritize; fires `TransmissionStarted` / `TransmissionEnded`
- `Transmission` / `TransmissionQueue` / `TransmissionPriority` / `TransmissionType`
- Types: Incoming, Outgoing, Emergency, Mission, Companion, Ambient
- Emergency preempts lower-priority active traffic
- Bootstrap: `CommunicationsBootstrap` (F5 smoke call from Kael-9 / first crew)

Nothing AI yet. Radio HUD is Phase 4 (MVP shipped).

### Phase 4 — Radio UI

**Status: implemented (Phase 4 MVP).**

Simple HUD first:

- Name + portrait (`RadioHudUI` — lower-third panel on gameplay canvas)
- Subtitle text + priority label + thin duration fill
- Subscribes to `CommunicationsManager.TransmissionStarted` / `TransmissionEnded`
- Portrait resolved from `CrewMember` when the speaker is crew; otherwise placeholder hide
- Bootstrap: `CommunicationsBootstrap` ensures HUD after canvas exists; **F5** mission smoke, **F6** emergency preempt

Deferred polish (Phase 4.1 / Phase 8): waveform, radio static FX, PTT indicator, portrait animation.

Palette: `DarkMatterGenesisUiPalette` / `ShiftUiTheme`.

### Phase 5 — Context System

**Status: implemented (Phase 5).**

Bridge between gameplay and dialogue.

`ContextBuilder` reads **only** Game State snapshots and produces a structured `CommunicationsContextPack`:

- Mission / primary objective (quest id + progress ints)
- Inventory summary (occupied slots, distinct items, top stacks)
- Power (powered/total, avg fuel, critical flag)
- Environment (zones as biome stand-in, thermal/hazard labels, dominant threat)
- Companions (expedition trio + roster count)
- Player vitals ratios
- Capture time (`CapturedAtUtc`)

Smoke: **F7** logs a one-line context summary via `CommunicationsBootstrap` (no HUD, no enqueue).

Nothing talks. Only context.

### Phase 6 — Rule-Based Communications

**Status: implemented (Phase 6).**

Still **no** LLM.

Player asks Status / Mission / Weather / Inventory / Power / Research / Objectives via **Left Alt + 1..7**.  
`CommsQueryService` builds a context pack, `TemplateReplyGenerator` emits Ops-tone lines, replies enqueue on the radio HUD.

Speaker: **Colony Ops** (first non-AiCore crew, e.g. Kael-9) until Kairos is awake / advisory-unlocked (`CommsQueryService.KairosAdvisoryUnlocked`). Kairos is the prologue idle machine (GDD §A6) — not day-one channel traffic.

### Phase 7 — Dialogue Generator

**Status: implemented (Phase 7).**

Deterministic phrasing upgrade: same context data, crew-flavored lines via `DialogueGenerator` + `VoiceStyle` (Professional / Tactical / Clinical / Scout / WaryMachine). No LLM.

`VoiceStyleResolver` maps `CrewMember` role (and personality keywords). Kairos (`AiCore`) always uses **WaryMachine**, but only speaks on Alt+queries when `KairosAdvisoryUnlocked` is true.

### Phase 8 — Voice Pipeline

**Status: implemented (Phase 8 MVP — procedural voice + stub STT).**

Speech-to-text, push-to-talk, mic capture, procedural incoming voice, radio filters (high-pass, low-pass, static, beep, PTT click). Still no LLM.

- `Project.Features.Communications.Audio` assembly: `RadioAudioProfile`, `RadioDspChain`, `RadioTransmissionAudioPlayer`
- `IRadioVoiceSynthesizer` + `ProceduralRadioVoiceSynthesizer` (noise bursts timed to subtitle length)
- `IRadioSpeechRecognizer` + `StubRadioSpeechRecognizer` (`"Copy, Commander."`)
- `RadioPttController` — hold **V** (gamepad L3) for outgoing PTT
- `RadioHudUI` RX (Gold) / TX (Rich Fuchsia) channel pill + TX static bar
- Bootstrap: **F8** audio smoke; Alt+1..7 replies play through audio layer
- **Phase 8.1 (deferred):** import SimpleOfflineSTT + SimpleOfflineTTS → swap adapters via `LocalVoiceLLM` proxies (no queue/HUD rewrite)

### Phase 9 — LLM Integration

**Status: deferred** (optional later). Build internal template radio first (Phases 3–7).

```
IConversationProvider
  TemplateConversationProvider
  OpenAIConversationProvider
  LocalLLMConversationProvider
```

Same Game State + context; replaceable providers. Not required for offline play.

### Phase 10 — Memory System

Conversation, relationship, mission, crew, discovery, and event memory.

### Phase 11 — Living Crew

Schedules, friendships, stress, fatigue, trust, goals.

### Phase 12 — Living Episodes

Simulation-authored stories (arguments, illness, discoveries, equipment failure, celebrations).

### Phase 13 — Companion Conversations

Crew talk to each other, player, Kairos, Mission Control without always needing player input.

### Phase 14 — AI Expedition Companion

Natural-language teammate prompts over the same radio stack.

### Phase 15 — Full Dynamic Crew

Memory + relationships + schedules + radio + context + optional LLM — crew feel alive.

---

## Session / phase delivery status (disk-corrected July 22, 2026)

| Scope | Status on disk |
|-------|----------------|
| Phase -1 Engineering Standard | **Present** (this Documentation folder) |
| Phase 0 folder scaffold | **Partial** — Data/Audio READMEs only; no Runtime asmdef / interface stubs |
| Phases 1–8 (GameState through procedural audio) | **Designed — not on disk** |
| Phase 8.1 LocalVoiceLLM | **Deferred** |
| Phase 9+ LLM | **Deferred** |

## Explicit non-goals until later

- LLM packages (Phase 9+) — deferred; template radio first
- Phase 8.1 SimpleOffline STT/TTS
- Kairos idle machine / repair quest / cores / Resonance runtime (Intelligence layer, not Communications)
- ExperienceDirector silence / density scheduling (after World Engine spine)
- Treating prior “Phase X complete” ChatGPT notes as repo truth

## Architecture phases A–D (design complete; runtime blocked)

| Phase | Module | Status |
|-------|--------|--------|
| A0/A | HLA + TDB + audits | Docs present |
| B | WorldState | Not on disk — GDD B4 Run 1 |
| C | Directors | Not on disk — GDD B4 Run 1 |
| D | Validation | Checklist only — see Phase_D_Validation.md |

Communications Runtime is GDD B4 **Run 2** (after Run 1 spine).

See [Dark_Matter_Phase_D_Validation.md](../../../Documentation/Architecture/Dark_Matter_Phase_D_Validation.md) · [World_Engine_Disk_Status.md](../../../Documentation/Architecture/World_Engine_Disk_Status.md).

## Post–Phase 8 integration (HLA v1.0) — target

| Upstream layer | Communications role |
|----------------|---------------------|
| **Intelligence** | Receives transmission intents from Story/Event/Kairos directors |
| **Experience** | Schedules silence, radio density; may defer or request soft comms |
| **WorldState** | `ContextBuilder` enriches from story chapter, storm, Kairos flags |
| **Presentation** | Queue, HUD, audio — this feature |

## Manual test vision (Phase 7+ milestone)

Press a key (e.g. F5 / T): radio pops up → Ops/crew transmits one line → subtitle + portrait + static → done.  
That proves the network works **before** any LLM.

