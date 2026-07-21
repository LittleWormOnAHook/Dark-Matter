# Dark Matter Communication Framework

**Dark Matter: Genesis** — authoritative roadmap for the radio / crew communications system.

> Build the phone network before you teach anyone to talk.

Primary GDD: `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt`  
Architecture: [HLA v1.0](../../../Documentation/Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md) · [TDB v1.0 §6 Presentation](../../../Documentation/Architecture/Dark_Matter_Technical_Design_Bible.md#6-presentation-layer-contracts)  
Engineering contract: [Dark_Matter_Framework_Engineering_Standard.md](Dark_Matter_Framework_Engineering_Standard.md)

**WoOS layer:** Communications is **Presentation** (HLA §2.8) — it delivers what Intelligence and Experience authorize. It is not the world's brain.

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
- GDD: communications attachment module / Probe Uplink; **Aether-9** is a future callsign / hub identity, not an LLM dependency.
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
(builds from existing `NamedPioneerDefinition` assets; keeps **Aether-9**; removes placeholder Harper/Patel/Reyes/Morgan).

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

Palette: `SurvivalPioneerUiPalette` / `ShiftUiTheme`.

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

Speaker: **Colony Ops** (first non-AiCore crew, e.g. Kael-9) until Aether-9 is awake / advisory-unlocked (`CommsQueryService.Aether9AdvisoryUnlocked`). Aether-9 is the prologue idle machine (GDD §A6) — not day-one channel traffic.

### Phase 7 — Dialogue Generator

**Status: implemented (Phase 7).**

Deterministic phrasing upgrade: same context data, crew-flavored lines via `DialogueGenerator` + `VoiceStyle` (Professional / Tactical / Clinical / Scout / WaryMachine). No LLM.

`VoiceStyleResolver` maps `CrewMember` role (and personality keywords). Aether-9 (`AiCore`) always uses **WaryMachine**, but only speaks on Alt+queries when `Aether9AdvisoryUnlocked` is true.

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

```
IConversationProvider
  TemplateConversationProvider
  OpenAIConversationProvider
  LocalLLMConversationProvider
```

Same Game State + context; replaceable providers.

### Phase 10 — Memory System

Conversation, relationship, mission, crew, discovery, and event memory.

### Phase 11 — Living Crew

Schedules, friendships, stress, fatigue, trust, goals.

### Phase 12 — Living Episodes

Simulation-authored stories (arguments, illness, discoveries, equipment failure, celebrations).

### Phase 13 — Companion Conversations

Crew talk to each other, player, Aether-9, Mission Control without always needing player input.

### Phase 14 — AI Expedition Companion

Natural-language teammate prompts over the same radio stack.

### Phase 15 — Full Dynamic Crew

Memory + relationships + schedules + radio + context + optional LLM — crew feel alive.

---

## Session 1 scope (complete)

Phases **-1** and **0** only: standards docs + feature scaffold + interface stubs.

## Phase 1 scope (complete)

Game State API under `Assets/_Project/Features/GameState/` with adapters + bootstrap.

## Phase 2 scope (complete)

CrewMember ScriptableObjects + CrewDatabase bridge to pioneer ids.

## Phase 3 scope (complete)

CommunicationsManager + TransmissionQueue + F5 smoke enqueue.

## Phase 4 scope (complete)

`RadioHudUI` lower-third panel (portrait / callsign / subtitle / priority / duration fill) wired to manager events; F5/F6 smoke via bootstrap.

## Phase 5 scope (complete)

`ContextBuilder` + `CommunicationsContextPack` from `GameStateSnapshot` only; F7 context smoke log.

## Phase 6 scope (complete)

Rule-based Alt+1..7 queries → template replies on Radio HUD; Ops speaker until Aether-9 advisory unlock; GDD §A6 Aether-9 prologue canon expanded.

## Phase 7 scope (complete)

`DialogueGenerator` + `VoiceStyle` / `VoiceStyleResolver` — same facts, crew-flavored wording; WaryMachine for Aether-9 when advisory unlocked.

## Phase 8 scope (complete)

Adapter-based radio audio: procedural incoming voice, stub PTT STT, DSP chain, RX/TX HUD chrome, F8 smoke. LocalVoiceLLM TTS/STT modules deferred to Phase 8.1.

## Explicit non-goals until later

- Game State providers / `GameStateService` (Phase 1 — done)
- `CrewMember` assets (Phase 2 — done)
- Live `CommunicationsManager` / queue playback (Phase 3 — done)
- Radio HUD MVP (Phase 4 — done); waveform / static / PTT / voice audio (Phase 8 — done, procedural MVP)
- Context pack (Phase 5 — done)
- Rule-based template replies (Phase 6 — done)
- Crew-flavored dialogue (Phase 7 — done)
- Aether-9 idle machine / repair quest / cores / Resonance runtime (dedicated prologue track — **Aether-9 Intelligence layer**, not Communications)
- LLM packages (Phase 9+)
- WorldState context enrichment (Phase B — **done**; `ContextBuilder` reads `WorldStateSnapshot`)
- ExperienceDirector silence / density scheduling (Phase C stub — full Experience module later)
- Director command wiring beyond Communications intent adapter (Quest/Weather stubs)

## Architecture phases A–D (complete — GDD B5)

| Phase | Module | Communications touchpoint |
|-------|--------|---------------------------|
| B | WorldState | `ContextWorldSummary` in context pack; F7 log enriched |
| C | Directors | `CommunicationsIntentServiceAdapter` enqueues transmissions |
| D | Validation | Cross-stack EditMode tests + smoke key registry |

See [Dark_Matter_Phase_D_Validation.md](../../../Documentation/Architecture/Dark_Matter_Phase_D_Validation.md).

## Post–Phase 8 integration (HLA v1.0)

| Upstream layer | Communications role |
|----------------|---------------------|
| **Intelligence** | Receives transmission intents from Story/Event/Aether-9 directors |
| **Experience** | Schedules silence, radio density; may defer or request soft comms |
| **WorldState** | `ContextBuilder` enriches from story chapter, storm, Aether-9 flags (Phase B) |
| **Presentation** | Queue, HUD, audio — this feature |

## Manual test vision (Phase 7+ milestone)

Press a key (e.g. F5 / T): radio pops up → Harper transmits one line → subtitle + portrait + static → done.  
That proves the network works **before** any LLM.
