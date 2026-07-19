using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Central hub for Survival Pioneer editor utilities.
    /// </summary>
    public class SurvivalPioneerToolsWindow : EditorWindow
    {
        private Vector2 scroll;

        [MenuItem(SurvivalPioneerEditorMenus.ToolsWindow, false, 0)]
        public static void Open()
        {
            GetWindow<SurvivalPioneerToolsWindow>("Survival Pioneer Tools");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Survival Pioneer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor utilities grouped by category. Match the Tools → Survival Pioneer menu.",
                MessageType.None);
            EditorGUILayout.Space(8f);

            DrawSection("Project", new[]
            {
                (SurvivalPioneerEditorMenus.Project + "Project Structure", "Create core folder layout"),
                (SurvivalPioneerEditorMenus.Project + "Organize Project Folders", "Move misplaced assets and remove orphan files"),
                (SurvivalPioneerEditorMenus.Project + "Refresh Folder Colors", "Reapply _Project folder tints"),
            });

            DrawSection("Prefab Creator", new[]
            {
                (SurvivalPioneerEditorMenus.ItemDataCreator, "Gatherables: ItemData + ResourceNode world prefab"),
                (SurvivalPioneerEditorMenus.CraftingItemCreator, "Consumables/resources: ItemData + ItemPickup world prefab"),
                (SurvivalPioneerEditorMenus.EquipmentItemCreator, "Weapons and tools with held + pickup prefabs"),
                (SurvivalPioneerEditorMenus.EquipmentItemCreatorFromSelection, "Equipment creator pre-filled from selection"),
                (SurvivalPioneerEditorMenus.WeaponPrefabCreator, "Weapon held/world prefabs with optional melee hitbox"),
                (SurvivalPioneerEditorMenus.WeaponPrefabCreatorFromSelection, "Weapon creator pre-filled from selection"),
                (SurvivalPioneerEditorMenus.ProjectileAmmoCreator, "Projectile + ammo ItemData and prefabs"),
                (SurvivalPioneerEditorMenus.EnemyPrefabCreator, "Enemy prefabs with AI, animation, and loot"),
                (SurvivalPioneerEditorMenus.TwoHandedWeaponFromScene, "Bake a two-handed weapon prefab from scene selection"),
                (SurvivalPioneerEditorMenus.PetPrefabCreator, "Pet prefabs, definitions, and icons"),
                (SurvivalPioneerEditorMenus.PetPrefabCreatorFromSelection, "Pet creator pre-filled from selection"),
                (SurvivalPioneerEditorMenus.PetPrefabFoxCubDemo, "Fox cub pet prefab, definition, and icon"),
                (SurvivalPioneerEditorMenus.ExposureZonePrefabCreator, "Exposure zone profile + trigger prefab with hazards and mitigation"),
                (SurvivalPioneerEditorMenus.RecipePrefabCreator, "World recipe scroll/book pickup prefabs for existing recipes"),
                (SurvivalPioneerEditorMenus.InventorySlotPrefab, "Inventory slot UI prefab"),
            });

            DrawSection("Invector Companion", new[]
            {
                (SurvivalPioneerEditorMenus.PioneerCompanionInvectorPrefab, "Build PioneerCompanion_Invector from Player_Invector + weapon slots"),
                (SurvivalPioneerEditorMenus.CompanionPrefabTool, "Rebuild base chassis, seed/author companion data, sync registry, bake per-companion + Echo prefabs"),
            });

            DrawSection("Invector Player", new[]
            {
                (SurvivalPioneerEditorMenus.Combat + "Build Player_Invector Prefab", "Hybrid Invector shooter + Pioneer systems"),
                (SurvivalPioneerEditorMenus.Combat + "Wire ItemData Invector Weapon Prefabs", "Assign default pistol/rifle Invector prefabs"),
                (SurvivalPioneerEditorMenus.Combat + "Swap Pioneer Scene Player To Invector", "Build prefab, wire items, swap Pioneer.unity player"),
                (SurvivalPioneerEditorMenus.Combat + "Refresh Player_Invector Melee Slots", "Create melee weapon slots on Player_Invector"),
                (SurvivalPioneerEditorMenus.Combat + "Refresh Player_Invector Ranged Slots", "Create Drawn_/Holstered_ pistol and rifle slots"),
                (SurvivalPioneerEditorMenus.Combat + "Reset Player_Invector T-Pose & Weapon Slots", "Reset pose and clear weapon slot transforms"),
                (SurvivalPioneerEditorMenus.OpenInvectorWeaponGripWindow, "Bake drawn/holstered weapon grips for Player_Invector"),
                (SurvivalPioneerEditorMenus.BakeInvectorDrawnGrip, "Capture drawn hand offsets from live player (Play mode)"),
                (SurvivalPioneerEditorMenus.BakeInvectorHolsteredGrip, "Capture holstered back offsets from live player (Play mode)"),
            });

            DrawSection("Equipment", new[]
            {
                (SurvivalPioneerEditorMenus.PreviewInvectorHolsteredWeapon, "Attach selected weapon to holster socket for tuning"),
                (SurvivalPioneerEditorMenus.EndInvectorHolsterPreview, "Return to normal drawn/holster weapon flow"),
                (SurvivalPioneerEditorMenus.ResetInvectorWeaponGrips, "Reset held/sheathed offsets on selected ItemData"),
            });

            DrawSection("Content", new[]
            {
                (SurvivalPioneerEditorMenus.Content + "Create Starting ItemData Assets", "Seed starter items"),
                (SurvivalPioneerEditorMenus.Content + "Add Building Control Panel to Selected", "Add building panel component to selection"),
                (SurvivalPioneerEditorMenus.Content + "Create Progression Curve", "XP required per player level"),
                (SurvivalPioneerEditorMenus.Content + "Create Starter Skills + Registry", "Seed skill definitions and SkillRegistry"),
                (SurvivalPioneerEditorMenus.Content + "Create Starter Achievements", "Seed achievement definitions and AchievementRegistry"),
                (SurvivalPioneerEditorMenus.Content + "Create Enemy Registry", "Seed enemy definitions into EnemyRegistry"),
            });

            DrawSection("Crafting", new[]
            {
                (SurvivalPioneerEditorMenus.Crafting + "Craftable Equipment Recipe Creator", "Add a recipe for an existing weapon or tool"),
                (SurvivalPioneerEditorMenus.Crafting + "Recipe Creator", "Author, edit, and register recipes"),
                (SurvivalPioneerEditorMenus.Crafting + "Sync Recipe Icons From Output", "Copy output item icons onto recipe assets"),
                (SurvivalPioneerEditorMenus.Crafting + "Wire Scene Stations", "Wire Cooking, Workbench, and recipe pickups"),
                (SurvivalPioneerEditorMenus.Crafting + "Seed Starter Recipes", "Create starter recipe assets and registry entries"),
                (SurvivalPioneerEditorMenus.Crafting + "Sync Recipe Registry", "Refresh recipe registry from assets"),
            });

            DrawSection("Quests", new[]
            {
                (SurvivalPioneerEditorMenus.Quests + "Quest Creator", "Author quests with objectives and rewards"),
                (SurvivalPioneerEditorMenus.Quests + "Quest Giver NPC", "Place a quest giver NPC in the open scene"),
            });

            DrawSection("Combat", new (string menuPath, string description)[]
            {
                (SurvivalPioneerEditorMenus.EnemyPrefabCreator, "Create or rebuild humanoid and generic enemy prefabs"),
                (SurvivalPioneerEditorMenus.Combat + "Setup Phase C Ranged Crafting", "Seed Phase C ranged crafting content"),
                (SurvivalPioneerEditorMenus.Combat + "Place Test Enemy", "Place HumanoidEnemy_Invector in the scene"),
                (SurvivalPioneerEditorMenus.Combat + "Combat Test Dummy", "Place combat training dummy"),
                (SurvivalPioneerEditorMenus.Combat + "Update All Enemy Prefabs And Scene", "Apply loot and disintegration to enemies"),
                (SurvivalPioneerEditorMenus.Combat + "Add Combat Zone To Selection", "Add combat zone trigger to selected objects"),
                (SurvivalPioneerEditorMenus.Combat + "Strip NavMesh From Combat Prefabs", "Remove NavMeshAgent from combat prefabs"),
                (SurvivalPioneerEditorMenus.RepairAllHumanoidCombatPrefabs, "Full humanoid Invector repair: gameplay stack, damage receivers, ragdoll"),
                (SurvivalPioneerEditorMenus.Combat + "Audit Humanoid Ragdoll Setup", "Check vRagdoll, bridge, and BodyPart bone layers"),
                (SurvivalPioneerEditorMenus.Combat + "Audit Selected Ragdoll Setup", "Audit ragdoll on selected prefab only"),
                (SurvivalPioneerEditorMenus.Combat + "Rescale Oversized Ragdoll Colliders", "Shrink oversized ragdoll capsule colliders"),
                (SurvivalPioneerEditorMenus.AddWeaponHitboxToSelectedPrefab, "Add WeaponHitbox + capsule collider to selected weapon prefab(s)"),
                (SurvivalPioneerEditorMenus.RefreshAllWeaponHitboxes, "Rebuild hitboxes on all held + melee world prefabs"),
            });

            DrawSection("Enemy Animations", new[]
            {
                (SurvivalPioneerEditorMenus.CombatAnimations + "Setup Enemy Strafe Locomotion", "Mixamo strafe blend trees on enemies"),
                (SurvivalPioneerEditorMenus.CombatAnimations + "Rebuild Gongo Controller", "Rebuild GongoController with Mixamo clips"),
                (SurvivalPioneerEditorMenus.RebuildEnemyControllerFromShooterMelee, "Rebuild selected enemy controller from ShooterMelee base"),
            });

            DrawSection("UI", new[]
            {
                (SurvivalPioneerEditorMenus.Ui + "Full UI Canvas + Inventory", "Bootstrap main canvas and inventory grid"),
                (SurvivalPioneerEditorMenus.Ui + "Inventory Panel", "Create inventory panel shell"),
                (SurvivalPioneerEditorMenus.Ui + "UI Studio", "Browse panels, preview sandbox, edit layout profiles"),
                (SurvivalPioneerEditorMenus.Ui + "UI Layout Editor (Legacy)", "Legacy layout editor window"),
                (SurvivalPioneerEditorMenus.Ui + "Create / Open UI Preview Scene", "Sandbox scene for UI Studio"),
                (SurvivalPioneerEditorMenus.Ui + "Fix Inventory Grid Layout", "Repair inventory grid spacing"),
                (SurvivalPioneerEditorMenus.Ui + "Setup Shift UI Theme", "Apply Shift UI theme assets"),
                (SurvivalPioneerEditorMenus.Ui + "Sanitize Layout Profiles", "Clean invalid layout profile entries"),
                (SurvivalPioneerEditorMenus.Ui + "Reset Map UI To Default Layout", "Reset minimap and full map layouts"),
                (SurvivalPioneerEditorMenus.Ui + "Reset Enemy Loot Dialog UI To Default Layout", "Reset enemy loot dialog layout"),
                (SurvivalPioneerEditorMenus.Ui + "Reset Quest Giver Dialog UI To Default Layout", "Reset quest giver dialog layout"),
                (SurvivalPioneerEditorMenus.Ui + "Reset Journal UI To Default Layout", "Reset journal overlay, tab rail, and window host"),
                (SurvivalPioneerEditorMenus.Ui + "Reset Map & Loot UI To Default Layout", "Reset map, loot, quest, and journal UI layouts"),
            });

            DrawSection("Scene", new[]
            {
                (SurvivalPioneerEditorMenus.PlaceExposureStarterKitInPioneer, "Create all 7 exposure zone prefabs and place in Pioneer.unity"),
                (SurvivalPioneerEditorMenus.PlaceExposureStarterKit, "Create all 7 exposure zones in the currently open scene"),
                (SurvivalPioneerEditorMenus.Scene + "Map System", "Wire map UI and providers"),
                (SurvivalPioneerEditorMenus.Scene + "Sync Map To Terrain", "Sync map bounds, texture bake, and minimap span to terrain"),
                (SurvivalPioneerEditorMenus.Scene + "Journal Input Shortcuts", "Wire J/I/M/K journal tab hotkeys"),
                (SurvivalPioneerEditorMenus.Scene + "Reflection Probe", "Add an active realtime reflection probe"),
            });

            DrawSection("Audio", new[]
            {
                (SurvivalPioneerEditorMenus.Audio + "Create Game Audio Profile", "Create audio profile asset"),
                (SurvivalPioneerEditorMenus.Audio + "Open Game Audio Profile", "Select resources audio profile"),
                (SurvivalPioneerEditorMenus.Audio + "Create Ambient Audio Zone", "Place ambient zone in scene"),
            });

            DrawSection("Optics", new[]
            {
                (SurvivalPioneerEditorMenus.Optics + "Setup Crosshair Library", "Wire TooManyCrosshairs textures"),
                (SurvivalPioneerEditorMenus.Optics + "Select Crosshair Library", "Ping OpticsCrosshairLibrary asset in Project"),
            });

            DrawSection("Debug (Play Mode)", new[]
            {
                ("Tools/Survival Pioneer/Debug/Toggle Sulfur Crisis HUD", "Toggle environmental crisis HUD overlay"),
                ("Tools/Survival Pioneer/Debug/Show Echo Rescue Reveal (Test)", "Preview echo rescue reveal popup"),
                ("Tools/Survival Pioneer/Debug/Spawn Test Echo Signal", "Spawn a test echo signal in the world"),
                ("Tools/Survival Pioneer/Debug/Refresh Expedition Trio Companions", "Refresh expedition trio companion spawns"),
            });

            DrawSection("Play Mode Saver", new[]
            {
                (SurvivalPioneerEditorMenus.PlayModeSaverWindow, "One-click save for Play Mode edits"),
                (SurvivalPioneerEditorMenus.PlayModeSaverSaveNow, "Capture live edits (Play Mode only)"),
                (SurvivalPioneerEditorMenus.PlayModeSaverSaveAndExit, "Capture edits and exit Play Mode"),
            });

            DrawSection("Maintenance", new[]
            {
                (SurvivalPioneerEditorMenus.Maintenance + "Fix Tag Manager", "Remove duplicate/built-in tags (fixes Player already registered)"),
                (SurvivalPioneerEditorMenus.Maintenance + "Repair Cursor MCP Connection", "Reconnect Cursor MCP bridge and HTTP server"),
                (SurvivalPioneerEditorMenus.Maintenance + "Repair Cursor MCP Connection (Silent)", "Silent MCP reconnect without dialogs"),
                (SurvivalPioneerEditorMenus.Maintenance + "Persist Play Mode Edits", "Auto-capture all scene edits when Play Mode stops"),
                (SurvivalPioneerEditorMenus.Maintenance + "Fix Failed Editor Windows", "Close broken editor windows after Play mode"),
                (SurvivalPioneerEditorMenus.Maintenance + "Clear Stale Selection", "Fix null Inspector selection after prefab edits"),
                (SurvivalPioneerEditorMenus.Maintenance + "Reset Editor Layout", "Reset Unity editor window layout"),
                (SurvivalPioneerEditorMenus.Maintenance + "Fix AI Toolkit Import Loop", "Clear AI Toolkit temp GLBs and close Unity AI windows"),
                (SurvivalPioneerEditorMenus.Maintenance + "Clear AI Toolkit Temp Folder", "Delete AI Toolkit temp import files only"),
                (SurvivalPioneerEditorMenus.Maintenance + "Configure Platform Quality Tiers", "Set PC/console quality tier defaults"),
                (SurvivalPioneerEditorMenus.Maintenance + "Audit _Project Resources Size", "Report large assets under Resources"),
                (SurvivalPioneerEditorMenus.Maintenance + "Repair PlayerInput Action Events", "Remove stale UI/orphan action events from Player prefabs and scene"),
                (SurvivalPioneerEditorMenus.Maintenance + "Repair PlayerInput + Sync Player Map", "Repair PlayerInput and sync action maps"),
                (SurvivalPioneerEditorMenus.Maintenance + "Restore Player Animators After Play", "Restore missing legacy Player animator controllers"),
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
            if (menuPath == SurvivalPioneerEditorMenus.AddWeaponHitboxToSelectedPrefab)
            {
                WeaponPrefabBuilder.AddHitboxToSelectedPrefab();
                return;
            }

            if (menuPath == SurvivalPioneerEditorMenus.RefreshAllWeaponHitboxes)
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
