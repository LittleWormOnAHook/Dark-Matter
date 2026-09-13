#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    public enum DMStudioPanelMode
    {
        SingletonAsset = 0,
        AssetFolder = 1,
        EmbeddedItemData = 2,
        EmbeddedAmmo = 3,
        EmbeddedCraftingItem = 4,
        ExternalBlueprintTab = 5,
        ExternalTool = 6,
        PlayerSystemsLink = 7,
        FootstepsCombined = 8,
        EmbeddedCompanionEditor = 9,
        EmbeddedCompanionSystems = 10
    }

    public readonly struct DMStudioSubtab
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Description;
        public readonly DMStudioPanelMode Mode;
        public readonly string AssetPath;
        public readonly string SearchFolder;
        public readonly string TypeFilter;
        public readonly string ExternalMenuPath;
        public readonly bool PlayModeSave;
        public readonly DMStudioProfileSectionFilter SectionFilter;

        public DMStudioSubtab(
            string id,
            string label,
            string description,
            DMStudioPanelMode mode,
            string assetPath = null,
            string searchFolder = null,
            string typeFilter = null,
            string externalMenuPath = null,
            bool playModeSave = false,
            DMStudioProfileSectionFilter sectionFilter = DMStudioProfileSectionFilter.None)
        {
            Id = id;
            Label = label;
            Description = description;
            Mode = mode;
            AssetPath = assetPath;
            SearchFolder = searchFolder;
            TypeFilter = typeFilter;
            ExternalMenuPath = externalMenuPath;
            PlayModeSave = playModeSave;
            SectionFilter = sectionFilter;
        }
    }

    public readonly struct DMStudioCategory
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Icon;
        public readonly string Description;
        public readonly Color Accent;
        public readonly DMStudioSubtab[] Subtabs;

        public DMStudioCategory(string id, string label, string icon, string description, Color accent, DMStudioSubtab[] subtabs)
        {
            Id = id;
            Label = label;
            Icon = icon;
            Description = description;
            Accent = accent;
            Subtabs = subtabs ?? System.Array.Empty<DMStudioSubtab>();
        }
    }

    /// <summary>
    /// Canonical Genesis Studio navigation: categories, subtabs, asset paths, and play-mode-save roots.
    /// </summary>
    public static class DMStudioRegistry
    {
        private static DMStudioCategory[] categories;

        public static IReadOnlyList<DMStudioCategory> Categories
        {
            get
            {
                if (categories == null)
                    categories = BuildCategories();
                return categories;
            }
        }

        public static string[] GetPlayModeSaveRoots()
        {
            var roots = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < Categories.Count; c++)
            {
                DMStudioSubtab[] subtabs = Categories[c].Subtabs;
                for (int s = 0; s < subtabs.Length; s++)
                {
                    DMStudioSubtab sub = subtabs[s];
                    if (!sub.PlayModeSave)
                        continue;

                    if (!string.IsNullOrEmpty(sub.SearchFolder))
                        roots.Add(sub.SearchFolder);
                    else if (!string.IsNullOrEmpty(sub.AssetPath))
                    {
                        int slash = sub.AssetPath.LastIndexOf('/');
                        if (slash > 0)
                            roots.Add(sub.AssetPath.Substring(0, slash));
                    }
                }
            }

            roots.Add("Assets/_Project/Resources/Landing");
            roots.Add("Assets/_Project/Data/Companions");
            roots.Add("Assets/_Project/Resources/CompanionAbilities");
            roots.Add("Assets/_Project/Resources/CompanionClassProfiles");
            return new List<string>(roots).ToArray();
        }

        private static DMStudioCategory[] BuildCategories()
        {
            return new[]
            {
                new DMStudioCategory(
                    "survival",
                    "Survival",
                    "♡",
                    "Health, energy, oxygen, walk/run/sprint gaits, exposure caps, fall damage, and hazard zone profiles.",
                    FromHex("#D4A017"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "stats-pools",
                            "Stats & Pools",
                            "Max pools, drains, regen, exposure recovery, and fall-damage thresholds.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset",
                            playModeSave: true,
                            sectionFilter: DMStudioProfileSectionFilter.SurvivalOnly),
                        new DMStudioSubtab(
                            "locomotion-gaits",
                            "Walk / Run / Sprint",
                            "Slow walk default, Shift jog, and double-tap Shift sprint burst multipliers.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset",
                            playModeSave: true,
                            sectionFilter: DMStudioProfileSectionFilter.LocomotionOnly),
                        new DMStudioSubtab(
                            "exposure",
                            "Exposure Zones",
                            "Cold, heat, radiation, sulfur, and shelter hazard profiles.",
                            DMStudioPanelMode.AssetFolder,
                            searchFolder: "Assets/_Project/Data/Exposure",
                            typeFilter: "t:ExposureZoneProfile")
                    }),
                new DMStudioCategory(
                    "player",
                    "Player",
                    "◆",
                    "Climb, dash, jetpack, landing, footsteps, and live player wiring.",
                    FromHex("#C02E7A"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "climb",
                            "Climb",
                            "Wall attach, mantle, climb/sprint stamina, surface layers, and ClingSense.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset",
                            playModeSave: true,
                            sectionFilter: DMStudioProfileSectionFilter.ClimbOnly),
                        new DMStudioSubtab(
                            "dash",
                            "Dash",
                            "Double-tap slide, stamina cost, hologram, streaks, and smoke VFX.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset",
                            playModeSave: true,
                            sectionFilter: DMStudioProfileSectionFilter.DashOnly),
                        new DMStudioSubtab(
                            "jetpack",
                            "Jetpack",
                            "Boost, hover, and jetpack energy drain.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Features/Jetpack/Data/DMJetpackProfile.asset",
                            playModeSave: true),
                        new DMStudioSubtab(
                            "landing",
                            "Landing Clips",
                            "Hero land / roll animation clip sets.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Landing/DMLandingClips.asset",
                            playModeSave: true),
                        new DMStudioSubtab(
                            "footsteps",
                            "Footsteps",
                            "Look (marks, dust, tags, terrain 0-10) and audio clip libraries in one place.",
                            DMStudioPanelMode.FootstepsCombined,
                            "Assets/_Project/Resources/Player/DM_FootstepProfile.asset",
                            playModeSave: true),
                        new DMStudioSubtab(
                            "player-systems",
                            "Player Systems",
                            "Toggle DM modules on Player_v7 Variant (MonoBehaviour — not a profile asset).",
                            DMStudioPanelMode.PlayerSystemsLink)
                    }),
                new DMStudioCategory(
                    "world",
                    "World",
                    "◎",
                    "Map calibration and fog-of-war.",
                    FromHex("#4A4A5A"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "map",
                            "Map Calibration",
                            "Journal / minimap UV, zoom meters, fog-of-war.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Map/DMWorldMapCalibrationProfile.asset",
                            playModeSave: true),
                        new DMStudioSubtab(
                            "terrain-splats",
                            "Terrain Layer Render",
                            "Per splat: height blend on/off, parallax on select Io layers, normal/tile overrides.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/World/DM_TerrainSplatRenderProfile.asset",
                            playModeSave: true)
                    }),
                new DMStudioCategory(
                    "combat",
                    "Combat",
                    "✦",
                    "Ammo FX profiles and surface hit catalog.",
                    FromHex("#8F1E5E"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "ammo-fx",
                            "Ammo FX",
                            "Per-type projectile / impact profiles (ItemData + combat FX).",
                            DMStudioPanelMode.AssetFolder,
                            searchFolder: "Assets/_Project/Data/Items/Ammo",
                            typeFilter: "t:DMAmmoFxProfile",
                            playModeSave: true,
                            sectionFilter: DMStudioProfileSectionFilter.CombatAmmoOnly),
                        new DMStudioSubtab(
                            "hit-catalog",
                            "Hit Catalog",
                            "Surface tag → hit mark mapping for ranged combat.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Data/Items/Ammo/DMAmmoFxCatalog.asset")
                    }),
                new DMStudioCategory(
                    "companions",
                    "Companions",
                    "◇",
                    "Companion chassis, roster data, class loadouts, abilities, and AI behavior.",
                    FromHex("#4A4A5A"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "companion-editor",
                            "Companion Editor",
                            "Build the Invector chassis, author roster data, generate Companion / Echo / Recruit prefabs, and sync the catalog.",
                            DMStudioPanelMode.EmbeddedCompanionEditor),
                        new DMStudioSubtab(
                            "class-profiles",
                            "Class Profiles",
                            "Med-tech, salvage, logistics, comms slot limits and default abilities.",
                            DMStudioPanelMode.AssetFolder,
                            searchFolder: "Assets/_Project/Resources/CompanionClassProfiles",
                            typeFilter: "t:CompanionClassProfile",
                            playModeSave: true),
                        new DMStudioSubtab(
                            "companion-systems",
                            "Loadouts & AI",
                            "Weapon / tool loadouts, abilities, follow behavior, and live scene companion AI.",
                            DMStudioPanelMode.EmbeddedCompanionSystems,
                            playModeSave: true)
                    }),
                new DMStudioCategory(
                    "crafting",
                    "Crafting",
                    "⚙",
                    "Blueprints, items, ammo authoring — embed creators or open full manager.",
                    FromHex("#C02E7A"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "blueprints",
                            "Blueprints",
                            "Recipe definitions, ingredients, and outputs.",
                            DMStudioPanelMode.ExternalBlueprintTab,
                            externalMenuPath: DarkMatterGenesisEditorMenus.BlueprintCraftingManager),
                        new DMStudioSubtab(
                            "item-data",
                            "Item Data",
                            "Gatherables, consumables, throwables.",
                            DMStudioPanelMode.EmbeddedItemData),
                        new DMStudioSubtab(
                            "ammo-creator",
                            "Ammo Creator",
                            "New ammo type + DMAmmoFxProfile + prefabs.",
                            DMStudioPanelMode.EmbeddedAmmo),
                        new DMStudioSubtab(
                            "crafting-item",
                            "Crafting Item",
                            "Ingredients and craft outputs.",
                            DMStudioPanelMode.EmbeddedCraftingItem),
                        new DMStudioSubtab(
                            "registry",
                            "Blueprint Registry",
                            "Sync Resources/Crafting/BlueprintRegistry.",
                            DMStudioPanelMode.ExternalBlueprintTab,
                            externalMenuPath: DarkMatterGenesisEditorMenus.BlueprintCraftingManager)
                    }),
                new DMStudioCategory(
                    "ui",
                    "UI",
                    "▣",
                    "Scanner optics and layout tooling.",
                    FromHex("#EDE9E4"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "scanner",
                            "Scanner Highlight",
                            "Optics scan outline / highlight profile.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/Scanner/ScannerHighlightProfile.asset"),
                        new DMStudioSubtab(
                            "ui-studio",
                            "UI Studio",
                            "Browse panels, preview sandbox, layout profiles.",
                            DMStudioPanelMode.ExternalTool,
                            externalMenuPath: DarkMatterGenesisEditorMenus.Ui + "UI Studio")
                    }),
                new DMStudioCategory(
                    "audio",
                    "Audio",
                    "♫",
                    "Global music, SFX, and footstep libraries.",
                    FromHex("#8C7F75"),
                    new[]
                    {
                        new DMStudioSubtab(
                            "game-audio",
                            "Game Audio",
                            "Resources/GameAudioProfile — music, combat, UI clips.",
                            DMStudioPanelMode.SingletonAsset,
                            "Assets/_Project/Resources/GameAudioProfile.asset")
                    })
            };
        }

        private static Color FromHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }
}
#endif
