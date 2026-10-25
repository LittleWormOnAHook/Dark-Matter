# Companions — Genesis Studio

**Status:** Stub (generated tables; notes pending)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Companions**  
Companion chassis, roster data, class loadouts, abilities, and AI behavior.

## Tabs

- [Companion Editor](#companion-editor)
- [Class Profiles](#class-profiles)
- [Loadouts & AI](#loadouts--ai)

## Companion Editor

**What it does:** Build the Invector chassis, author roster data, generate Companion / Echo / Recruit prefabs, and sync the catalog.

**Where:**
- Studio: `Genesis Studio / Companions / Companion Editor` (panel mode `EmbeddedCompanionEditor`)

## Class Profiles

**What it does:** Med-tech, salvage, logistics, comms slot limits and default abilities.

**Where:**
- Studio: `Genesis Studio / Companions / Class Profiles` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/CompanionClassProfiles` (`t:CompanionClassProfile`)
- Script: `Assets/_Project/Scripts/Companions/Abilities/CompanionClassProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `pioneerClass` | SkilledPioneerClass |  |  |  |
| Slot Limits | `weaponSlots` | int | 1 |  |  |
| Slot Limits | `deployableSlots` | int | 1 |  |  |
| Slot Limits | `buffSlots` | int | 1 |  |  |
| Slot Limits | `toolSlots` | int | 1 |  |  |
| Defaults (configure later) | `defaultWeaponAbilityId` | string |  |  |  |
| Defaults (configure later) | `defaultDeployableAbilityId` | string |  |  |  |
| Defaults (configure later) | `defaultBuffAbilityId` | string |  |  |  |
| Defaults (configure later) | `defaultToolAbilityId` | string |  |  |  |

## Loadouts & AI

**What it does:** Weapon / tool loadouts, abilities, follow behavior, and live scene companion AI.

**Where:**
- Studio: `Genesis Studio / Companions / Loadouts & AI` (panel mode `EmbeddedCompanionSystems`)

