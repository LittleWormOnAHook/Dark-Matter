#if UNITY_EDITOR
using System.IO;
using Project.EditorTools;
using Project.Interaction;
using Project.Map;
using Project.UI;
using Project.Vendor;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Vendor
{
    public static class DMVendorSceneMenu
    {
        private const string ProjectPrefabFolder = "Assets/_Project/Prefabs/NPCs";
        private const string CommissaryPrefabPath = ProjectPrefabFolder + "/DM_Vendor_Commissary.prefab";
        private const string TechPrefabPath = ProjectPrefabFolder + "/DM_Vendor_Tech.prefab";
        private const string CommissaryProfilePath = "Assets/_Project/Resources/Vendors/DM_VendorProfile_Commissary.asset";
        private const string TechProfilePath = "Assets/_Project/Resources/Vendors/DM_VendorProfile_Tech.asset";

        [MenuItem(DarkMatterGenesisEditorMenus.World + "Wire Scene Commissary And Tech Vendors")]
        public static void WireSceneVendors()
        {
            string commissary = WireSelection(CommissaryProfilePath, CommissaryPrefabPath, "Commissary", "Commis");
            string tech = WireSelection(TechProfilePath, TechPrefabPath, "Tech", "Tech");
            SelectProjectPrefabs(commissary, tech);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.World + "Add Commissary Vendor To Selection")]
        public static void AddCommissary()
        {
            string saved = WireSelection(CommissaryProfilePath, CommissaryPrefabPath, "Commissary", "Commis");
            SelectProjectPrefabs(saved);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.World + "Add Tech Vendor To Selection")]
        public static void AddTech()
        {
            string saved = WireSelection(TechProfilePath, TechPrefabPath, "Tech", "Tech");
            SelectProjectPrefabs(saved);
        }

        private static string WireSelection(string profilePath, string projectPrefabPath, string label, string sceneNameHint)
        {
            DMVendorContentMenu.EnsureVendorContent();
            DMVendorProfile profile = AssetDatabase.LoadAssetAtPath<DMVendorProfile>(profilePath);
            if (profile == null)
            {
                Debug.LogError("[DM] Missing vendor profile at " + profilePath);
                return null;
            }

            GameObject sceneObject = FindSceneVendorRoot(sceneNameHint);
            TryResolveSelection(out GameObject selectedSceneObject, out string selectedPrefabPath);
            if (sceneObject == null)
                sceneObject = selectedSceneObject;

            if (sceneObject != null)
                ApplyToSceneObject(sceneObject, profile);

            if (sceneObject == null && string.IsNullOrEmpty(selectedPrefabPath))
            {
                Debug.LogWarning("[DM] Select a vendor prefab in the Project window, or a scene instance.");
                return null;
            }

            string savedPrefabPath = WriteProjectPrefab(selectedPrefabPath, sceneObject, projectPrefabPath, profile, label);
            if (string.IsNullOrEmpty(savedPrefabPath))
                return null;

            Debug.Log("[DM] " + label + " vendor saved to " + savedPrefabPath
                + (sceneObject != null ? " and applied to scene object " + sceneObject.name : string.Empty)
                + ".");
            return savedPrefabPath;
        }

        private static void SelectProjectPrefabs(params string[] prefabPaths)
        {
            var objects = new System.Collections.Generic.List<Object>();
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                if (string.IsNullOrEmpty(prefabPaths[i]))
                    continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab != null)
                    objects.Add(prefab);
            }

            if (objects.Count == 0)
                return;

            EditorUtility.FocusProjectWindow();
            Selection.objects = objects.ToArray();
            EditorGUIUtility.PingObject(objects[0]);
        }

        private static GameObject FindSceneVendorRoot(string nameHint)
        {
            if (string.IsNullOrEmpty(nameHint))
                return null;

            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || string.IsNullOrEmpty(root.name))
                    continue;
                if (root.name.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (PrefabUtility.IsPartOfPrefabInstance(root))
                    return PrefabUtility.GetOutermostPrefabInstanceRoot(root) ?? root;
                return root;
            }

            return null;
        }

        private static bool TryResolveSelection(out GameObject sceneObject, out string sourcePrefabPath)
        {
            sceneObject = null;
            sourcePrefabPath = null;

            Object[] projectAssets = Selection.GetFiltered<Object>(SelectionMode.Assets);
            for (int i = 0; i < projectAssets.Length; i++)
            {
                string assetPath = AssetDatabase.GetAssetPath(projectAssets[i]);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    sourcePrefabPath = assetPath;
                    break;
                }
            }

            Object[] selected = Selection.objects;
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i] as GameObject;
                if (go == null || EditorUtility.IsPersistent(go))
                    continue;

                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    sceneObject = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if (string.IsNullOrEmpty(sourcePrefabPath))
                        sourcePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    return sceneObject != null || !string.IsNullOrEmpty(sourcePrefabPath);
                }

                sceneObject = go;
            }

            if (sceneObject == null && Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject))
                sceneObject = Selection.activeGameObject;

            if (sceneObject != null && PrefabUtility.IsPartOfPrefabInstance(sceneObject))
            {
                sceneObject = PrefabUtility.GetOutermostPrefabInstanceRoot(sceneObject);
                if (string.IsNullOrEmpty(sourcePrefabPath))
                    sourcePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneObject);
            }

            return sceneObject != null || !string.IsNullOrEmpty(sourcePrefabPath);
        }

        private static string WriteProjectPrefab(
            string sourcePrefabPath,
            GameObject sceneObject,
            string projectPrefabPath,
            DMVendorProfile profile,
            string label)
        {
            EnsureFolder(ProjectPrefabFolder);

            string selectedPath = sourcePrefabPath;
            if (string.IsNullOrEmpty(selectedPath) && sceneObject != null)
                selectedPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneObject);

            bool selectedIsProjectOwned = !string.IsNullOrEmpty(selectedPath) && !IsVendorPackPath(selectedPath);
            string savePath = selectedIsProjectOwned ? selectedPath : projectPrefabPath;
            string loadPath = selectedIsProjectOwned ? selectedPath : null;

            GameObject contents;
            bool loadedContents = false;
            if (sceneObject != null && !selectedIsProjectOwned)
            {
                contents = Object.Instantiate(sceneObject);
                contents.name = "DM_Vendor_" + label;
                contents.hideFlags = HideFlags.None;
                if (PrefabUtility.IsPartOfAnyPrefab(contents))
                    PrefabUtility.UnpackPrefabInstance(contents, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else if (!string.IsNullOrEmpty(loadPath) && File.Exists(ToAbsolute(loadPath)))
            {
                contents = PrefabUtility.LoadPrefabContents(loadPath);
                loadedContents = contents != null;
            }
            else if (!string.IsNullOrEmpty(selectedPath) && File.Exists(ToAbsolute(selectedPath)))
            {
                contents = PrefabUtility.LoadPrefabContents(selectedPath);
                loadedContents = contents != null;
            }
            else if (sceneObject != null)
            {
                contents = Object.Instantiate(sceneObject);
                contents.name = "DM_Vendor_" + label;
            }
            else
            {
                Debug.LogError("[DM] Could not load a prefab from the Project-window selection.");
                return null;
            }

            if (contents == null)
            {
                Debug.LogError("[DM] Prefab contents were empty.");
                return null;
            }

            ApplyVendorToRoot(contents, profile);

            if (savePath != loadPath && File.Exists(ToAbsolute(savePath)))
            {
                if (loadedContents)
                    PrefabUtility.UnloadPrefabContents(contents);
                else
                    Object.DestroyImmediate(contents);

                contents = PrefabUtility.LoadPrefabContents(savePath);
                loadedContents = true;
                ApplyVendorToRoot(contents, profile);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, savePath, out bool success);
            if (loadedContents)
                PrefabUtility.UnloadPrefabContents(contents);
            else
                Object.DestroyImmediate(contents);

            if (!success)
            {
                Debug.LogError("[DM] Failed to save vendor prefab at " + savePath);
                return null;
            }

            AssetDatabase.ImportAsset(savePath);
            return savePath;
        }

        private static void ApplyToSceneObject(GameObject sceneObject, DMVendorProfile profile)
        {
            if (sceneObject == null || EditorUtility.IsPersistent(sceneObject))
                return;

            Undo.RegisterCompleteObjectUndo(sceneObject, "Wire Vendor");
            ApplyVendorToRoot(sceneObject, profile);
            EditorUtility.SetDirty(sceneObject);
        }

        private static void ApplyVendorToRoot(GameObject root, DMVendorProfile profile)
        {
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            if (box.size.sqrMagnitude < 0.01f)
                box.size = new Vector3(0.9f, 2f, 0.9f);
            if (Mathf.Abs(box.center.y) < 0.01f)
                box.center = new Vector3(0f, 1f, 0f);

            DMVendorNpc npc = root.GetComponent<DMVendorNpc>();
            if (npc == null)
                npc = root.AddComponent<DMVendorNpc>();

            SerializedObject so = new SerializedObject(npc);
            SerializedProperty profileProp = so.FindProperty("profile");
            SerializedProperty rangeProp = so.FindProperty("interactRange");
            if (profileProp != null)
                profileProp.objectReferenceValue = profile;
            if (rangeProp != null && profile != null)
                rangeProp.floatValue = profile.interactRange;
            so.ApplyModifiedPropertiesWithoutUndo();
            npc.Configure(profile);

            string poiName = profile != null && !string.IsNullOrWhiteSpace(profile.displayName)
                ? profile.displayName
                : "Vendor";
            ScannableTarget scan = root.GetComponent<ScannableTarget>();
            if (scan == null)
                scan = root.AddComponent<ScannableTarget>();
            scan.Configure(poiName, DarkMatterGenesisUiPalette.MapPoiBlue, ScannerTargetCategory.Interactable, true, false, true);

            MapMarker marker = root.GetComponent<MapMarker>();
            if (marker == null)
                marker = root.AddComponent<MapMarker>();
            marker.ConfigureScannedPoi(poiName, DarkMatterGenesisUiPalette.MapPoiBlue);
        }

        private static bool IsVendorPackPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return true;

            string n = assetPath.Replace('\\', '/');
            return n.StartsWith("Assets/PolygonTown/", System.StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("Assets/PROTOFACTOR/", System.StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("Assets/Invector-", System.StringComparison.OrdinalIgnoreCase)
                || !n.StartsWith("Assets/_Project/", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string ToAbsolute(string assetPath)
        {
            string dataPath = Application.dataPath;
            if (assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
                return Path.Combine(Directory.GetParent(dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
            return assetPath;
        }
    }
}
#endif
