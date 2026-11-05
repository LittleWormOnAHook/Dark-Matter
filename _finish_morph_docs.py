from pathlib import Path

p = Path(r"Assets/_Project/Editor/GenesisStudio/DMStudioRegistry.cs")
t = p.read_text(encoding="utf-8")
if "weapon-morph" in t:
    print("studio already")
else:
    bak = Path(str(p) + ".pre20261027a")
    if not bak.exists():
        bak.write_text(t, encoding="utf-8", newline="\n")
    insert = '''                        new DMStudioSubtab(
                            "weapon-morph",
                            "Weapon Morph Prototypes",
                            "DM_WeaponMorphStudioSettings + per-form DM_WeaponMorphProfile (Morph Gun Pistol/Rifle/Sniper, Morph Blade Metal/Light/Plasma/Ion/Laser/Cryo). Subtle/Default/Extreme presets. Build via Combat/Build Morph Weapon Prototypes.",
                            DMStudioPanelMode.AssetFolder,
                            searchFolder: "Assets/_Project/Resources/Combat/Morph",
                            typeFilter: "t:DM_WeaponMorphStudioSettings t:DM_WeaponMorphProfile",
                            playModeSave: true),
'''
    marker = '                            "scope-overlay",'
    idx = t.find(marker)
    if idx < 0:
        raise SystemExit("scope-overlay marker missing")
    start = t.rfind("new DMStudioSubtab(", 0, idx)
    t = t[:start] + insert + t[start:]
    p.write_text(t, encoding="utf-8", newline="\n")
    print("studio registry OK")

# CHANGELOG
cl = Path("CHANGELOG_20261021.txt")
ct = cl.read_text(encoding="utf-8", errors="replace")
if "2026-10-27a" not in ct and "Morph Gun" not in ct[:5000]:
    entry = """=== 2026-10-27a  Morph weapon prototypes (backups .pre20261027a) ===
Prototype Morph Gun (Pistol/Rifle/Sniper) + Morph Blade (Metal/Light/Plasma/Ion/Laser/Cryo) as world pickups under Prefabs/Weapons/Prototypes.
Existing weapons untouched; player/enemy/FORGE3D prefabs not edited. Y cycles forms while the morph weapon is drawn (other weapons keep Hot Cross Y).
Scripts/Prototypes/Weapons: DM_WeaponMorphProfile, DM_WeaponMorphStudioSettings (Subtle/Default/Extreme), DMWeaponMorphController, DMWeaponMorphRuntime.
Editor: Combat/Build Morph Weapon Prototypes. Studio Combat tab: Weapon Morph Prototypes.
Hooks: GameplayKeyboardShortcuts / EquipmentController / MeleeCombatController (Y cycle), PioneerShooterMeleeInput (block fire while morphing), DMScopeMode (Sniper as rifle grip), DMRangedAmmoStats + ApplyItemStats (form stats), DMPlayerBodyWeight (form bodyWeightScale).
Docs: DevManual/03-Combat.md.
Docs: Assets/_Project/Documentation/DevManual/03-Combat.md

"""
    cl.write_text(entry + ct, encoding="utf-8", newline="\n")
    print("changelog OK")
else:
    print("changelog skip")

# Dev manual
doc = Path(r"Assets/_Project/Documentation/DevManual/03-Combat.md")
dt = doc.read_text(encoding="utf-8", errors="replace")
if "Morph Gun" not in dt:
    bak = Path(str(doc) + ".pre20261027a")
    if not bak.exists():
        bak.write_text(dt, encoding="utf-8", newline="\n")
    section = """
## Morph weapon prototypes (2026-10-27a)

Player-only pickups under `Assets/_Project/Prefabs/Weapons/Prototypes/`:

- **MorphGun_Pickup** / held — forms **Pistol → Rifle → Sniper** (Y while drawn). Reuses pistol/rifle ItemData stats via `DM_WeaponMorphProfile.statsSource`. Sniper sets two-hand grip so existing `DMScopeMode` rifle scope path applies when Scope is armed.
- **MorphBlade_Pickup** / held — blade types **Metal, Light, Plasma, Ion, Laser, Cryo** (project combat-element family + solid Metal). Distinct primitive shapes/colours; hitbox rebuild follows blade length.

Authoring: `DM_WeaponMorphStudioSettings` + per-form profiles in `Resources/Combat/Morph/`. Studio → Combat → Weapon Morph Prototypes. Rebuild: **Tools → Dark Matter Genesis → Combat → Build Morph Weapon Prototypes**. Drop pickups in a scene yourself (do not bake into shared scenes). Fire is blocked during the ~0.3s morph. Body-weight scale is per form (`bodyWeightScale`).

"""
    # insert after first heading block if possible
    if "## " in dt:
        # append near top after intro
        idx = dt.find("\n## ")
        if idx > 0:
            dt = dt[:idx] + "\n" + section + dt[idx:]
        else:
            dt = dt + "\n" + section
    else:
        dt = dt + "\n" + section
    doc.write_text(dt, encoding="utf-8", newline="\n")
    print("devmanual OK")
else:
    print("devmanual skip")
