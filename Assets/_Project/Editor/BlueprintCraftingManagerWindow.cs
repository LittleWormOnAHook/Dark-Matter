using System.Collections.Generic;
using Project.Crafting;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Primary editor entry for blueprints and crafting: author blueprints, craftable equipment,
    /// world pickup prefabs, registry maintenance, and ItemData / crafting item creators.
    /// </summary>
    public class BlueprintCraftingManagerWindow : EditorWindow
    {
        private enum ManagerTab
        {
            Blueprints = 0,
            Equipment = 1,
            Pickups = 2,
            Registry = 3,
            ItemData = 4,
            CraftingItem = 5
        }

        private static readonly string[] TabLabels =
        {
            "Blueprints",
            "Equipment Craft",
            "Pickup Prefabs",
            "Registry",
            "Item Data",
            "Crafting Item"
        };

        private ManagerTab tab = ManagerTab.Blueprints;
        private readonly ItemDataCreatorPanel itemDataPanel = new ItemDataCreatorPanel();
        private readonly CraftingItemCreatorPanel craftingItemPanel = new CraftingItemCreatorPanel();

        private RecipeDefinition[] blueprintAssets = System.Array.Empty<RecipeDefinition>();
        private ItemData[] itemOptions = System.Array.Empty<ItemData>();
        private int selectedBlueprintIndex = -1;

        private string blueprintId = "new_blueprint";
        private string displayName = "New Blueprint";
        private string description = string.Empty;
        private CraftingStationType stationType = CraftingStationType.Cooking;
        private ItemData outputItem;
        private int outputAmount = 1;
        private Sprite blueprintIcon;
        private List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        private string assetFileName = "new_blueprint";
        private bool addToRegistry = true;
        private int requiredPlayerLevel = 1;
        private int blueprintTier = 1;

        private GameObject pickupVisualTemplate;
        private float pickupInteractRange = 2.5f;
        private Vector3 pickupColliderSize = new Vector3(0.5f, 0.5f, 0.5f);
        private bool autoFitPickupCollider = true;
        private bool createPickupPrefabOnSave = true;

        private Vector2 listScroll;
        private Vector2 editorScroll;
        private Vector2 pickupListScroll;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;

        [MenuItem(DarkMatterGenesisEditorMenus.BlueprintCraftingManager, false, 10)]
        public static void Open()
        {
            OpenTab(ManagerTab.Blueprints);
        }

        public static void OpenBlueprintsTab() => OpenTab(ManagerTab.Blueprints);
        public static void OpenEquipmentTab() => OpenTab(ManagerTab.Equipment);
        public static void OpenPickupsTab() => OpenTab(ManagerTab.Pickups);
        public static void OpenRegistryTab() => OpenTab(ManagerTab.Registry);
        public static void OpenItemDataTab() => OpenTab(ManagerTab.ItemData);
        public static void OpenCraftingItemTab() => OpenTab(ManagerTab.CraftingItem);

        private static void OpenTab(ManagerTab targetTab)
        {
            BlueprintCraftingManagerWindow window = GetWindow<BlueprintCraftingManagerWindow>("Blueprint + Crafting");
            window.minSize = new Vector2(820f, 560f);
            window.tab = targetTab;
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshLists();
            if (pickupVisualTemplate == null)
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
        }

        private void RefreshLists()
        {
            blueprintAssets = CraftingEditorUtility.LoadAllRecipeAssets();
            itemOptions = CraftingEditorUtility.LoadAllItems();
            if (selectedBlueprintIndex >= blueprintAssets.Length)
                selectedBlueprintIndex = blueprintAssets.Length > 0 ? 0 : -1;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Blueprint and Crafting Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create and edit DM blueprints, link craftable equipment, build world pickup prefabs, " +
                "sync the blueprint registry, and author ItemData / crafting ingredients.",
                MessageType.Info);

            tab = (ManagerTab)GUILayout.Toolbar((int)tab, TabLabels);
            EditorGUILayout.Space(8f);

            switch (tab)
            {
                case ManagerTab.Blueprints:
                    DrawBlueprintsTab();
                    break;
                case ManagerTab.Equipment:
                    DrawEquipmentTab();
                    break;
                case ManagerTab.Pickups:
                    DrawPickupsTab();
                    break;
                case ManagerTab.Registry:
                    DrawRegistryTab();
                    break;
                case ManagerTab.ItemData:
                    itemDataPanel.Draw();
                    break;
                case ManagerTab.CraftingItem:
                    craftingItemPanel.Draw();
                    break;
            }

            if (tab != ManagerTab.ItemData && tab != ManagerTab.CraftingItem && !string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        private void SetStatus(string message, MessageType type = MessageType.Info)
        {
            statusMessage = message;
            statusType = type;
        }

        #region Blueprints tab

        private void DrawBlueprintsTab()
        {
            EditorGUILayout.BeginHorizontal();
            DrawBlueprintListPanel();
            DrawBlueprintEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBlueprintListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            EditorGUILayout.LabelField("Blueprints", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < blueprintAssets.Length; i++)
            {
                RecipeDefinition blueprint = blueprintAssets[i];
                if (blueprint == null)
                    continue;

                string label = string.IsNullOrEmpty(blueprint.displayName) ? blueprint.name : blueprint.displayName;
                bool selected = i == selectedBlueprintIndex;
                if (GUILayout.Toggle(selected, label, "Button"))
                {
                    if (selectedBlueprintIndex != i)
                        LoadBlueprint(blueprint, i);
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("New Blueprint", GUILayout.Height(28f)))
                StartNewBlueprint();

            if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                RefreshLists();

            EditorGUILayout.EndVertical();
        }

        private void DrawBlueprintEditorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

            DrawIdentityFields();
            EditorGUILayout.Space(8f);
            CraftingEditorUtility.DrawIngredientListEditor(ref ingredients, itemOptions);
            addToRegistry = EditorGUILayout.Toggle("Add To Blueprint Registry", addToRegistry);

            EditorGUILayout.Space(10f);
            DrawPickupPrefabSection();

            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Blueprint", GUILayout.Height(34f)))
                SaveCurrentBlueprint();

            using (new EditorGUI.DisabledScope(selectedBlueprintIndex < 0 || selectedBlueprintIndex >= blueprintAssets.Length))
            {
                if (GUILayout.Button("Delete Blueprint", GUILayout.Height(34f)))
                    DeleteSelectedBlueprint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Pickup Prefab", GUILayout.Height(34f)))
                SavePickupPrefab();

            if (GUILayout.Button("Place In Scene", GUILayout.Height(34f)))
                PlacePickupInScene();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawIdentityFields()
        {
            blueprintId = EditorGUILayout.TextField(
                new GUIContent("Blueprint Id", "Stable id used for discovery, saves, and XP keys. Prefer not to change after ship."),
                blueprintId);
            assetFileName = EditorGUILayout.TextField("Asset File Name", assetFileName);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(56f));
            stationType = (CraftingStationType)EditorGUILayout.EnumPopup("Station", stationType);
            outputItem = (ItemData)EditorGUILayout.ObjectField("Output Item", outputItem, typeof(ItemData), false);
            outputAmount = Mathf.Max(1, EditorGUILayout.IntField("Output Amount", outputAmount));
            requiredPlayerLevel = Mathf.Max(1, EditorGUILayout.IntField("Required Player Level", requiredPlayerLevel));
            blueprintTier = Mathf.Max(1, EditorGUILayout.IntField("Blueprint Tier", blueprintTier));

            EditorGUILayout.BeginHorizontal();
            blueprintIcon = (Sprite)EditorGUILayout.ObjectField("Blueprint Icon", blueprintIcon, typeof(Sprite), false);
            if (GUILayout.Button("Use Output", GUILayout.Width(90f)))
            {
                blueprintIcon = outputItem != null ? outputItem.icon : null;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            if (blueprintIcon != null)
            {
                Rect preview = GUILayoutUtility.GetRect(48f, 48f, GUILayout.Width(48f));
                EditorGUI.DrawPreviewTexture(preview, blueprintIcon.texture);
            }
        }

        private void DrawPickupPrefabSection()
        {
            EditorGUILayout.LabelField("Blueprint Pickup Prefab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Saves to {CraftingEditorUtility.CraftingPrefabsFolder}/BlueprintPickup_<id>.prefab (loads legacy RecipePickup_* if present).",
                MessageType.None);

            pickupVisualTemplate = (GameObject)EditorGUILayout.ObjectField(
                "Visual Template",
                pickupVisualTemplate,
                typeof(GameObject),
                false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Open Book", GUILayout.Width(120f)))
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
            if (GUILayout.Button("Use Crafting Book", GUILayout.Width(140f)))
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultCraftingBookVisual();
            if (GUILayout.Button("Use Selection", GUILayout.Width(110f)))
                UseSelectedVisualTemplate();
            EditorGUILayout.EndHorizontal();

            pickupInteractRange = EditorGUILayout.FloatField("Interact Range", pickupInteractRange);
            autoFitPickupCollider = EditorGUILayout.Toggle("Auto-fit Collider To Mesh", autoFitPickupCollider);
            using (new EditorGUI.DisabledScope(autoFitPickupCollider))
            {
                pickupColliderSize = EditorGUILayout.Vector3Field("Collider Size", pickupColliderSize);
            }

            createPickupPrefabOnSave = EditorGUILayout.Toggle("Create Pickup Prefab On Save", createPickupPrefabOnSave);
        }

        private void UseSelectedVisualTemplate()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Select a mesh or prefab in the Hierarchy or Project window.", "OK");
                return;
            }

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(selected);
            pickupVisualTemplate = source != null ? source : selected;
            Repaint();
        }

        private void StartNewBlueprint()
        {
            selectedBlueprintIndex = -1;
            blueprintId = "new_blueprint";
            assetFileName = "new_blueprint";
            displayName = "New Blueprint";
            description = string.Empty;
            stationType = CraftingStationType.Cooking;
            outputItem = null;
            outputAmount = 1;
            requiredPlayerLevel = 1;
            blueprintTier = 1;
            blueprintIcon = null;
            ingredients = new List<RecipeIngredient>();
            addToRegistry = true;
            createPickupPrefabOnSave = true;
            if (pickupVisualTemplate == null)
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
            Repaint();
        }

        private void LoadBlueprint(RecipeDefinition blueprint, int index)
        {
            selectedBlueprintIndex = index;
            blueprintId = blueprint.ResolvedId;
            assetFileName = blueprint.name;
            displayName = blueprint.displayName;
            description = blueprint.description;
            stationType = blueprint.stationType;
            outputItem = blueprint.outputItem;
            outputAmount = blueprint.outputAmount;
            requiredPlayerLevel = Mathf.Max(1, blueprint.requiredPlayerLevel);
            blueprintTier = Mathf.Max(1, blueprint.recipeTier);
            blueprintIcon = blueprint.icon;
            ingredients = blueprint.ingredients != null
                ? new List<RecipeIngredient>(blueprint.ingredients)
                : new List<RecipeIngredient>();
            Repaint();
        }

        private void SaveCurrentBlueprint()
        {
            if (string.IsNullOrWhiteSpace(blueprintId) || string.IsNullOrWhiteSpace(displayName))
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Blueprint id and display name are required.", "OK");
                return;
            }

            string safeFileName = CraftingEditorUtility.SanitizeAssetName(
                string.IsNullOrWhiteSpace(assetFileName) ? blueprintId : assetFileName);
            if (string.IsNullOrEmpty(safeFileName))
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Asset file name is invalid.", "OK");
                return;
            }

            RecipeDefinition draft = ScriptableObject.CreateInstance<RecipeDefinition>();
            draft.recipeId = blueprintId.Trim();
            draft.displayName = displayName.Trim();
            draft.description = description;
            draft.stationType = stationType;
            draft.outputItem = outputItem;
            draft.outputAmount = outputAmount;
            draft.requiredPlayerLevel = requiredPlayerLevel;
            draft.recipeTier = blueprintTier;
            draft.icon = blueprintIcon != null ? blueprintIcon : (outputItem != null ? outputItem.icon : null);
            draft.ingredients = new List<RecipeIngredient>(ingredients);

            RecipeDefinition saved = CraftingEditorUtility.SaveRecipeAsset(draft, safeFileName);
            if (saved == null)
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Failed to save blueprint.", "OK");
                return;
            }

            if (addToRegistry)
                CraftingEditorUtility.AddRecipeToRegistry(saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLists();

            for (int i = 0; i < blueprintAssets.Length; i++)
            {
                if (blueprintAssets[i] == saved)
                {
                    selectedBlueprintIndex = i;
                    break;
                }
            }

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            SetStatus($"Saved blueprint '{saved.displayName}'.", MessageType.Info);

            if (createPickupPrefabOnSave)
            {
                CraftingEditorUtility.CreateRecipePickupPrefab(
                    blueprintId.Trim(),
                    pickupVisualTemplate,
                    pickupInteractRange,
                    pickupColliderSize,
                    autoFitPickupCollider,
                    confirmOverwrite: false);
            }
        }

        private void DeleteSelectedBlueprint()
        {
            if (selectedBlueprintIndex < 0 || selectedBlueprintIndex >= blueprintAssets.Length)
                return;

            RecipeDefinition blueprint = blueprintAssets[selectedBlueprintIndex];
            if (blueprint == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "Blueprint + Crafting",
                    $"Delete blueprint asset '{blueprint.name}'?",
                    "Delete",
                    "Cancel"))
                return;

            string path = AssetDatabase.GetAssetPath(blueprint);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            RefreshLists();
            StartNewBlueprint();
            SetStatus($"Deleted '{path}'.", MessageType.Warning);
        }

        private void SavePickupPrefab(bool showSuccessDialog = true)
        {
            if (string.IsNullOrWhiteSpace(blueprintId))
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Set a blueprint id before saving a pickup prefab.", "OK");
                return;
            }

            GameObject prefab = CraftingEditorUtility.CreateRecipePickupPrefab(
                blueprintId.Trim(),
                pickupVisualTemplate,
                pickupInteractRange,
                pickupColliderSize,
                autoFitPickupCollider,
                confirmOverwrite: true);

            if (prefab == null)
                return;

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            SetStatus($"Saved pickup prefab to {AssetDatabase.GetAssetPath(prefab)}.", MessageType.Info);

            if (showSuccessDialog)
            {
                EditorUtility.DisplayDialog(
                    "Blueprint + Crafting",
                    $"Saved pickup prefab to\n{AssetDatabase.GetAssetPath(prefab)}",
                    "OK");
            }
        }

        private void PlacePickupInScene()
        {
            if (string.IsNullOrWhiteSpace(blueprintId))
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Set a blueprint id before placing a pickup.", "OK");
                return;
            }

            Transform parent = Selection.activeTransform;
            GameObject instance = CraftingEditorUtility.PlaceRecipePickupInScene(
                blueprintId.Trim(),
                pickupVisualTemplate,
                parent,
                pickupInteractRange,
                pickupColliderSize,
                autoFitPickupCollider,
                savePrefabIfMissing: true);

            if (instance == null)
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Could not place blueprint pickup.", "OK");
                return;
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            SetStatus($"Placed {instance.name} in scene.", MessageType.Info);
        }

        #endregion

        #region Equipment tab

        private void DrawEquipmentTab()
        {
            EditorGUILayout.HelpBox(
                "Attach a crafting blueprint to an existing weapon or tool ItemData. " +
                "Create the equipment first with Prefab Creator > Equipment Item Creator if needed.",
                MessageType.None);

            outputItem = (ItemData)EditorGUILayout.ObjectField("Output Equipment", outputItem, typeof(ItemData), false);

            if (outputItem != null && outputItem.itemType != ItemType.MeleeWeapon && outputItem.itemType != ItemType.Tool)
                EditorGUILayout.HelpBox("Output should be a melee weapon or tool.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected ItemData", GUILayout.Width(170f)))
                UseSelectedItemDataForEquipment();
            if (GUILayout.Button("Open Equipment Item Creator", GUILayout.Width(210f)))
                EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.EquipmentItemCreator);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            blueprintId = EditorGUILayout.TextField("Blueprint Id", blueprintId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(48f));
            stationType = (CraftingStationType)EditorGUILayout.EnumPopup("Station", stationType);
            outputAmount = Mathf.Max(1, EditorGUILayout.IntField("Output Amount", outputAmount));

            EditorGUILayout.Space(8f);
            CraftingEditorUtility.DrawIngredientListEditor(ref ingredients, itemOptions);
            addToRegistry = EditorGUILayout.Toggle("Add To Blueprint Registry", addToRegistry);

            EditorGUILayout.Space(16f);
            using (new EditorGUI.DisabledScope(outputItem == null || string.IsNullOrWhiteSpace(blueprintId) || string.IsNullOrWhiteSpace(displayName)))
            {
                if (GUILayout.Button("Create Equipment Blueprint", GUILayout.Height(42f)))
                    CreateEquipmentBlueprint();
            }
        }

        private void UseSelectedItemDataForEquipment()
        {
            ItemData selected = Selection.activeObject as ItemData;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Select an ItemData asset in the Project window.", "OK");
                return;
            }

            outputItem = selected;
            displayName = $"Craft {selected.itemName}";
            blueprintId = CraftingEditorUtility.SanitizeAssetName(selected.itemName).ToLowerInvariant();
            assetFileName = blueprintId;
            stationType = CraftingStationType.Workbench;
            Repaint();
        }

        private void CreateEquipmentBlueprint()
        {
            string safeFileName = CraftingEditorUtility.SanitizeAssetName(blueprintId);
            if (string.IsNullOrEmpty(safeFileName))
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Blueprint id is invalid.", "OK");
                return;
            }

            string path = $"{CraftingEditorUtility.RecipesFolder}/{safeFileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path) != null &&
                !EditorUtility.DisplayDialog(
                    "Blueprint + Crafting",
                    $"Blueprint asset '{safeFileName}' already exists. Overwrite?",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            RecipeDefinition draft = ScriptableObject.CreateInstance<RecipeDefinition>();
            draft.recipeId = blueprintId.Trim();
            draft.displayName = displayName.Trim();
            draft.description = description;
            draft.stationType = stationType;
            draft.outputItem = outputItem;
            draft.outputAmount = outputAmount;
            draft.icon = outputItem != null ? outputItem.icon : null;
            draft.ingredients = new List<RecipeIngredient>(ingredients);

            RecipeDefinition saved = CraftingEditorUtility.SaveRecipeAsset(draft, safeFileName);
            if (saved == null)
            {
                EditorUtility.DisplayDialog("Blueprint + Crafting", "Failed to save blueprint asset.", "OK");
                return;
            }

            if (addToRegistry)
                CraftingEditorUtility.AddRecipeToRegistry(saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLists();

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            SetStatus($"Created equipment blueprint '{saved.displayName}'.", MessageType.Info);
            EditorUtility.DisplayDialog("Blueprint + Crafting", $"Created blueprint '{saved.displayName}'.", "OK");
        }

        #endregion

        #region Pickups tab

        private void DrawPickupsTab()
        {
            EditorGUILayout.HelpBox(
                "Build world blueprint-scroll pickup prefabs for existing blueprints. Prefer the Blueprints tab when authoring a new blueprint + pickup together.",
                MessageType.None);

            EditorGUILayout.LabelField("Existing Blueprints", EditorStyles.boldLabel);
            pickupListScroll = EditorGUILayout.BeginScrollView(pickupListScroll, GUILayout.Height(220f));
            for (int i = 0; i < blueprintAssets.Length; i++)
            {
                RecipeDefinition blueprint = blueprintAssets[i];
                if (blueprint == null)
                    continue;

                string label = string.IsNullOrEmpty(blueprint.displayName)
                    ? blueprint.ResolvedId
                    : $"{blueprint.displayName} ({blueprint.ResolvedId})";
                bool selected = i == selectedBlueprintIndex;
                if (GUILayout.Toggle(selected, label, "Button"))
                {
                    if (selectedBlueprintIndex != i)
                        LoadBlueprint(blueprint, i);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Blueprints", GUILayout.Width(140f)))
                RefreshLists();
            EditorGUILayout.EndHorizontal();

            blueprintId = EditorGUILayout.TextField("Blueprint Id", blueprintId);

            string resolvedPath = CraftingEditorUtility.ResolveRecipePickupPrefabPath(blueprintId);
            if (!string.IsNullOrEmpty(resolvedPath))
                EditorGUILayout.LabelField("Pickup Path", resolvedPath);

            EditorGUILayout.Space(6f);
            DrawPickupPrefabSection();

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Pickup Prefab", GUILayout.Height(34f)))
                SavePickupPrefab();
            if (GUILayout.Button("Place In Scene", GUILayout.Height(34f)))
                PlacePickupInScene();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Batch Create Missing Pickup Prefabs", GUILayout.Height(30f)))
                BatchCreateMissingPickups();
        }

        private void BatchCreateMissingPickups()
        {
            int created = 0;
            for (int i = 0; i < blueprintAssets.Length; i++)
            {
                RecipeDefinition blueprint = blueprintAssets[i];
                if (blueprint == null || string.IsNullOrEmpty(blueprint.ResolvedId))
                    continue;

                if (CraftingEditorUtility.LoadRecipePickupPrefab(blueprint.ResolvedId) != null)
                    continue;

                GameObject prefab = CraftingEditorUtility.CreateRecipePickupPrefab(
                    blueprint.ResolvedId,
                    pickupVisualTemplate,
                    pickupInteractRange,
                    pickupColliderSize,
                    autoFitPickupCollider,
                    confirmOverwrite: false);
                if (prefab != null)
                    created++;
            }

            SetStatus(
                created > 0
                    ? $"Created {created} missing blueprint pickup prefab(s)."
                    : "No missing pickup prefabs — all blueprints already have one.",
                MessageType.Info);
            EditorUtility.DisplayDialog("Blueprint + Crafting", statusMessage, "OK");
        }

        #endregion

        #region Registry tab

        private void DrawRegistryTab()
        {
            EditorGUILayout.HelpBox(
                "Maintenance actions for the blueprint registry and scene crafting stations. " +
                "These replace the old standalone Recipe Creator / Sync Recipe menus.",
                MessageType.None);

            EditorGUILayout.LabelField("Registry", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Asset", CraftingEditorUtility.RecipeRegistryPath);

            if (GUILayout.Button("Sync Blueprint Registry From Assets", GUILayout.Height(32f)))
            {
                EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.Crafting + "Sync Blueprint Registry");
                SetStatus("Ran Sync Blueprint Registry.", MessageType.Info);
            }

            if (GUILayout.Button("Sync Blueprint Icons From Output", GUILayout.Height(32f)))
            {
                CraftingEditorUtility.SyncRecipeIconsFromOutput();
                SetStatus("Synced blueprint icons from output items.", MessageType.Info);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Scene / Seed", EditorStyles.boldLabel);

            if (GUILayout.Button("Wire Scene Stations", GUILayout.Height(32f)))
            {
                EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.Crafting + "Wire Scene Stations");
                SetStatus("Wired scene crafting stations and pickups.", MessageType.Info);
            }

            if (GUILayout.Button("Seed Starter Blueprints", GUILayout.Height(32f)))
            {
                EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.Crafting + "Seed Starter Blueprints");
                RefreshLists();
                SetStatus("Seeded starter blueprints.", MessageType.Info);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Ping", EditorStyles.boldLabel);
            if (GUILayout.Button("Select Blueprint Registry Asset", GUILayout.Height(28f)))
            {
                Object registry = AssetDatabase.LoadAssetAtPath<Object>(CraftingEditorUtility.RecipeRegistryPath);
                if (registry != null)
                {
                    Selection.activeObject = registry;
                    EditorGUIUtility.PingObject(registry);
                }
                else
                {
                    SetStatus($"Registry not found at {CraftingEditorUtility.RecipeRegistryPath}.", MessageType.Warning);
                }
            }

            if (GUILayout.Button("Reveal Blueprints Data Folder", GUILayout.Height(28f)))
            {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(CraftingEditorUtility.RecipesFolder);
                if (folder != null)
                {
                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                }
            }
        }

        #endregion
    }
}
