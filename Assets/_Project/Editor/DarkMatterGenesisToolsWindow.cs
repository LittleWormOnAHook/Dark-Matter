using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Central hub for Dark Matter Genesis editor utilities.
    /// </summary>
    public class DarkMatterGenesisToolsWindow : EditorWindow
    {
        private Vector2 scroll;

        [MenuItem(DarkMatterGenesisEditorMenus.ToolsWindow, false, 0)]
        public static void Open()
        {
            GetWindow<DarkMatterGenesisToolsWindow>("Dark Matter Genesis Tools");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Dark Matter Genesis", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor utilities grouped by category. Match the Tools Ã¢â€ â€™ Dark Matter Genesis menu.",
                MessageType.None);
            EditorGUILayout.Space(8f);

            DrawSection("Project", new[]
            {
                (DarkMatterGenesisEditorMenus.Project + "Project Structure", "Create core folder layout"),
                (DarkMatterGenesisEditorMenus.Project + "Organize Project Folders", "Move misplaced assets and remove orphan files"),
                (DarkMatterGenesisEditorMenus.Project + "Refresh Folder Colors", "Reapply _Project folder tints"),
            });

            DrawSection("Prefab Creator", new[]
            {
                (DarkMatterGenesisEditorMenus.ItemDataCreator, "Opens Blueprint Manager → Item Data tab (gatherables / consumables / throwables)"),
                (DarkMatterGenesisEditorMenus.CraftingItemCreator, "Opens Blueprint Manager → Crafting Item tab (ingredients / outputs)"),
                (DarkMatterGenesisEditorMenus.GrenadeItemCreator, "Throwable consumable ItemData (Throwables folder, identity-only)"),
                (DarkMatterGenesisEditorMenus.ResourceManager, "Resource Manager — mining / plant nodes, yields, tools, ItemData"),
                (DarkMatterGenesisEditorMenus.EquipmentItemCreator, "Weapons and tools with held + pickup prefabs"),
                (DarkMatterGenesisEditorMenus.EquipmentItemCreatorFromSelection, "Equipment creator pre-filled from selection"),
                (DarkMatterGenesisEditorMenus.WeaponPrefabCreator, "Weapon held/world prefabs with optional melee hitbox"),
                (DarkMatterGenesisEditorMenus.WeaponPrefabCreatorFromSelection, "Weapon creator pre-filled from selection"),
                (DarkMatterGenesisEditorMenus.ProjectileAmmoCreator, "Projectile + ammo ItemData and prefabs"),
                (DarkMatterGenesisEditorMenus.EnemyPrefabCreator, "Enemy prefabs with AI, animation, and loot"),
                (DarkMatterGenesisEditorMenus.TwoHandedWeaponFromScene, "Bake a two-handed weapon prefab from scene selection"),
                (DarkMatterGenesisEditorMenus.PetManager, "Pet Manager — prefabs, definitions, melee/ranged combat"),
                (DarkMatterGenesisEditorMenus.PetManagerFromSelection, "Pet Manager pre-filled from selection"),
                (DarkMatterGenesisEditorMenus.PetPrefabFoxCubDemo, "Fox cub pet prefab, definition, and icon"),
                (DarkMatterGenesisEditorMenus.ExposureZonePrefabCreator, "Exposure zone profile + trigger prefab with hazards and mitigation"),
                (DarkMatterGenesisEditorMenus.CameraShakeEmitterCreator, "Camera shake emitters: explosion, continuous, pulse"),
                (DarkMatterGenesisEditorMenus.CameraShakeEmitterCreateAllPresets, "Batch-create all camera shake emitter prefabs"),
                (DarkMatterGenesisEditorMenus.RecipePrefabCreator, "World blueprint scroll/book pickup prefabs (opens Blueprint Manager)"),
                (DarkMatterGenesisEditorMenus.InventorySlotPrefab, "Inventory slot UI prefab"),
            });

            DrawSection("Invector Companion", new[]
            {
                (DarkMatterGenesisEditorMenus.PioneerCompanionInvectorPrefab, "Build PioneerCompanion_Invector from Player_Invector + weapon slots"),
                (DarkMatterGenesisEditorMenus.CompanionPrefabTool, "Rebuild base chassis, seed/author companion data, sync registry, bake per-companion + Echo prefabs"),
            });

            DrawSection("Invector Player", new[]
            {
                (DarkMatterGenesisEditorMenus.Combat + "Build Player_Invector Prefab", "Hybrid Invector shooter + Pioneer systems"),
                (DarkMatterGenesisEditorMenus.Combat + "Wire ItemData Invector Weapon Prefabs", "Assign default pistol/rifle Invector prefabs"),
                (DarkMatterGenesisEditorMenus.Combat + "Swap Pioneer Scene Player To Invector", "Build prefab, wire items, swap Pioneer.unity player"),
                (DarkMatterGenesisEditorMenus.Combat + "Refresh Player_Invector Melee Slots", "Create melee weapon slots on Player_Invector"),
                (DarkMatterGenesisEditorMenus.Combat + "Refresh Player_Invector Ranged Slots", "Create Drawn_/Holstered_ pistol and rifle slots"),
                (DarkMatterGenesisEditorMenus.Combat + "Reset Player_Invector T-Pose & Weapon Slots", "Reset pose and clear weapon slot transforms"),
                (DarkMatterGenesisEditorMenus.OpenInvectorWeaponGripWindow, "Bake drawn/holstered weapon grips for Player_Invector"),
                (DarkMatterGenesisEditorMenus.BakeInvectorDrawnGrip, "Capture drawn hand offsets from live player (Play mode)"),
                (DarkMatterGenesisEditorMenus.BakeInvectorHolsteredGrip, "Capture holstered back offsets from live player (Play mode)"),
            });

            DrawSection("Equipment", new[]
            {
                (DarkMatterGenesisEditorMenus.PreviewInvectorHolsteredWeapon, "Attach selected weapon to holster socket for tuning"),
                (DarkMatterGenesisEditorMenus.EndInvectorHolsterPreview, "Return to normal drawn/holster weapon flow"),
                (DarkMatterGenesisEditorMenus.ResetInvectorWeaponGrips, "Reset held/sheathed offsets on selected ItemData"),
            });

            DrawSection("Content", new[]
            {
                (DarkMatterGenesisEditorMenus.Content + "Create Starting ItemData Assets", "Seed starter items"),
                (DarkMatterGenesisEditorMenus.Content + "Add Building Control Panel to Selected", "Add building panel component to selection"),
                (DarkMatterGenesisEditorMenus.Content + "Create Progression Curve", "XP required per player level"),
                (DarkMatterGenesisEditorMenus.Content + "Create Starter Skills + Registry", "Seed skill definitions and SkillRegistry"),
                (DarkMatterGenesisEditorMenus.Content + "Create Starter Achievements", "Seed achievement definitions and AchievementRegistry"),
                (DarkMatterGenesisEditorMenus.Content + "Create Enemy Registry", "Seed enemy definitions into EnemyRegistry"),
            });

            DrawSection("Crafting", new[]
            {
                (DarkMatterGenesisEditorMenus.BlueprintCraftingManager, "Primary: blueprints, equipment craft, pickups, registry, Item Data, Crafting Item"),
                (DarkMatterGenesisEditorMenus.Crafting + "Wire Scene Stations", "Wire Cooking, Workbench, and blueprint pickups"),
                (DarkMatterGenesisEditorMenus.Crafting + "Seed Starter Blueprints", "Create starter blueprint assets and registry entries"),
            });

            DrawSection("Quests", new[]
            {
                (DarkMatterGenesisEditorMenus.Quests + "Quest Creator", "Author quests with objectives and rewards"),
                (DarkMatterGenesisEditorMenus.Quests + "Quest Giver NPC", "Place a quest giver NPC in the open scene"),
            });

            DrawSection("Combat", new (string menuPath, string description)[]
            {
                (DarkMatterGenesisEditorMenus.EnemyPrefabCreator, "Create or rebuild humanoid and generic enemy prefabs"),
                (DarkMatterGenesisEditorMenus.Combat + "Setup Phase C Ranged Crafting", "Seed Phase C ranged crafting content"),
                (DarkMatterGenesisEditorMenus.Combat + "Place Test Enemy", "Place HumanoidEnemy_Invector in the scene"),
                (DarkMatterGenesisEditorMenus.Combat + "Combat Test Dummy", "Place combat training dummy"),
                (DarkMatterGenesisEditorMenus.Combat + "Update All Enemy Prefabs And Scene", "Apply loot and disintegration to enemies"),
                (DarkMatterGenesisEditorMenus.Combat + "Add Combat Zone To Selection", "Add combat zone trigger to selected objects"),
                (DarkMatterGenesisEditorMenus.Combat + "Strip NavMesh From Combat Prefabs", "Remove NavMeshAgent from combat prefabs"),
                (DarkMatterGenesisEditorMenus.RepairAllHumanoidCombatPrefabs, "Full humanoid Invector repair: gameplay stack, damage receivers, ragdoll"),
                (DarkMatterGenesisEditorMenus.Combat + "Audit Humanoid Ragdoll Setup", "Check vRagdoll, bridge, and BodyPart bone layers"),
                (DarkMatterGenesisEditorMenus.Combat + "Audit Selected Ragdoll Setup", "Audit ragdoll on selected prefab only"),
                (DarkMatterGenesisEditorMenus.Combat + "Rescale Oversized Ragdoll Colliders", "Shrink oversized ragdoll capsule colliders"),
                (DarkMatterGenesisEditorMenus.AddWeaponHitboxToSelectedPrefab, "Add WeaponHitbox + capsule collider to selected weapon prefab(s)"),
                (DarkMatterGenesisEditorMenus.RefreshAllWeaponHitboxes, "Rebuild hitboxes on all held + melee world prefabs"),
            });

            DrawSection("Enemy Animations", new[]
            {
                (DarkMatterGenesisEditorMenus.CombatAnimations + "Setup Enemy Strafe Locomotion", "Mixamo strafe blend trees on enemies"),
                (DarkMatterGenesisEditorMenus.CombatAnimations + "Rebuild Gongo Controller", "Rebuild GongoController with Mixamo clips"),
                (DarkMatterGenesisEditorMenus.RebuildEnemyControllerFromShooterMelee, "Rebuild selected enemy controller from ShooterMelee base"),
            });

            DrawSection("UI", new[]
            {
                (DarkMatterGenesisEditorMenus.Ui + "Full UI Canvas + Inventory", "Bootstrap main canvas and inventory grid"),
                (DarkMatterGenesisEditorMenus.Ui + "Inventory Panel", "Create inventory panel shell"),
                (DarkMatterGenesisEditorMenus.Ui + "UI Studio", "Browse panels, preview sandbox, edit layout profiles"),
                (DarkMatterGenesisEditorMenus.Ui + "UI Layout Editor (Legacy)", "Legacy layout editor window"),
                (DarkMatterGenesisEditorMenus.Ui + "Create / Open UI Preview Scene", "Sandbox scene for UI Studio"),
                (DarkMatterGenesisEditorMenus.Ui + "Fix Inventory Grid Layout", "Repair inventory grid spacing"),
                (DarkMatterGenesisEditorMenus.Ui + "Setup Shift UI Theme", "Apply Shift UI theme assets"),
                (DarkMatterGenesisEditorMenus.Ui + "Sanitize Layout Profiles", "Clean invalid layout profile entries"),
                (DarkMatterGenesisEditorMenus.Ui + "Reset Map UI To Default Layout", "Reset minimap and full map layouts"),
                (DarkMatterGenesisEditorMenus.Ui + "Reset Enemy Loot Dialog UI To Default Layout", "Reset enemy loot dialog layout"),
                (DarkMatterGenesisEditorMenus.Ui + "Reset Quest Giver Dialog UI To Default Layout", "Reset quest giver dialog layout"),
                (DarkMatterGenesisEditorMenus.Ui + "Reset Journal UI To Default Layout", "Reset journal overlay, tab rail, and window host"),
                (DarkMatterGenesisEditorMenus.Ui + "Reset Map & Loot UI To Default Layout", "Reset map, loot, quest, and journal UI layouts"),
            });

            DrawSection("Scene", new[]
            {
                (DarkMatterGenesisEditorMenus.PlaceExposureStarterKitInPioneer, "Create all 7 exposure zone prefabs and place in Pioneer.unity"),
                (DarkMatterGenesisEditorMenus.PlaceExposureStarterKit, "Create all 7 exposure zones in the currently open scene"),
                (DarkMatterGenesisEditorMenus.Scene + "Map System", "Wire map UI and providers"),
                (DarkMatterGenesisEditorMenus.Scene + "Sync Map To Terrain", "Sync map bounds, texture bake, and minimap span to terrain"),
                (DarkMatterGenesisEditorMenus.Scene + "Journal Input Shortcuts", "Wire J/I/M/K journal tab hotkeys"),
                (DarkMatterGenesisEditorMenus.Scene + "Reflection Probe", "Add an active realtime reflection probe"),
            });

            DrawSection("Audio", new[]
            {
                (DarkMatterGenesisEditorMenus.Audio + "Create Game Audio Profile", "Create audio profile asset"),
                (DarkMatterGenesisEditorMenus.Audio + "Open Game Audio Profile", "Select resources audio profile"),
                (DarkMatterGenesisEditorMenus.Audio + "Create Ambient Audio Zone", "Place ambient zone in scene"),
            });

            DrawSection("Optics", new[]
            {
                (DarkMatterGenesisEditorMenus.Optics + "Setup Crosshair Library", "Wire TooManyCrosshairs textures"),
                (DarkMatterGenesisEditorMenus.Optics + "Select Crosshair Library", "Ping OpticsCrosshairLibrary asset in Project"),
            });

            DrawSection("Debug (Play Mode)", new[]
            {
                ("Tools/Dark Matter Genesis/Debug/Toggle Sulfur Crisis HUD", "Toggle environmental crisis HUD overlay"),
                ("Tools/Dark Matter Genesis/Debug/Show Echo Rescue Reveal (Test)", "Preview echo rescue reveal popup"),
                ("Tools/Dark Matter Genesis/Debug/Spawn Test Echo Signal", "Spawn a test echo signal in the world"),
                ("Tools/Dark Matter Genesis/Debug/Refresh Expedition Trio Companions", "Refresh expedition trio companion spawns"),
            });

            DrawSection("Play Mode Saver", new[]
            {
                (DarkMatterGenesisEditorMenus.PlayModeSaverWindow, "One-click save for Play Mode edits"),
                (DarkMatterGenesisEditorMenus.PlayModeSaverSaveNow, "Capture live edits (Play Mode only)"),
                (DarkMatterGenesisEditorMenus.PlayModeSaverSaveAndExit, "Capture edits and exit Play Mode"),
            });

            DrawSection("Maintenance", new[]
            {
                (DarkMatterGenesisEditorMenus.Maintenance + "Fix Tag Manager", "Remove duplicate/built-in tags (fixes Player already registered)"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Repair Cursor MCP Connection", "Reconnect Cursor MCP bridge and HTTP server"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Repair Cursor MCP Connection (Silent)", "Silent MCP reconnect without dialogs"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Persist Play Mode Edits", "Auto-capture all scene edits when Play Mode stops"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Fix AI Toolkit Import Loop", "Clear AI Toolkit temp GLBs and close Unity AI windows"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Clear AI Toolkit Temp Folder", "Delete AI Toolkit temp import files only"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Configure Platform Quality Tiers", "Set PC/console quality tier defaults"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Audit _Project Resources Size", "Report large assets under Resources"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Repair PlayerInput Action Events", "Remove stale UI/orphan action events from Player prefabs and scene"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Repair PlayerInput + Sync Player Map", "Repair PlayerInput and sync action maps"),
                (DarkMatterGenesisEditorMenus.Maintenance + "Restore Player Animators After Play", "Restore missing legacy Player animator controllers"),
            });

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSection(string title, (string menuPath, string description)[] entries)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            for (int i = 0; i < entries.Length; i++)
            {
                (string menuPath, string description) entry = entries[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(GetButtonLabel(entry.menuPath), GUILayout.Width(260f)))
                    InvokeToolEntry(entry.menuPath);
                EditorGUILayout.LabelField(entry.description, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void InvokeToolEntry(string menuPath)
        {
            if (menuPath == DarkMatterGenesisEditorMenus.AddWeaponHitboxToSelectedPrefab)
            {
                WeaponPrefabBuilder.AddHitboxToSelectedPrefab();
                return;
            }

            if (menuPath == DarkMatterGenesisEditorMenus.RefreshAllWeaponHitboxes)
            {
                WeaponPrefabBuilder.RefreshAllWeaponHitboxes();
                return;
            }

            EditorApplication.ExecuteMenuItem(menuPath);
        }

        private static string GetButtonLabel(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return string.Empty;

            int lastSlash = menuPath.LastIndexOf('/');
            return lastSlash >= 0 ? menuPath.Substring(lastSlash + 1) : menuPath;
        }
    }
}
