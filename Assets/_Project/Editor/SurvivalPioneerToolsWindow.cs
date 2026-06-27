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

            DrawSection("Content", new[]
            {
                (SurvivalPioneerEditorMenus.Content + "Item Data Creator", "Create gatherable items and world prefabs"),
                (SurvivalPioneerEditorMenus.Content + "Create Starting ItemData Assets", "Seed starter items"),
                (SurvivalPioneerEditorMenus.Content + "Equipment Item Creator", "Create weapons, tools, and pickup prefabs"),
                (SurvivalPioneerEditorMenus.Content + "Add Building Control Panel to Selected", "Add building panel component to selection"),
            });

            DrawSection("Crafting", new[]
            {
                (SurvivalPioneerEditorMenus.Crafting + "Crafting Item Creator", "Create consumables and resources for recipes"),
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

            DrawSection("Combat", new[]
            {
                (SurvivalPioneerEditorMenus.Combat + "Enemy Prefab Creator", "Create enemy prefabs with AI and loot"),
                (SurvivalPioneerEditorMenus.Combat + "Place Test Enemy", "Place a test enemy in the scene"),
                (SurvivalPioneerEditorMenus.Combat + "Combat Test Dummy", "Place combat training dummy"),
                (SurvivalPioneerEditorMenus.Combat + "Update All Enemy Prefabs And Scene", "Apply loot and disintegration to enemies"),
                (SurvivalPioneerEditorMenus.Combat + "Weapon Prefab Creator", "Author weapon prefabs"),
                (SurvivalPioneerEditorMenus.Combat + "Create Two-Handed Weapon From Scene", "Bake two-handed weapon from selection"),
            });

            DrawSection("Combat Animations", new[]
            {
                (SurvivalPioneerEditorMenus.CombatAnimations + "Two-Handed Combat Animations", "Animator setup for two-handed weapons"),
                (SurvivalPioneerEditorMenus.CombatAnimations + "Setup Upper Body Combat Layer", "Upper-body attack layer on player animator"),
                (SurvivalPioneerEditorMenus.CombatAnimations + "Tune Upper Body Combat Blending", "Longer attack blend transitions"),
                (SurvivalPioneerEditorMenus.CombatAnimations + "Setup Player Strafe Locomotion", "Human Melee strafe clips on player"),
                (SurvivalPioneerEditorMenus.CombatAnimations + "Setup Enemy Strafe Locomotion", "Mixamo strafe blend trees on enemies"),
                (SurvivalPioneerEditorMenus.CombatAnimations + "Rebuild Gongo Controller", "Rebuild GongoController with Mixamo clips"),
            });

            DrawSection("Equipment", new[]
            {
                (SurvivalPioneerEditorMenus.Equipment + "Bake Sheathed Grip From Player Back", "Capture holstered grip from player back"),
                (SurvivalPioneerEditorMenus.Equipment + "Bake Sheathed Grip From Selected Transform", "Capture holstered grip from Spine child"),
                (SurvivalPioneerEditorMenus.Equipment + "Bake Sheathed Grip From Clipboard JSON", "Paste holstered offsets from clipboard"),
                (SurvivalPioneerEditorMenus.Equipment + "Bake Held Grip From Player Hand", "Capture held grip from player"),
                (SurvivalPioneerEditorMenus.Equipment + "Bake Held Grip From Selected Transform", "Capture grip from transform"),
                (SurvivalPioneerEditorMenus.Equipment + "Bake Held Grip From Clipboard JSON", "Paste grip offsets from clipboard"),
            });

            DrawSection("UI", new[]
            {
                (SurvivalPioneerEditorMenus.Ui + "Full UI Canvas + Inventory", "Bootstrap main canvas and inventory grid"),
                (SurvivalPioneerEditorMenus.Ui + "Inventory Panel", "Create inventory panel shell"),
                (SurvivalPioneerEditorMenus.Ui + "Inventory Slot Prefab", "Generate slot prefab"),
                (SurvivalPioneerEditorMenus.Ui + "UI Studio", "Browse panels, preview sandbox, edit layout profiles"),
                (SurvivalPioneerEditorMenus.Ui + "Create / Open UI Preview Scene", "Sandbox scene for UI Studio"),
                (SurvivalPioneerEditorMenus.Ui + "Fix Inventory Grid Layout", "Repair inventory grid spacing"),
                (SurvivalPioneerEditorMenus.Ui + "Setup Shift UI Theme", "Apply Shift UI theme assets"),
                (SurvivalPioneerEditorMenus.Ui + "Sanitize Layout Profiles", "Clean invalid layout profile entries"),
                (SurvivalPioneerEditorMenus.Ui + "Reset Map & Loot UI To Default Layout", "Reset map, loot, quest, and journal UI layouts"),
            });

            DrawSection("Scene", new[]
            {
                (SurvivalPioneerEditorMenus.Scene + "Map System", "Wire map UI and providers"),
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
            });

            DrawSection("Maintenance", new[]
            {
                (SurvivalPioneerEditorMenus.Maintenance + "Fix Tag Manager", "Remove duplicate/built-in tags (fixes Player already registered)"),
                (SurvivalPioneerEditorMenus.Maintenance + "Persist Play Mode Edits", "Keep scene edits made during Play Mode"),
                (SurvivalPioneerEditorMenus.Maintenance + "Fix Failed Editor Windows", "Close broken editor windows after Play mode"),
                (SurvivalPioneerEditorMenus.Maintenance + "Clear Stale Selection", "Fix null Inspector selection after prefab edits"),
                (SurvivalPioneerEditorMenus.Maintenance + "Reset Editor Layout", "Reset Unity editor window layout"),
                (SurvivalPioneerEditorMenus.Maintenance + "Fix AI Toolkit Import Loop", "Clear AI Toolkit temp GLBs and close Unity AI windows"),
                (SurvivalPioneerEditorMenus.Maintenance + "Clear AI Toolkit Temp Folder", "Delete AI Toolkit temp import files only"),
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
                    EditorApplication.ExecuteMenuItem(entry.menuPath);
                EditorGUILayout.LabelField(entry.description, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
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
