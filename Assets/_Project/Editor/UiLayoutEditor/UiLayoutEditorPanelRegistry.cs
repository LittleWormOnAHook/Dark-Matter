using System;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    internal sealed class UiPanelDefinition
    {
        public string PanelId;
        public string Label;
        public string Category;
        public string[] SearchNames;
        public string ParentSearchName;
        public string RelativePath;
        public Type ComponentType;
        public bool PlayModeOnly;
        public string PrefabAssetPath;
        public string Description;
    }

    internal static class UiLayoutEditorPanelRegistry
    {
        public const string InventorySlotPrefabPath = "Assets/_Project/Prefabs/UI/InventorySlot.prefab";

        public static readonly UiPanelDefinition[] Panels =
        {
            new UiPanelDefinition { Label = "Survival Stats", Category = "HUD", PanelId = UiPanelIds.SurvivalStats, SearchNames = new[] { "SurvivalStatsPanel", "CondensedSurvivalStatsHud" }, ComponentType = typeof(CondensedSurvivalStatsHud) },
            new UiPanelDefinition { Label = "Hotbar", Category = "HUD", PanelId = UiPanelIds.Hotbar, SearchNames = new[] { "Hotbar" } },
            new UiPanelDefinition { Label = "Toolbar", Category = "HUD", SearchNames = new[] { "ToolBar" }, ComponentType = typeof(ToolBarUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Pickup Aim Reticle", Category = "HUD", SearchNames = new[] { "PickupAimReticle" }, ComponentType = typeof(PickupAimReticleUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Pi Balance", Category = "HUD", SearchNames = new[] { "PiBalanceText" } },
            new UiPanelDefinition { Label = "Interaction Prompt", Category = "HUD", SearchNames = new[] { "InteractionPrompt" } },
            new UiPanelDefinition { Label = "Active Quest HUD", Category = "HUD", SearchNames = new[] { "ActiveQuestHud" }, ComponentType = typeof(ActiveQuestHudUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Pickup Proximity Dots", Category = "HUD", SearchNames = new[] { "PickupProximityDots" }, PlayModeOnly = true },

            new UiPanelDefinition
            {
                Label = "Minimap Panel",
                Category = "Map",
                PanelId = UiPanelIds.Minimap,
                SearchNames = new[] { "MinimapPanel" },
                ComponentType = typeof(MapUI),
                Description = "Top-right circular minimap shell. Use Create Map Shells if missing."
            },
            new UiPanelDefinition
            {
                Label = "Minimap Ring",
                Category = "Map",
                ParentSearchName = "MinimapPanel",
                SearchNames = new[] { "RingBorder" }
            },
            new UiPanelDefinition
            {
                Label = "Minimap Viewport",
                Category = "Map",
                ParentSearchName = "MinimapPanel",
                RelativePath = "CircleAssembly/CircularViewport"
            },
            new UiPanelDefinition
            {
                Label = "Minimap Map Image",
                Category = "Map",
                ParentSearchName = "MinimapPanel",
                RelativePath = "CircleAssembly/CircularViewport/MapContent/MapImage",
                Description = "RawImage showing the baked terrain texture."
            },
            new UiPanelDefinition
            {
                Label = "Full Map Overlay",
                Category = "Map",
                PanelId = UiPanelIds.MapFull,
                SearchNames = new[] { "FullMapOverlay" },
                ComponentType = typeof(MapUI),
                Description = "Standalone world map opened with M."
            },
            new UiPanelDefinition
            {
                Label = "Full Map Panel",
                Category = "Map",
                ParentSearchName = "FullMapOverlay",
                SearchNames = new[] { "FullMapPanel" }
            },
            new UiPanelDefinition
            {
                Label = "Full Map Viewport",
                Category = "Map",
                ParentSearchName = "FullMapPanel",
                RelativePath = "MapFrame/MapViewport"
            },
            new UiPanelDefinition
            {
                Label = "Full Map Image",
                Category = "Map",
                ParentSearchName = "FullMapPanel",
                RelativePath = "MapFrame/MapViewport/MapContent/MapImage"
            },

            new UiPanelDefinition { Label = "Inventory Panel", Category = "Panels", PanelId = UiPanelIds.InventoryPanel, SearchNames = new[] { "InventoryPanel" }, ComponentType = typeof(InventoryUI) },
            new UiPanelDefinition { Label = "Main Inventory Grid", Category = "Panels", SearchNames = new[] { "MainInventoryGrid" } },

            new UiPanelDefinition
            {
                Label = "Journal Shell",
                Category = "Journal",
                PanelId = UiPanelIds.JournalOverlay,
                SearchNames = new[] { "JournalOverlay", "JournalPanel" },
                ComponentType = typeof(JournalPanelUI),
                PlayModeOnly = true,
                Description = "Fullscreen journal overlay and navigator host on UIManager."
            },
            new UiPanelDefinition
            {
                Label = "Journal Tab Rail",
                Category = "Journal",
                PanelId = UiPanelIds.JournalTabRail,
                SearchNames = new[] { "JournalTabRailHost" },
                ComponentType = typeof(JournalTabRail),
                PlayModeOnly = true,
                Description = "Left vertical tab rail (J/I/M/K/P/C/R/T/L shortcuts)."
            },
            new UiPanelDefinition
            {
                Label = "Journal Window Host",
                Category = "Journal",
                PanelId = UiPanelIds.JournalWindowHost,
                SearchNames = new[] { "JournalWindowHost" },
                ComponentType = typeof(FullscreenUiNavigator),
                PlayModeOnly = true,
                Description = "Content area to the right of the tab rail; holds fullscreen journal windows."
            },
            new UiPanelDefinition
            {
                Label = "Journal Quest Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "JournalQuestWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Inventory Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "InventoryWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Map Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "MapWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Craft Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "CraftWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Pet Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "PetWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Pioneers Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "PioneersWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Recipes Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "RecipesWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Skills Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "SkillsWindowHost" },
                PlayModeOnly = true
            },
            new UiPanelDefinition
            {
                Label = "Journal Echoes Window",
                Category = "Journal",
                ParentSearchName = "JournalWindowHost",
                SearchNames = new[] { "EchoesWindowHost" },
                PlayModeOnly = true
            },

            new UiPanelDefinition { Label = "Crafting Window", Category = "Panels", PanelId = UiPanelIds.CraftPanel, SearchNames = new[] { "CraftingWindow", "CraftPanel" }, ComponentType = typeof(CraftingUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Quest Giver Dialog", Category = "Panels", SearchNames = new[] { "DialogPanel", "QuestGiverDialogUI" }, ComponentType = typeof(QuestGiverDialogUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Enemy Loot Dialog", Category = "Panels", PanelId = UiPanelIds.EnemyLootDialog, SearchNames = new[] { "LootOverlay", "EnemyLootDialogUI" }, ComponentType = typeof(EnemyLootDialogUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Echo Rescue Reveal", Category = "Panels", SearchNames = new[] { "EchoRescueRevealUI", "Neural Echo Rescued" }, ComponentType = typeof(EchoRescueRevealUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Starter Pioneer Select", Category = "Panels", SearchNames = new[] { "StarterPioneerSelectUI" }, ComponentType = typeof(StarterPioneerSelectUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Game Start Popup", Category = "Panels", SearchNames = new[] { "GameStartPopup" }, ComponentType = typeof(GameStartPopup), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Pioneer Roster Panel", Category = "Panels", SearchNames = new[] { "PioneerRosterPanelUI" }, ComponentType = typeof(PioneerRosterPanelUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Pickup Toast", Category = "Panels", SearchNames = new[] { "PickupToastUI" }, PlayModeOnly = true },
            new UiPanelDefinition { Label = "Pet Panel", Category = "Panels", PanelId = UiPanelIds.PetPanel, SearchNames = new[] { "PetPanel" }, ComponentType = typeof(PetUI), PlayModeOnly = true },
            new UiPanelDefinition { Label = "Main Menu", Category = "Panels", PanelId = UiPanelIds.MainMenu, SearchNames = new[] { "MainMenuPanel", "MainMenuBackground" }, ComponentType = typeof(MainMenuController), PlayModeOnly = true, Description = "Runtime-built main menu (MenuUiBuilder)." },
            new UiPanelDefinition { Label = "Building Control", Category = "Panels", PanelId = UiPanelIds.BuildingControl, SearchNames = new[] { "BuildingControlOverlay", "BuildingControlPanelUI" }, ComponentType = typeof(BuildingControlPanelUI), PlayModeOnly = true },

            new UiPanelDefinition { Label = "Item Hover Tooltip", Category = "Mini Windows", SearchNames = new[] { "ItemHoverTooltip" }, PlayModeOnly = true, Description = "Runtime tooltip shown when hovering inventory slots." },
            new UiPanelDefinition { Label = "Inventory Context Menu", Category = "Mini Windows", SearchNames = new[] { "InventoryContextMenu", "MenuPanel" }, PlayModeOnly = true },
            new UiPanelDefinition { Label = "Recipe Hover Tooltip", Category = "Mini Windows", SearchNames = new[] { "RecipeHoverTooltip", "RecipeTooltip" }, PlayModeOnly = true },
            new UiPanelDefinition { Label = "Optics Overlay", Category = "Mini Windows", SearchNames = new[] { "OpticsOverlay" }, PlayModeOnly = true },
            new UiPanelDefinition { Label = "Death Popup", Category = "Mini Windows", SearchNames = new[] { "DeathPopupPanel" }, PlayModeOnly = true },
            new UiPanelDefinition { Label = "Floating Damage", Category = "Mini Windows", SearchNames = new[] { "DamagePopup" }, PlayModeOnly = true },

            new UiPanelDefinition { Label = "UI Front Layer", Category = "Overlays", SearchNames = new[] { "UiFrontLayer" }, PlayModeOnly = true },
            new UiPanelDefinition
            {
                Label = "Inventory Slot Prefab",
                Category = "Prefabs",
                SearchNames = new[] { "InventorySlot", "InventorySlot_LayoutPreview" },
                PrefabAssetPath = InventorySlotPrefabPath
            },
        };

        public static bool HasPrefabAsset(UiPanelDefinition panel)
        {
            return !string.IsNullOrEmpty(panel.PrefabAssetPath)
                && AssetDatabase.LoadAssetAtPath<GameObject>(panel.PrefabAssetPath) != null;
        }

        public static RectTransform FindPanelRect(UiPanelDefinition panel, Canvas rootCanvas)
        {
            if (panel == null)
                return null;

            Transform searchRoot = rootCanvas != null ? rootCanvas.transform : null;
            if (!string.IsNullOrEmpty(panel.ParentSearchName))
            {
                RectTransform parentRect = FindDeepRect(searchRoot, panel.ParentSearchName);
                if (parentRect == null && panel.ComponentType != null)
                {
                    UnityEngine.Object[] components = UnityEngine.Object.FindObjectsByType(
                        panel.ComponentType,
                        FindObjectsInactive.Include);

                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] is not Component component)
                            continue;

                        parentRect = FindDeepRect(component.transform, panel.ParentSearchName);
                        if (parentRect != null)
                            break;
                    }
                }

                if (parentRect == null)
                    return null;

                searchRoot = parentRect;

                if (!string.IsNullOrEmpty(panel.RelativePath))
                    return FindRelativeRect(searchRoot, panel.RelativePath);

                if (panel.SearchNames != null)
                {
                    for (int i = 0; i < panel.SearchNames.Length; i++)
                    {
                        RectTransform child = FindScopedRect(searchRoot, panel.SearchNames[i]);
                        if (child != null)
                            return child;
                    }
                }

                return null;
            }

            if (panel.SearchNames == null)
                return null;

            for (int i = 0; i < panel.SearchNames.Length; i++)
            {
                RectTransform fromCanvas = FindDeepRect(searchRoot, panel.SearchNames[i]);
                if (fromCanvas != null)
                    return fromCanvas;
            }

            if (panel.ComponentType != null)
            {
                UnityEngine.Object[] components = UnityEngine.Object.FindObjectsByType(
                    panel.ComponentType,
                    FindObjectsInactive.Include);

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is not Component component)
                        continue;

                    for (int j = 0; j < panel.SearchNames.Length; j++)
                    {
                        RectTransform child = FindDeepRect(component.transform, panel.SearchNames[j]);
                        if (child != null)
                            return child;
                    }

                    if (component is RectTransform directRect)
                        return directRect;

                    RectTransform onSameObject = component.GetComponent<RectTransform>();
                    if (onSameObject != null)
                        return onSameObject;
                }
            }

            return null;
        }

        public static RectTransform FindDeepRect(Transform searchRoot, string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            Transform[] all = searchRoot != null
                ? searchRoot.GetComponentsInChildren<Transform>(true)
                : FindAllSceneTransforms();

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName && all[i] is RectTransform rect)
                    return rect;
            }

            return null;
        }

        private static Transform[] FindAllSceneTransforms()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            if (canvases.Length == 0)
                return Array.Empty<Transform>();

            System.Collections.Generic.List<Transform> transforms = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < canvases.Length; i++)
                transforms.AddRange(canvases[i].GetComponentsInChildren<Transform>(true));

            return transforms.ToArray();
        }

        public static bool PreparePanel(UiPanelDefinition panel)
        {
            RectTransform rect = FindPanelRect(panel, UnityEngine.Object.FindAnyObjectByType<Canvas>());
            if (rect != null && TryPrepareFromRect(rect))
                return true;

            switch (panel.Label)
            {
                case "Survival Stats":
                    return PrepareSurvivalStatsLayout();
                case "Hotbar":
                    return PrepareHotbarLayout();
                case "Toolbar":
                    return PrepareToolbarLayout();
                case "Pi Balance":
                case "Interaction Prompt":
                    return PrepareHudLabelLayout();
                case "Active Quest HUD":
                    return PrepareActiveQuestHudLayout();
                case "Minimap Panel":
                case "Full Map Overlay":
                case "Minimap Viewport":
                case "Full Map Panel":
                    return PrepareMapUiLayout();
                case "Inventory Panel":
                    return PrepareInventoryUiLayout();
                case "Inventory Slot Prefab":
                    return PrepareInventorySlotLayout();
                case "Main Inventory Grid":
                    return PrepareInventoryUiLayout() | PrepareInventorySlotLayout();
                default:
                    return TryPrepareKnownComponents(panel);
            }
        }

        public static bool TryPrepareFromRect(RectTransform rect)
        {
            if (rect == null)
                return false;

            int prepared = 0;
            MonoBehaviour[] behaviours = rect.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                prepared += SetSerializedBoolIfExists(behaviours[i], "applyRuntimeLayout", false) ? 1 : 0;
                prepared += SetSerializedBoolIfExists(behaviours[i], "applyRuntimeHudLayout", false) ? 1 : 0;
                prepared += SetSerializedBoolIfExists(behaviours[i], "preserveHotbarLayout", true) ? 1 : 0;
                prepared += SetSerializedBoolIfExists(behaviours[i], "preserveMainGridLayout", true) ? 1 : 0;
                prepared += SetSerializedBoolIfExists(behaviours[i], "preserveManualLayout", true) ? 1 : 0;
            }

            return prepared > 0;
        }

        private static bool TryPrepareKnownComponents(UiPanelDefinition panel)
        {
            if (panel.ComponentType == null)
                return false;

            UnityEngine.Object[] components = UnityEngine.Object.FindObjectsByType(
                panel.ComponentType,
                FindObjectsInactive.Include);

            bool prepared = false;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is MonoBehaviour behaviour)
                    prepared |= TryPrepareFromRect(behaviour.GetComponent<RectTransform>());
            }

            return prepared;
        }

        public static bool PrepareInventorySlotLayout()
        {
            bool prepared = false;

            GameObject selected = Selection.activeGameObject;
            InventorySlotUI selectedSlot = selected != null ? selected.GetComponentInParent<InventorySlotUI>() : null;
            if (selectedSlot != null)
                prepared |= SetSerializedBoolIfExists(selectedSlot, "preserveManualLayout", true);

            prepared |= SetSerializedBoolOnPrefabAsset(InventorySlotPrefabPath, "preserveManualLayout", true);
            return prepared;
        }

        public static bool SetSerializedBoolOnPrefabAsset(string prefabPath, string propertyName, bool value)
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
                return false;

            InventorySlotUI slot = prefabRoot.GetComponent<InventorySlotUI>();
            if (slot == null)
                return false;

            SerializedObject serialized = new SerializedObject(slot);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
                return false;

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefabRoot);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static bool SetSerializedBoolIfExists(UnityEngine.Object target, string propertyName, bool value)
        {
            if (target == null)
                return false;

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
                return false;

            Undo.RecordObject(target, "Prepare Manual UI Layout");
            property.boolValue = value;
            serialized.ApplyModifiedProperties();
            UiLayoutEditorInspectorDrawers.CommitChange(target, "Prepare Manual UI Layout");
            return true;
        }

        private static bool PrepareSurvivalStatsLayout()
        {
            CondensedSurvivalStatsHud statsHud = UnityEngine.Object.FindAnyObjectByType<CondensedSurvivalStatsHud>();
            if (statsHud == null)
            {
                RectTransform statsPanel = FindDeepRect(null, "SurvivalStatsPanel");
                if (statsPanel != null)
                    statsHud = statsPanel.GetComponent<CondensedSurvivalStatsHud>();
            }

            return statsHud != null && SetSerializedBoolIfExists(statsHud, "applyRuntimeLayout", false);
        }

        private static bool PrepareHotbarLayout()
        {
            InventoryUI inventoryUi = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
            return inventoryUi != null && SetSerializedBoolIfExists(inventoryUi, "preserveHotbarLayout", true);
        }

        private static bool PrepareInventoryUiLayout()
        {
            InventoryUI inventoryUi = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
            if (inventoryUi == null)
                return false;

            bool prepared = SetSerializedBoolIfExists(inventoryUi, "preserveMainGridLayout", true);
            prepared |= SetSerializedBoolIfExists(inventoryUi, "applyLayoutProfile", true);
            prepared |= SetSerializedBoolIfExists(inventoryUi, "skipDefaultLayoutWhenProfileApplied", true);
            return prepared;
        }

        private static bool PrepareToolbarLayout()
        {
            InventoryUI inventoryUi = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
            return inventoryUi != null && SetSerializedBoolIfExists(inventoryUi, "preserveHotbarLayout", true);
        }

        private static bool PrepareHudLabelLayout()
        {
            UIManager uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            return uiManager != null && SetSerializedBoolIfExists(uiManager, "applyRuntimeHudLayout", false);
        }

        public static RectTransform FindScopedRect(Transform searchRoot, string objectName)
        {
            if (searchRoot == null || string.IsNullOrEmpty(objectName))
                return null;

            Transform direct = searchRoot.Find(objectName);
            if (direct is RectTransform directRect)
                return directRect;

            for (int i = 0; i < searchRoot.childCount; i++)
            {
                Transform child = searchRoot.GetChild(i);
                if (child.name != objectName)
                    continue;

                if (child is RectTransform rect)
                    return rect;
            }

            Transform[] descendants = searchRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == objectName && descendants[i] is RectTransform rect)
                    return rect;
            }

            return null;
        }

        public static RectTransform FindRelativeRect(Transform searchRoot, string relativePath)
        {
            if (searchRoot == null || string.IsNullOrEmpty(relativePath))
                return null;

            Transform found = searchRoot.Find(relativePath);
            return found as RectTransform;
        }

        public static bool CreateMapLayoutShells(Canvas rootCanvas)
        {
            MapUI mapUi = rootCanvas != null
                ? rootCanvas.GetComponent<MapUI>()
                : null;
            mapUi ??= UnityEngine.Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
                return false;

            Undo.RegisterFullObjectHierarchyUndo(mapUi.gameObject, "Create Map Layout Shells");
            mapUi.EnsureLayoutShells();
            EditorUtility.SetDirty(mapUi);
            return true;
        }

        private static bool PrepareMapUiLayout()
        {
            MapUI mapUi = UnityEngine.Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
                return false;

            bool prepared = SetSerializedBoolIfExists(mapUi, "preserveManualLayout", true);
            prepared |= SetSerializedBoolIfExists(mapUi, "applyRuntimeLayout", false);
            return prepared;
        }

        private static bool PrepareActiveQuestHudLayout()
        {
            ActiveQuestHudUI questHud = UnityEngine.Object.FindAnyObjectByType<ActiveQuestHudUI>();
            return questHud != null && SetSerializedBoolIfExists(questHud, "applyRuntimeLayout", false);
        }
    }
}
