#if UNITY_EDITOR
namespace Project.EditorTools
{
    public enum DMToolsTier
    {
        Primary = 0,
        Seed = 1,
        Legacy = 2,
        Destructive = 3
    }

    public readonly struct DMToolsEntry
    {
        public readonly string MenuPath;
        public readonly string Description;
        public readonly DMToolsTier Tier;

        public DMToolsEntry(string menuPath, string description, DMToolsTier tier = DMToolsTier.Primary)
        {
            MenuPath = menuPath;
            Description = description;
            Tier = tier;
        }
    }

    public readonly struct DMToolsSection
    {
        public readonly string Title;
        public readonly string Hint;
        public readonly bool DefaultCollapsed;
        public readonly DMToolsEntry[] Entries;

        public DMToolsSection(string title, string hint, bool defaultCollapsed, params DMToolsEntry[] entries)
        {
            Title = title;
            Hint = hint;
            DefaultCollapsed = defaultCollapsed;
            Entries = entries ?? System.Array.Empty<DMToolsEntry>();
        }
    }

    /// <summary>
    /// Single list for the Tools Window. Menu paths stay on the existing [MenuItem] scripts.
    /// </summary>
    public static class DarkMatterGenesisToolsCatalog
    {
        public static readonly DMToolsSection[] Sections =
        {
            new DMToolsSection(
                "Profiles",
                "ScriptableObject tuning. Profile Save keeps SO edits after Play; Play Mode Saver is for scene objects.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.GenesisStudio, "Survival, walk/run/sprint, climb, dash, jetpack, map, ammo FX, companions, crafting embeds, UI, audio"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.KeepProfilesAfterPlay, "Toggle SO profile persistence when exiting Play")),
            new DMToolsSection(
                "Authoring",
                "Daily content editors. Redirect-only Item/Ammo/Recipe windows stay in the Unity menu, not here.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BlueprintCraftingManager, "Blueprints, equipment craft, pickups, registry, Item Data, Ammo, Crafting Item"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.CreatureManager, "Creature prefabs, brains, and encounter wiring"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.ResourceManager, "Mining / plant nodes, yields, tools, ItemData"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Quests + "Quest Creator", "Author quests with objectives and rewards"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Quests + "Quest Giver NPC", "Place a quest giver NPC in the open scene"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.EquipmentItemCreator, "Weapons and tools with held + pickup prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.WeaponPrefabCreator, "Weapon held/world prefabs with optional melee hitbox"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.GrenadeItemCreator, "Throwable consumable ItemData")),
            new DMToolsSection(
                "Player",
                "Player_v7 wiring only. Do not retune capsule, layers, or physics.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Climb + "Build Climb Blend Tree", "8-way Protofactor climb blend on the player animator"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Climb + "Spawn Climb Test Wall", "Place a Climbable test wall in front of Player_v7"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Jetpack + "Wire Player_v7 Prefab", "Wire jetpack on Player_v7 / Variant"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Jetpack + "Setup Selected Player For Jetpack", "Add jetpack stack to the selected player"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Player + "Add Systems Profile To Player_v7", "Add DMPlayerSystemsProfile toggles"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlayerPrefabCreator, "Create a new player visual variant from the Invector template"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlayerPrefabCreator + "Repair Player_v7 Prefab", "Repair visual wiring on Player_v7 — does not retune physics")),
            new DMToolsSection(
                "Companions & Pets",
                "Companion chassis and pet authoring. Invector player rebuilds stay under Legacy.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.CompanionPrefabTool, "Rebuild chassis, seed companion data, bake Echo / recruit prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PetManager, "Pet prefabs, definitions, melee/ranged combat"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Root + "Generate All Echo Prefabs", "Bake Echo companion prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Root + "Generate All Recruit Prefabs", "Bake recruit companion prefabs")),
            new DMToolsSection(
                "Combat",
                "Enemy and weapon prefab tools. One Enemy Prefab Creator entry.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.EnemyPrefabCreator, "Create or rebuild humanoid and generic enemy prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Place Test Enemy", "Place HumanoidEnemy_Invector in the scene"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Combat Test Dummy", "Place combat training dummy"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Update All Enemy Prefabs And Scene", "Apply loot and disintegration to enemies"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Add Combat Zone To Selection", "Add combat zone trigger to selected objects"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RepairAllHumanoidCombatPrefabs, "Full humanoid Invector repair: gameplay stack, damage receivers, ragdoll"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RepairCompanionEchoHumanoidAnims, "Player_v7 Jetpack controller + FreeWithStrafe on companions/echoes; refresh weapon slots"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Audit Humanoid Ragdoll Setup", "Check vRagdoll, bridge, and BodyPart bone layers"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.AddWeaponHitboxToSelectedPrefab, "Add WeaponHitbox + capsule collider to selected weapon prefab(s)"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RefreshAllWeaponHitboxes, "Rebuild hitboxes on all held + melee world prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BuildLaserBurnMarkPrefab, "Build the laser burn mark prefab")),
            new DMToolsSection(
                "World & Scene",
                "Primary scene is Dark Matter Genesis v1.6.3.1. Gaia terrain tools live under World → Gaia in the Unity menu.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlaceExposureStarterKit, "Create all 7 exposure zones in the currently open scene"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlaceExposureStarterKitInPioneer, "Create all 7 exposure zones in the playable v1.6.3.1 scene"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Scene + "Map System", "Wire map UI and providers"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Scene + "Sync Map To Terrain", "Sync map bounds, texture bake, and minimap span to terrain"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RefreshAllMapMarkers, "Fog reveal + live position on every MapMarker"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Scene + "Journal Input Shortcuts", "Wire J/I/M/K journal tab hotkeys"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Scene + "Reflection Probe", "Add an active realtime reflection probe"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BindGaiaTerrainScenes, "Bind DM Genesis Gaia terrain scenes to build settings"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BindPlayerTerrainLoader, "Wire Player_v7 to Gaia Terrain Loader Manager"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BakeTlmImpostors, "Bake TLM impostor scenes for the 16 DM Genesis tiles"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.EditorFourTerrainsAndImpostors, "Editor streaming: 4 terrains around player + impostors"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.CreateTerrainContentScenes, "Create per-tile terrain content additive scenes"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.StitchCenterTileSplatEdges, "Blend center 2x2 tile splat edges after neighbor copy"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RemoveStamperTerrainLayers, "Strip today's Gaia stamper splat layers and renormalize"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RebuildSplatmapsFromHeight, "Rebuild splatmaps from height like Gaia"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.StripTreesFromAllTerrains, "Remove tree instances from all DM Genesis terrains"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.SetPixelError25OnAllTerrains, "Set terrain pixel error to 25 on all tiles"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BakeBorderFence, "Bake world border + tile seam fence meshes"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.SetupWalkerDrillInScene, "Wire Walker Drill interactable in the open scene")),
            new DMToolsSection(
                "UI",
                "UITK first. uGUI bootstrap and layout resets live under Legacy.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ui + "UI Studio", "Browse panels, preview sandbox, edit layout profiles"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RebuildHotCrossIconRegistry, "Rebuild Hot Cross cutout icon registry"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RewireItemAndRecipeIcons, "Rewire ItemData and recipe icons"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ui + "Create Controls Menu Content", "Author the controls menu content")),
            new DMToolsSection(
                "Audio & Optics",
                "Game audio profile plus binoculars / scanner overlay library (not the UITK weapon reticle).",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Audio + "Create Game Audio Profile", "Create audio profile asset"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Audio + "Open Game Audio Profile", "Select Resources/GameAudioProfile"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Audio + "Create Ambient Audio Zone", "Place an ambient zone in the scene"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Optics + "Setup Crosshair Library", "Wire TooManyCrosshairs textures into OpticsCrosshairLibrary"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Optics + "Select Crosshair Library", "Ping OpticsCrosshairLibrary in the Project window")),
            new DMToolsSection(
                "Content seeds",
                "Run-once seeders. Safe to hide after they have already been applied.",
                true,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Content + "Create Starting ItemData Assets", "Seed starter items", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Content + "Create Progression Curve", "XP required per player level", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Content + "Create Starter Skills + Registry", "Seed skill definitions and SkillRegistry", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Content + "Create Starter Achievements", "Seed achievement definitions", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Content + "Create Enemy Registry", "Seed enemy definitions into EnemyRegistry", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Setup Phase C Ranged Crafting", "Seed Phase C ranged crafting content", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Crafting + "Seed Starter Blueprints", "Create starter blueprint assets and registry entries", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Crafting + "Wire Scene Stations", "Wire Cooking, Workbench, and blueprint pickups", DMToolsTier.Seed),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ppt + "Phase 1 - Wire Pioneer Guide + Sample Registry", "PPT Phase 1 sample wiring", DMToolsTier.Seed)),
            new DMToolsSection(
                "Debug (Play Mode)",
                "Play-mode test hooks.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.DebugToggleSulfurCrisisHud, "Toggle environmental crisis HUD overlay"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.DebugShowEchoRescueReveal, "Preview echo rescue reveal popup"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.DebugSpawnTestEchoSignal, "Spawn a test echo signal in the world"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.DebugRefreshExpeditionTrio, "Refresh expedition trio companion spawns")),
            new DMToolsSection(
                "Play Mode Saver",
                "Scene / object edits. Distinct from Keep Profiles After Play.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlayModeSaverWindow, "One-click save for Play Mode scene edits"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlayModeSaverSaveNow, "Capture live edits (Play Mode only)"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PlayModeSaverSaveAndExit, "Capture edits and exit Play Mode")),
            new DMToolsSection(
                "Maintenance",
                "Repair and diagnostics. Strip NavMesh is defensive only.",
                false,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Maintenance + "Fix Tag Manager", "Remove duplicate/built-in tags"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Maintenance + "Repair Cursor MCP Connection", "Reconnect Cursor MCP bridge and HTTP server"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Maintenance + "Persist Play Mode Edits", "Auto-capture all scene edits when Play Mode stops"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Maintenance + "Repair PlayerInput Action Events", "Remove stale UI/orphan action events"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Maintenance + "Prune Unused ItemData Fields", "Strip unused ItemData serialized fields"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.TextureStreamingPreview, "Log how many world textures would opt into mip streaming"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.TextureStreamingApply, "Enable mip streaming on world/environment textures"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Strip NavMesh From Combat Prefabs", "Remove NavMeshAgent from combat prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Project + "Refresh Folder Colors", "Reapply _Project folder tints"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.AuditConsole, "Count and export Unity console errors/warnings"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.AuditResourcesPaths, "Audit Resources folder load paths")),
            new DMToolsSection(
                "HDRP",
                "Vendor material audit and URP→HDRP conversion. Maintenance only — not daily authoring.",
                true,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Audit Vendor Materials (write report)", "Scan vendor packs and write HDRP_Vendor_Material_Audit.md"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Convert Folder URP→HDRP (Dry Run)...", "Preview URP→HDRP conversion for a chosen folder"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Convert Folder URP→HDRP (Apply)...", "Apply URP→HDRP conversion for a chosen folder"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Convert Scene-Referenced Particles→HDRP", "Convert scene particle materials to HDRP"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Inventory _Project Material Shaders", "List _Project material shader usage"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Convert _Project Materials URP→HDRP", "Bulk-convert _Project materials to HDRP Lit"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Disable Incompatible GPU Instancing (Dry Run)", "Find third-party HDRP mats with bad instancing flags"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Disable Incompatible GPU Instancing (Apply)", "Disable incompatible GPU instancing on vendor materials"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Audit Project Materials (Prefabs + Open Scene)", "Scan Assets/_Project prefabs and open scene for shared material slots"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Check Hierarchy Materials (Selection)", "Report shared .mat slots on selected hierarchy roots"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Add Material Instancer To All _Project Prefabs", "Batch-add runtime instancer component to mesh prefab roots"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Remove Material Instancer From All _Project Prefabs (Undo Batch)", "Strip batch-added DMRuntimeHierarchyMaterialInstancer from _Project prefabs"),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Hdrp + "Ensure Material Instancers In Open Scene", "Add instancer to scene objects with mesh/skinned renderers")),
            new DMToolsSection(
                "Legacy",
                "Invector player migration, uGUI bootstrap, Pioneer backups, redirect menus. Hidden from daily use.",
                true,
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Build Player_Invector Prefab", "Hybrid Invector shooter + Pioneer systems", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Combat + "Swap Pioneer Scene Player To Invector", "Legacy Pioneer.unity swap — not the playable scene", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.PioneerCompanionInvectorPrefab, "Build PioneerCompanion_Invector from Player_Invector", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ui + "Full UI Canvas + Inventory", "Legacy uGUI canvas bootstrap", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ui + "Inventory Panel", "Legacy uGUI inventory shell", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ui + "UI Layout Editor (Legacy)", "Legacy layout editor window", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.Ui + "Reset Map & Loot UI To Default Layout", "Reset leftover uGUI map/loot/journal layouts", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.ItemDataCreator, "Redirect → Blueprint Manager Item Data tab", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.ProjectileAmmoCreator, "Redirect → Blueprint Manager Ammo tab", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.RecipePrefabCreator, "Redirect → Blueprint Manager Pickup Prefabs tab", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.BuildSulfurHoundCreature, "Legacy Sulfur Hound prefab bake", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.ValidateSulfurHoundSetup, "Legacy Sulfur Hound NavMesh + collider check", DMToolsTier.Legacy),
                new DMToolsEntry(DarkMatterGenesisEditorMenus.SyncGaiaTerrainNavMesh, "Legacy Gaia terrain NavMesh bake wiring — project is no-NavMesh", DMToolsTier.Legacy))
        };
    }
}
#endif
