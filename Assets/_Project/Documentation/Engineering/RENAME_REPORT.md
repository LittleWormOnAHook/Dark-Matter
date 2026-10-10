# Product rename report — The Overseer & UEA Credits

**Date:** 2026-10-08  
**Scope:** `Assets/_Project/`, `Assets/UI Toolkit/`, `.cursor/rules/`, `AGENTS.md`  
**Requested:** Aether-9 → **The Overseer**; Aether Credits / AC (economy) → **UEA Credits** (short **UEA** in tight UI)

## Summary

| Metric | Value |
|--------|------:|
| Files touched (rename scope, approximate) | **~75+** |
| Unity compile after edits | **0 console errors** (MCP `read_console`, errors only) |
| Git commit | **None** (per owner request) |

Primary canon updates: `GAME_DESIGN_DOCUMENT_5.0.txt` (A6 + economy), `.cursor/rules/dark-matter-genesis-{core,gdd,unity-hdrp}.mdc`, `AGENTS.md`, UITK Journal/Vendor/AcReward, runtime wallet/vendor/toast strings, GDD-aligned design docs and narrative packages.

## Player-facing changes (representative)

- **Currency:** HUD / journal / vendor / quest reward toasts → **UEA Credits** or **UEA**; save slot summary `UEA:` instead of `AC:`.
- **Mystery hub:** Design docs and scene display label `'Aether 9 '` → **`The Overseer`** (playable + legacy Genesis scenes).
- **GDD A6:** Retitled to **The Overseer (Kairos)** — player-facing **The Overseer**; in-fiction self-name **Kairos** unchanged.

## Intentional skips (vendor / unrelated)

| Area | Reason |
|------|--------|
| `Assets/Invector-3rdPersonController/` (and other vendor packages) | Third-party; not DM economy/lore |
| `DMICreatureBoneRetargeter.cs`, `DMICreatureDefinition.cs` | **Malbers AC** = animation controller bones, not currency |
| `DMICreatureAutoReskin.cs` | Bulk replace briefly broke triangle math local `ac`; **reverted** to `Vector3 ac = c - a` |
| `Assets/_Project/Documentation/Design/ArtReference/**` | Protected life sheets (no edits) |
| Unrelated **Aether** IP outside Echoes/Overseer context | Not inventoried; grep if new assets appear |

## Serialized / code identifiers kept (breaking if renamed)

Save and asset compatibility — **display updated; internal keys unchanged** unless noted.

| Identifier | Location | Notes |
|------------|----------|--------|
| `aetherCredits`, `AetherCredits` | `PioneerRosterManager`, `GameSaveData`, `UIManager`, snapshots | JSON save field `aetherCredits`; migrate with `[FormerlySerializedAs]` only in a dedicated pass |
| `GrantAetherCredits`, `AddAetherCredits`, `TrySpendAetherCredits`, `FormatAetherCredits*` | Loot, quests, roster | Method names; rename → wide diff + no player benefit |
| `QuestRewardType.Pi` | Quest rewards enum | Legacy enum value for UEA grants |
| `isAcInfused`, `acValue`, `acDropMin` / `acDropMax`, `acCost`, `acListPrice` | Items, enemies, pioneers | Serialized on assets |
| `DMUiToolkitAcReward`, `AcRewardPopup`, `acreward-*` UXML ids | UITK / uGUI legacy toast | Type/asset names; strings inside updated |
| `piWalletBalance` | Save migration | Still merged into credits on load |
| POI / prefab ids `aether9`, `Aether9*` | Scenes (if present) | **Display** `'The Overseer'` updated in scenes; GameObject/prefab **names** not bulk-renamed |

## Documentation / planning renames (non-code)

- Story plans: Aether-9 → The Overseer; planning ids e.g. `POI_Overseer_Shell`, `OverseerDialogue` (forward-looking labels in markdown only).
- Narrative HTML exports under `Design/Exports/` updated for consistency.
- `Documentation/INDEX.md` — **0 UEA Credits** lock wording.

## Owner decisions / follow-up

1. **Kairos vs The Overseer:** Canon now: **The Overseer** (player-facing product name) + **Kairos** (voice’s self-name). Confirm all new dialogue uses that split.
2. **Code rename pass (optional):** `UeaCredits` types, `GrantUeaCredits`, rename `QuestRewardType.Pi` → `Uea` with enum migration — schedule with save bump.
3. **Saves / Plastic:** Existing saves remain valid (`aetherCredits` field). No migration required for this string pass.
4. **Localization:** When l10n tables exist, add keys for `UEA Credits` / `The Overseer` instead of hardcoded English.
5. **ItemRegistry / loot assets:** Inspect infused items in Editor — headers now say **UEA Credits**; item **names** in registry not auto-renamed.
6. **Unity MCP refresh:** Initial `refresh_unity` hit 60s readiness timeout; subsequent `read_console` reported **0 errors**. If Editor was mid-domain-reload, run **Ctrl+R** once and re-check console before commit.

## Ambiguous strings (needs owner)

| String / topic | Question |
|----------------|----------|
| GDD still uses **Kairos** heavily in A6 body | Keep Kairos as in-fiction only, or replace all with The Overseer in narrative text? |
| Scene object still named **Aether 9** in hierarchy vs label **The Overseer** | Rename GameObjects in v1.6.5 for author clarity? |
| **UEA** acronym expansion in UI | First-use **UEA Credits (UEA)** in popups OK; tighten HUD to **UEA: 123** only? |
| `StarterAcGrant`, `StarterPioneerCatalog` | Rename to `StarterUeaGrant` in a later API pass? |
| Design doc `aetherCredits = 0` in prologue save spec | Rename variable in docs to `ueaCredits` for clarity? |

## Files not exhaustively listed

Bulk pass touched design markdown, GDD txt files, Features communications docs, combat handoff, editor tooltips, multiple Genesis scene versions, and UITK screens. For a full list before commit:

```powershell
git diff --name-only -- Assets/_Project Assets/UI Toolkit .cursor/rules AGENTS.md
```

---

*Generated by agent rename pass. Update this file if follow-up code migrations ship.*
