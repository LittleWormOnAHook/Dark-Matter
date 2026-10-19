using FORGE3D;
using Project.Building;
using Project.Combat.Turrets;
using Project.EditorTools;
using Project.EditorTools.Building;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    [InitializeOnLoad]
    public static class DMTurretEditorIntegration
    {
        static DMTurretEditorIntegration()
        {
            F3DTurretEditorCallbacks.OnAddPlaceableToScene += AddPlaceableToScene;
            F3DTurretEditorCallbacks.OnBakePlaceablePrefab += BakePlaceablePrefab;
            F3DTurretEditorCallbacks.OnBakeSelectedSceneTurret += BakeSelectedSceneTurret;
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Add placeable to scene (current template)", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Add_Placeable_To_Scene)]
        public static void MenuAddPlaceable()
        {
            AddPlaceableToScene(F3DTurretEditorWindow.GetCurrentIndex());
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Bake combat prefab (current template)", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Bake_Combat_Prefab)]
        public static void MenuBakeCombatCurrentTemplate()
        {
            int index = F3DTurretEditorWindow.GetCurrentIndex();
            GameObject prefab = DMTurretPrefabBakeUtility.BakeCombatPrefabFromTemplateIndex(index);
            if (prefab != null)
                Selection.activeObject = prefab;
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Bake placeable prefab (current template)", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Bake_Placeable_Prefab)]
        public static void MenuBakeCurrentTemplate()
        {
            BakePlaceablePrefab(F3DTurretEditorWindow.GetCurrentIndex());
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Bake selected scene turret → prefab...", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Bake_Selected_Scene_Turret)]
        public static void MenuBakeSelection()
        {
            BakeSelectedSceneTurret();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Ensure alarm on all combat turret prefabs", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Ensure_Alarm_All_Prefabs)]
        public static void MenuEnsureAlarmAllCombatPrefabs()
        {
            int ok = DMTurretAlarmEnsure.EnsureAllCombatTurretPrefabs();
            Debug.Log("[DM Turret] Ensured Alarm on " + ok + " combat turret prefab(s) under "
                + DMTurretPrefabBakeUtility.CombatTurretFolder + ". Re-place scene instances from prefabs if needed.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Ensure alarm on selected/scene turrets", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Ensure_Alarm_Selected)]
        public static void MenuEnsureAlarmSelectedOrScene()
        {
            var roots = new System.Collections.Generic.List<GameObject>();
            if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            {
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    DMTurretRuntime runtime = Selection.gameObjects[i].GetComponentInParent<DMTurretRuntime>();
                    if (runtime != null)
                        roots.Add(runtime.gameObject);
                }
            }
            else
            {
                DMTurretRuntime[] sceneTurrets = Object.FindObjectsByType<DMTurretRuntime>(FindObjectsInactive.Exclude);
                for (int i = 0; i < sceneTurrets.Length; i++)
                {
                    if (sceneTurrets[i] != null)
                        roots.Add(sceneTurrets[i].gameObject);
                }
            }

            if (roots.Count == 0)
            {
                Debug.LogWarning("[DM Turret] Select turret root(s) with DMTurretRuntime, or run with nothing selected to batch the open scene.");
                return;
            }

            int ok = 0;
            for (int i = 0; i < roots.Count; i++)
            {
                Undo.RegisterFullObjectHierarchyUndo(roots[i], "Ensure turret Alarm");
                if (DMTurretAlarmEnsure.EnsureAlarmSphere(roots[i]))
                    ok++;
                DMTurretPrefabBakeUtility.WireAlarmLight(roots[i]);
                EditorUtility.SetDirty(roots[i]);
            }

            Debug.Log("[DM Turret] Ensured Alarm on " + roots.Count + " turret(s) (" + ok + " new sphere(s)).");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Center pivot on selected turret", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Center_Pivot)]
        public static void MenuRecenterSelectedTurret()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[DM Turret] Select a turret root to center its pivot.");
                return;
            }

            DMTurretRuntime runtime = selected.GetComponentInParent<DMTurretRuntime>();
            GameObject root = runtime != null ? runtime.gameObject : selected;
            Undo.RegisterFullObjectHierarchyUndo(root, "Center turret pivot");
            DMTurretPrefabBakeUtility.RecenterPlacedInstance(root);
            EditorUtility.SetDirty(root);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Register all templates in Building Equipment", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Register_All_Equipment)]
        public static void MenuRegisterAllBuildingEquipment()
        {
            DMBuildingTurretBuilder.RegisterAllTemplates();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Register current template in Building Equipment", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Register_Current_Equipment)]
        public static void MenuRegisterBuildingEquipment()
        {
            DMBuildingTurretBuilder.BuildMenu();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Sync all FORGE3D templates → Combat/Turrets", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Sync_All_Combat)]
        public static void MenuSyncAllCombatTurrets()
        {
            int ok = DMTurretPrefabBakeUtility.SyncAllCombatTurretsFromForge3D();
            Debug.Log("[DM Turret] Combat/Turrets sync finished (" + ok + " prefabs).");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CombatTurrets + "Wire runtime references on selected turret", false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Turrets_Wire_Runtime_References)]
        public static void MenuWireSelectedRuntimeReferences()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[DM Turret] Select a turret root with DMTurretRuntime.");
                return;
            }

            DMTurretRuntime runtime = selected.GetComponentInParent<DMTurretRuntime>();
            GameObject root = runtime != null ? runtime.gameObject : selected;
            Undo.RegisterFullObjectHierarchyUndo(root, "Wire turret runtime references");
            DMTurretPrefabBakeUtility.WireRuntimeReferences(root);
            EditorUtility.SetDirty(root);
        }

        public static void AddPlaceableToScene(int templateIndex)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (Selection.activeTransform != null)
            {
                position = Selection.activeTransform.position;
                rotation = Selection.activeTransform.rotation;
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                position = SceneView.lastActiveSceneView.pivot;
            }

            GameObject placed = DMTurretPrefabBakeUtility.AddPlaceableToScene(templateIndex, position, rotation);
            if (placed == null)
                return;

            DMTurretRuntime.StripAllDemoInput();
            EditorGUIUtility.PingObject(placed);
        }

        public static void BakePlaceablePrefab(int templateIndex)
        {
            DMTurretPrefabBakeUtility.TryGetTemplateInfo(templateIndex, out _, out string slug);
            string fileSlug = "SI_turret_" + slug;
            string defaultPath = DMTurretPrefabBakeUtility.EquipmentPrefabPath(templateIndex);
            string path = EditorUtility.SaveFilePanelInProject(
                "Save placeable turret prefab",
                fileSlug,
                "prefab",
                "Choose where to save the baked turret prefab.",
                DMTurretPrefabBakeUtility.EquipmentFolder);

            if (string.IsNullOrEmpty(path))
                path = defaultPath;

            GameObject prefab = DMTurretPrefabBakeUtility.BakePrefabFromTemplateIndex(templateIndex, path, fileSlug);
            if (prefab != null)
                Selection.activeObject = prefab;
        }

        public static void BakeSelectedSceneTurret()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[DM Turret] Select a scene turret to bake.");
                return;
            }

            F3DTurret turret = selected.GetComponentInParent<F3DTurret>();
            if (turret == null && selected.GetComponentInParent<F3DTurretConstructor>() == null)
            {
                Debug.LogWarning("[DM Turret] Selection is not a FORGE3D turret.");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Bake scene turret",
                selected.name,
                "prefab",
                "Save prefab",
                DMTurretPrefabBakeUtility.CombatTurretFolder);
            if (string.IsNullOrEmpty(path))
                return;

            GameObject root = turret != null ? turret.gameObject : selected.GetComponentInParent<F3DTurretConstructor>().gameObject;
            GameObject prefab = DMTurretPrefabBakeUtility.BakePrefabFromSceneHierarchy(root, path);
            if (prefab != null)
                Selection.activeObject = prefab;
        }
    }
}
