using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Optional workflow: capture scene hierarchy edits made during Play Mode and reapply them after exit.
    /// Toggle via Tools → Survival Pioneer → Maintenance → Persist Play Mode Edits.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeEditPersistence
    {
        private const string EnabledPrefKey = "SurvivalPioneer.PlayModeEditPersistence.Enabled";
        private const string ForcedDisableMigrationKey = "SurvivalPioneer.PlayModeEditPersistence.ForcedOff.v1";
        private const string SnapshotFileName = "play-mode-snapshot.json";
        private const string MenuPath = SurvivalPioneerEditorMenus.Maintenance + "Persist Play Mode Edits";
        private static readonly string[] PriorityAssetScanRoots =
        {
            "Assets/_Project/Data",
            "Assets/_Project/Resources",
            "Assets/_Project/Prefabs",
        };

        private static readonly string[] ProjectAssetScanRoots = { "Assets/_Project" };

        private static readonly string[] AbsoluteAssetFilters =
        {
            "t:ScriptableObject",
            "t:Material",
            "t:Font",
            "t:TMP_FontAsset",
            "t:TextAsset",
            "t:AnimatorController",
            "t:AnimatorOverrideController",
            "t:AvatarMask",
            "t:PhysicMaterial",
        };

        private static readonly string[] AlwaysCaptureAssetPaths =
        {
        };

        private static readonly string[] ExcludedProjectAssetPaths =
        {
            "Assets/_Project/Resources/Optics/OpticsCrosshairLibrary.asset",
        };

        private static bool pendingApply;
        private static Dictionary<string, string> playModeAssetBaselines;
        private static Dictionary<string, UnityEngine.Object> loadedAssetLookup;

        static PlayModeEditPersistence()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EnsureFeatureDisabledByDefault();
        }

        /// <summary>
        /// One-time migration: turn off auto-capture and discard any pending snapshot.
        /// Re-enable via Tools → Survival Pioneer → Maintenance → Persist Play Mode Edits.
        /// </summary>
        private static void EnsureFeatureDisabledByDefault()
        {
            if (EditorPrefs.GetBool(ForcedDisableMigrationKey, false))
                return;

            Enabled = false;
            DeleteSnapshotFile();
            pendingApply = false;
            EditorPrefs.SetBool(ForcedDisableMigrationKey, true);
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, false);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        public enum PlayModeSaveScope
        {
            AllOpenScenes,
            SelectedObjectsOnly,
            SelectedHierarchy
        }

        public struct PlayModeSaveSummary
        {
            public int objectCount;
            public int sceneCount;
            public int scriptableObjectCount;
            public int projectAssetCount;
            public int prefabAssetCount;
            public string capturedUtc;
        }

        public static int LastPrefabAssetCount => LastSaveSummary.prefabAssetCount;

        public static int LastProjectAssetCount => LastSaveSummary.projectAssetCount > 0
            ? LastSaveSummary.projectAssetCount
            : LastSaveSummary.scriptableObjectCount;

        public static bool HasPendingSnapshot => File.Exists(SnapshotPath);

        public static void ClearPendingSnapshot()
        {
            DeleteSnapshotFile();
        }

        public static PlayModeSaveSummary LastSaveSummary { get; private set; }

        public static bool SaveNow(PlayModeSaveScope scope = PlayModeSaveScope.AllOpenScenes)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Play Mode Saver",
                    "Enter Play Mode, make your edits, then click Save Now.",
                    "OK");
                return false;
            }

            try
            {
                LastSaveSummary = CaptureOpenScenes(scope);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                loadedAssetLookup = null;
            }

            pendingApply = LastSaveSummary.objectCount > 0
                || LastProjectAssetCount > 0
                || LastPrefabAssetCount > 0;

            if (!pendingApply)
            {
                EditorUtility.DisplayDialog(
                    "Play Mode Saver",
                    "Nothing was captured. Check Save Scope and your selection, then try again.",
                    "OK");
                return false;
            }

            Debug.Log(
                $"[Play Mode Saver] Captured {LastSaveSummary.objectCount} object(s), " +
                $"{LastProjectAssetCount} data/material/text asset(s), {LastPrefabAssetCount} prefab(s) across " +
                $"{LastSaveSummary.sceneCount} scene(s). Edits apply when Play Mode exits.");

            return true;
        }

        public static void SaveAndExitPlayMode(PlayModeSaveScope scope = PlayModeSaveScope.AllOpenScenes)
        {
            if (SaveNow(scope))
                EditorApplication.isPlaying = false;
        }

        [MenuItem(MenuPath, false, 3)]
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled)
            {
                DeleteSnapshotFile();
                pendingApply = false;
            }

            Debug.Log(Enabled
                ? "[Play Mode Edits] Enabled. Edits made while playing will be kept when Play Mode stops."
                : "[Play Mode Edits] Disabled. Unity will revert Play Mode scene changes as usual.");
        }

        [MenuItem(MenuPath, true)]
        public static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (!Enabled)
                        break;

                    playModeAssetBaselines = null;
                    EditorApplication.delayCall += CapturePlayModeBaselines;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    if (Enabled && !HasPendingSnapshot)
                    {
                        try
                        {
                            CaptureOpenScenes(PlayModeSaveScope.AllOpenScenes);
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                            loadedAssetLookup = null;
                        }
                    }

                    pendingApply = HasPendingSnapshot;
                    playModeAssetBaselines = null;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (!pendingApply)
                        return;

                    pendingApply = false;
                    EditorApplication.delayCall += ApplyCapturedSnapshots;
                    break;
            }
        }

        private static void CapturePlayModeBaselines()
        {
            if (!EditorApplication.isPlaying)
                return;

            HashSet<string> baselinePaths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < AlwaysCaptureAssetPaths.Length; i++)
                baselinePaths.Add(AlwaysCaptureAssetPaths[i]);

            for (int f = 0; f < AbsoluteAssetFilters.Length; f++)
            {
                for (int r = 0; r < PriorityAssetScanRoots.Length; r++)
                    AddAssetPaths(baselinePaths, AbsoluteAssetFilters[f], new[] { PriorityAssetScanRoots[r] });
            }

            loadedAssetLookup = BuildLoadedAssetLookup();
            playModeAssetBaselines = new Dictionary<string, string>(baselinePaths.Count, StringComparer.Ordinal);

            int index = 0;
            int total = baselinePaths.Count;
            foreach (string assetPath in baselinePaths)
            {
                index++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Play Mode Saver",
                        $"Recording play-mode baselines ({index}/{total})",
                        total <= 0 ? 1f : index / (float)total))
                {
                    playModeAssetBaselines = null;
                    EditorUtility.ClearProgressBar();
                    loadedAssetLookup = null;
                    Debug.LogWarning("[Play Mode Saver] Baseline capture cancelled.");
                    return;
                }

                playModeAssetBaselines[assetPath] = ComputeAssetFingerprint(assetPath);
            }

            EditorUtility.ClearProgressBar();
            loadedAssetLookup = null;
        }

        private static string SnapshotPath => Path.Combine(SnapshotDirectory, SnapshotFileName);

        private static string SnapshotDirectory
        {
            get
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Library", "SurvivalPioneerPlayModeSnapshots");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        private static PlayModeSaveSummary CaptureOpenScenes(PlayModeSaveScope scope)
        {
            loadedAssetLookup = BuildLoadedAssetLookup();

            PlayModeSnapshot snapshot = new PlayModeSnapshot
            {
                capturedUtc = DateTime.UtcNow.ToString("o"),
                scenes = Array.Empty<SceneSnapshot>(),
                projectAssets = Array.Empty<ProjectAssetSnapshot>(),
                prefabAssets = Array.Empty<PrefabAssetSnapshot>()
            };

            List<SceneSnapshot> sceneList = new List<SceneSnapshot>();
            HashSet<string> allowedPaths = BuildAllowedHierarchyPaths(scope);
            bool captureAll = scope == PlayModeSaveScope.AllOpenScenes;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Play Mode Saver",
                        $"Capturing scene '{scene.name}'",
                        (i + 1f) / Mathf.Max(1, SceneManager.sceneCount)))
                {
                    EditorUtility.ClearProgressBar();
                    loadedAssetLookup = null;
                    Debug.LogWarning("[Play Mode Saver] Scene capture cancelled.");
                    return new PlayModeSaveSummary();
                }

                List<GameObjectSnapshot> objectList = new List<GameObjectSnapshot>();
                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    CaptureHierarchy(roots[r].transform, objectList, captureAll, allowedPaths);

                if (objectList.Count == 0)
                    continue;

                sceneList.Add(new SceneSnapshot
                {
                    scenePath = scene.path,
                    objects = objectList.ToArray()
                });
            }

            snapshot.scenes = sceneList.ToArray();
            BuildChangedAndReferencedAssetPaths(
                sceneList,
                scope,
                out HashSet<string> projectAssetPaths,
                out HashSet<string> prefabAssetPaths);

            snapshot.projectAssets = CaptureProjectAssetSnapshots(projectAssetPaths);
            snapshot.prefabAssets = CapturePrefabAssetSnapshots(prefabAssetPaths);

            int objectCount = 0;
            for (int i = 0; i < sceneList.Count; i++)
                objectCount += sceneList[i].objects?.Length ?? 0;

            int assetCount = snapshot.projectAssets?.Length ?? 0;
            int prefabCount = snapshot.prefabAssets?.Length ?? 0;
            PlayModeSaveSummary summary = new PlayModeSaveSummary
            {
                objectCount = objectCount,
                sceneCount = sceneList.Count,
                scriptableObjectCount = assetCount,
                projectAssetCount = assetCount,
                prefabAssetCount = prefabCount,
                capturedUtc = snapshot.capturedUtc
            };

            if (objectCount == 0 && assetCount == 0 && prefabCount == 0)
            {
                DeleteSnapshotFile();
                LastSaveSummary = summary;
                return summary;
            }

            string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
            File.WriteAllText(SnapshotPath, json);
            LastSaveSummary = summary;
            return summary;
        }

        private static HashSet<string> BuildAllowedHierarchyPaths(PlayModeSaveScope scope)
        {
            HashSet<string> paths = new HashSet<string>();
            if (scope == PlayModeSaveScope.AllOpenScenes)
                return paths;

            GameObject[] selectedObjects = Selection.gameObjects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject selectedObject = selectedObjects[i];
                if (selectedObject == null)
                    continue;

                Transform selectedTransform = selectedObject.transform;
                paths.Add(GetHierarchyPath(selectedTransform));

                if (scope == PlayModeSaveScope.SelectedHierarchy)
                    AddDescendantHierarchyPaths(selectedTransform, paths);
            }

            return paths;
        }

        private static void AddDescendantHierarchyPaths(Transform root, HashSet<string> paths)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                paths.Add(GetHierarchyPath(child));
                AddDescendantHierarchyPaths(child, paths);
            }
        }

        private static void CaptureHierarchy(
            Transform transform,
            List<GameObjectSnapshot> output,
            bool captureAll,
            HashSet<string> allowedPaths)
        {
            if (transform == null)
                return;

            if (PlayModeEditPlayerExclusions.ShouldSkipCaptureChildren(transform))
                return;

            string hierarchyPath = GetHierarchyPath(transform);
            bool include = !PlayModeEditPlayerExclusions.ShouldSkipCapture(transform)
                && (captureAll || (allowedPaths != null && allowedPaths.Contains(hierarchyPath)));
            if (include)
            {
                GameObjectSnapshot entry = new GameObjectSnapshot
                {
                    hierarchyPath = hierarchyPath,
                    activeSelf = transform.gameObject.activeSelf,
                    tag = transform.gameObject.tag,
                    layer = transform.gameObject.layer,
                    isStatic = transform.gameObject.isStatic,
                    hasRectTransform = transform is RectTransform,
                    localPosition = transform.localPosition,
                    localRotation = transform.localRotation,
                    localScale = transform.localScale,
                    componentProperties = CaptureComponentProperties(transform.gameObject)
                };

                if (transform is RectTransform rectTransform)
                {
                    entry.anchorMin = rectTransform.anchorMin;
                    entry.anchorMax = rectTransform.anchorMax;
                    entry.pivot = rectTransform.pivot;
                    entry.anchoredPosition = rectTransform.anchoredPosition;
                    entry.anchoredPosition3D = rectTransform.anchoredPosition3D;
                    entry.sizeDelta = rectTransform.sizeDelta;
                    entry.offsetMin = rectTransform.offsetMin;
                    entry.offsetMax = rectTransform.offsetMax;
                }

                output.Add(entry);
            }

            for (int i = 0; i < transform.childCount; i++)
                CaptureHierarchy(transform.GetChild(i), output, captureAll, allowedPaths);
        }

        private static ComponentPropertySnapshot[] CaptureComponentProperties(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            List<ComponentPropertySnapshot> captured = new List<ComponentPropertySnapshot>();

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component is Transform)
                    continue;

                SerializedObject serializedObject = new SerializedObject(component);
                PropertySnapshot[] properties = CaptureProperties(serializedObject, component);
                if (properties.Length == 0)
                    continue;

                captured.Add(new ComponentPropertySnapshot
                {
                    componentType = component.GetType().AssemblyQualifiedName,
                    properties = properties
                });
            }

            return captured.Count > 0 ? captured.ToArray() : Array.Empty<ComponentPropertySnapshot>();
        }

        private static void BuildChangedAndReferencedAssetPaths(
            List<SceneSnapshot> scenes,
            PlayModeSaveScope scope,
            out HashSet<string> projectAssetPaths,
            out HashSet<string> prefabAssetPaths)
        {
            projectAssetPaths = new HashSet<string>(StringComparer.Ordinal);
            prefabAssetPaths = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < AlwaysCaptureAssetPaths.Length; i++)
                AddAssetPathByKind(AlwaysCaptureAssetPaths[i], projectAssetPaths, prefabAssetPaths);

            for (int i = 0; i < Selection.objects.Length; i++)
            {
                string selectedPath = AssetDatabase.GetAssetPath(Selection.objects[i]);
                if (IsProjectAssetPath(selectedPath))
                    AddAssetPathByKind(selectedPath, projectAssetPaths, prefabAssetPaths);
            }

            for (int s = 0; s < scenes.Count; s++)
            {
                SceneSnapshot sceneSnapshot = scenes[s];
                if (sceneSnapshot?.objects == null)
                    continue;

                for (int o = 0; o < sceneSnapshot.objects.Length; o++)
                    CollectAssetPathsFromComponentSnapshots(
                        sceneSnapshot.objects[o].componentProperties,
                        projectAssetPaths);
            }

            CollectPrefabPathsFromOpenScenes(prefabAssetPaths);

            if (playModeAssetBaselines != null)
            {
                foreach (KeyValuePair<string, string> baseline in playModeAssetBaselines)
                {
                    if (ComputeAssetFingerprint(baseline.Key) == baseline.Value)
                        continue;

                    AddAssetPathByKind(baseline.Key, projectAssetPaths, prefabAssetPaths);
                }
            }
            else if (scope == PlayModeSaveScope.AllOpenScenes)
            {
                AddChangedAssetsFromFolderFallback(projectAssetPaths, prefabAssetPaths);
            }
        }

        private static void AddAssetPathByKind(
            string assetPath,
            HashSet<string> projectAssetPaths,
            HashSet<string> prefabAssetPaths)
        {
            if (!IsProjectAssetPath(assetPath))
                return;

            if (IsPrefabAssetPath(assetPath))
                prefabAssetPaths.Add(assetPath);
            else
                projectAssetPaths.Add(assetPath);
        }

        private static void AddChangedAssetsFromFolderFallback(
            HashSet<string> projectAssetPaths,
            HashSet<string> prefabAssetPaths)
        {
            HashSet<string> fallbackPaths = new HashSet<string>(StringComparer.Ordinal);
            for (int f = 0; f < AbsoluteAssetFilters.Length; f++)
            {
                for (int r = 0; r < PriorityAssetScanRoots.Length; r++)
                    AddAssetPaths(fallbackPaths, AbsoluteAssetFilters[f], new[] { PriorityAssetScanRoots[r] });
            }

            foreach (string assetPath in fallbackPaths)
            {
                if (EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath)))
                    AddAssetPathByKind(assetPath, projectAssetPaths, prefabAssetPaths);
            }
        }

        private static void CollectPrefabPathsFromOpenScenes(HashSet<string> prefabAssetPaths)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    CollectPrefabPathsFromTransform(roots[r].transform, prefabAssetPaths);
            }
        }

        private static void CollectPrefabPathsFromTransform(Transform transform, HashSet<string> prefabAssetPaths)
        {
            if (transform == null)
                return;

            if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject);
                if (IsProjectAssetPath(prefabPath))
                    prefabAssetPaths.Add(prefabPath);
            }

            for (int i = 0; i < transform.childCount; i++)
                CollectPrefabPathsFromTransform(transform.GetChild(i), prefabAssetPaths);
        }

        private static Dictionary<string, UnityEngine.Object> BuildLoadedAssetLookup()
        {
            Dictionary<string, UnityEngine.Object> lookup = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            if (!EditorApplication.isPlaying)
                return lookup;

            UnityEngine.Object[] loadedAssets = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            for (int i = 0; i < loadedAssets.Length; i++)
            {
                UnityEngine.Object loadedAsset = loadedAssets[i];
                if (loadedAsset == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(loadedAsset);
                if (!IsProjectAssetPath(assetPath) || lookup.ContainsKey(assetPath))
                    continue;

                lookup[assetPath] = loadedAsset;
            }

            return lookup;
        }

        private static string ComputeAssetFingerprint(string assetPath)
        {
            UnityEngine.Object asset = LoadAssetForCapture(assetPath);
            if (asset == null)
                return string.Empty;

            PropertySnapshot[] properties = CaptureProperties(new SerializedObject(asset));
            if (properties.Length == 0)
                return string.Empty;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < properties.Length; i++)
                {
                    hash = (hash * 31) + (properties[i].propertyPath?.GetHashCode(StringComparison.Ordinal) ?? 0);
                    hash = (hash * 31) + (properties[i].value?.GetHashCode(StringComparison.Ordinal) ?? 0);
                }

                return hash.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private static ProjectAssetSnapshot[] CaptureProjectAssetSnapshots(HashSet<string> assetPaths)
        {
            List<ProjectAssetSnapshot> captured = new List<ProjectAssetSnapshot>();
            int index = 0;
            int total = assetPaths.Count;

            foreach (string assetPath in assetPaths)
            {
                index++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Play Mode Saver",
                        $"Capturing asset {index}/{total}",
                        total <= 0 ? 1f : index / (float)total))
                {
                    Debug.LogWarning("[Play Mode Saver] Asset capture cancelled.");
                    break;
                }

                if (IsPrefabAssetPath(assetPath))
                    continue;

                UnityEngine.Object asset = LoadAssetForCapture(assetPath);
                if (asset == null)
                    continue;

                PropertySnapshot[] properties = CaptureProperties(new SerializedObject(asset));
                if (properties.Length == 0)
                    continue;

                captured.Add(new ProjectAssetSnapshot
                {
                    assetPath = assetPath,
                    assetType = asset.GetType().AssemblyQualifiedName,
                    properties = properties
                });
            }

            return captured.Count > 0 ? captured.ToArray() : Array.Empty<ProjectAssetSnapshot>();
        }

        private static PrefabAssetSnapshot[] CapturePrefabAssetSnapshots(HashSet<string> prefabAssetPaths)
        {
            List<PrefabAssetSnapshot> captured = new List<PrefabAssetSnapshot>();
            int index = 0;
            int total = prefabAssetPaths.Count;

            foreach (string assetPath in prefabAssetPaths)
            {
                index++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Play Mode Saver",
                        $"Capturing prefab {index}/{total}",
                        total <= 0 ? 1f : index / (float)total))
                {
                    Debug.LogWarning("[Play Mode Saver] Prefab capture cancelled.");
                    break;
                }

                if (PlayModeEditPlayerExclusions.ShouldSkipPrefabAssetCapture(assetPath))
                    continue;

                if (!TryCapturePrefabHierarchy(assetPath, out GameObjectSnapshot[] objects) || objects.Length == 0)
                    continue;

                captured.Add(new PrefabAssetSnapshot
                {
                    assetPath = assetPath,
                    objects = objects
                });
            }

            return captured.Count > 0 ? captured.ToArray() : Array.Empty<PrefabAssetSnapshot>();
        }

        private static bool TryCapturePrefabHierarchy(string assetPath, out GameObjectSnapshot[] objects)
        {
            objects = Array.Empty<GameObjectSnapshot>();

            if (EditorApplication.isPlaying)
            {
                GameObject instanceRoot = FindPrefabInstanceRootInOpenScenes(assetPath);
                if (instanceRoot == null)
                    return false;

                List<GameObjectSnapshot> objectList = new List<GameObjectSnapshot>();
                CaptureHierarchy(instanceRoot.transform, objectList, captureAll: true, allowedPaths: null);
                objects = objectList.ToArray();
                return objects.Length > 0;
            }

            bool loadedViaContents = TryLoadPrefabContents(assetPath, out GameObject prefabRoot);
            if (prefabRoot == null)
                return false;

            try
            {
                List<GameObjectSnapshot> objectList = new List<GameObjectSnapshot>();
                CaptureHierarchy(prefabRoot.transform, objectList, captureAll: true, allowedPaths: null);
                objects = objectList.ToArray();
                return objects.Length > 0;
            }
            finally
            {
                if (loadedViaContents)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static GameObject FindPrefabInstanceRootInOpenScenes(string assetPath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    GameObject found = FindPrefabInstanceRootUnderTransform(roots[r].transform, assetPath);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        private static GameObject FindPrefabInstanceRootUnderTransform(Transform transform, string assetPath)
        {
            if (transform == null)
                return null;

            if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
            {
                GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject);
                if (instanceRoot != null
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot) == assetPath)
                {
                    return instanceRoot;
                }
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject found = FindPrefabInstanceRootUnderTransform(transform.GetChild(i), assetPath);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool TryLoadPrefabContents(string assetPath, out GameObject prefabRoot)
        {
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Play Mode Saver] Could not load prefab contents for '{assetPath}': {exception.Message}");
                prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                return false;
            }
        }

        private static void AddAssetPaths(HashSet<string> assetPaths, string filter, string[] scanRoots = null)
        {
            scanRoots ??= ProjectAssetScanRoots;
            for (int r = 0; r < scanRoots.Length; r++)
            {
                string[] guids = AssetDatabase.FindAssets(filter, new[] { scanRoots[r] });
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (IsProjectAssetPath(assetPath))
                        assetPaths.Add(assetPath);
                }
            }
        }

        private static bool IsPrefabAssetPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath)
                && assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProjectAssetPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath)
                && assetPath.StartsWith("Assets/_Project", StringComparison.Ordinal)
                && !ShouldSkipProjectAsset(assetPath);
        }

        private static bool ShouldSkipProjectAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            for (int i = 0; i < ExcludedProjectAssetPaths.Length; i++)
            {
                if (assetPath.Equals(ExcludedProjectAssetPaths[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static ProjectAssetSnapshot[] FilterExcludedProjectAssetSnapshots(ProjectAssetSnapshot[] snapshots)
        {
            if (snapshots == null || snapshots.Length == 0)
                return snapshots;

            List<ProjectAssetSnapshot> filtered = new List<ProjectAssetSnapshot>(snapshots.Length);
            for (int i = 0; i < snapshots.Length; i++)
            {
                ProjectAssetSnapshot snapshot = snapshots[i];
                if (snapshot == null || ShouldSkipProjectAsset(snapshot.assetPath))
                    continue;

                filtered.Add(snapshot);
            }

            return filtered.ToArray();
        }

        private static UnityEngine.Object LoadAssetForCapture(string assetPath)
        {
            if (loadedAssetLookup != null
                && loadedAssetLookup.TryGetValue(assetPath, out UnityEngine.Object cachedAsset))
            {
                return cachedAsset;
            }

            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        }

        private static void CollectAssetPathsFromComponentSnapshots(
            ComponentPropertySnapshot[] componentSnapshots,
            HashSet<string> assetPaths)
        {
            if (componentSnapshots == null)
                return;

            for (int i = 0; i < componentSnapshots.Length; i++)
            {
                PropertySnapshot[] properties = componentSnapshots[i]?.properties;
                if (properties == null)
                    continue;

                for (int p = 0; p < properties.Length; p++)
                {
                    string value = properties[p]?.value;
                    if (string.IsNullOrEmpty(value) || !value.StartsWith("asset:", StringComparison.Ordinal))
                        continue;

                    string payload = value.Substring("asset:".Length);
                    int typeSeparator = payload.LastIndexOf('|');
                    if (typeSeparator <= 0)
                        continue;

                    string assetPath = payload.Substring(0, typeSeparator);
                    if (!string.IsNullOrEmpty(assetPath))
                        assetPaths.Add(assetPath);
                }
            }
        }

        private static PropertySnapshot[] CaptureProperties(
            SerializedObject serializedObject,
            Component sourceComponent = null)
        {
            PlayModeEditDeepSerializer.PropertySnapshot[] captured =
                PlayModeEditDeepSerializer.CaptureAllProperties(serializedObject, sourceComponent);
            return ConvertFromDeepProperties(captured);
        }

        private static bool ApplyProperties(SerializedObject serializedObject, PropertySnapshot[] properties)
        {
            PlayModeEditDeepSerializer.PropertySnapshot[] deepProperties = ConvertToDeepProperties(properties);
            return PlayModeEditDeepSerializer.ApplyAllProperties(serializedObject, deepProperties, out bool changed) && changed;
        }

        private static PropertySnapshot[] ConvertFromDeepProperties(
            PlayModeEditDeepSerializer.PropertySnapshot[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<PropertySnapshot>();

            PropertySnapshot[] converted = new PropertySnapshot[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                converted[i] = new PropertySnapshot
                {
                    propertyPath = source[i].propertyPath,
                    value = source[i].value
                };
            }

            return converted;
        }

        private static PlayModeEditDeepSerializer.PropertySnapshot[] ConvertToDeepProperties(PropertySnapshot[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<PlayModeEditDeepSerializer.PropertySnapshot>();

            PlayModeEditDeepSerializer.PropertySnapshot[] converted =
                new PlayModeEditDeepSerializer.PropertySnapshot[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                converted[i] = new PlayModeEditDeepSerializer.PropertySnapshot
                {
                    propertyPath = source[i].propertyPath,
                    value = source[i].value
                };
            }

            return converted;
        }

        private static void ApplyCapturedSnapshots()
        {
            if (!File.Exists(SnapshotPath))
                return;

            PlayModeSnapshot snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<PlayModeSnapshot>(File.ReadAllText(SnapshotPath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Play Mode Edits] Could not read snapshot: {exception.Message}");
                DeleteSnapshotFile();
                return;
            }

            ProjectAssetSnapshot[] projectAssets = snapshot.projectAssets;
            if (projectAssets == null || projectAssets.Length == 0)
                projectAssets = ConvertLegacyScriptableObjectSnapshots(snapshot.scriptableObjects);

            projectAssets = FilterExcludedProjectAssetSnapshots(projectAssets);

            bool hasScenes = snapshot.scenes != null && snapshot.scenes.Length > 0;
            bool hasAssets = projectAssets != null && projectAssets.Length > 0;
            bool hasPrefabs = snapshot.prefabAssets != null && snapshot.prefabAssets.Length > 0;
            if (!hasScenes && !hasAssets && !hasPrefabs)
            {
                DeleteSnapshotFile();
                return;
            }

            int appliedObjects = 0;
            int createdObjects = 0;
            int savedScenes = 0;
            int appliedProjectAssets = 0;
            int appliedPrefabAssets = 0;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Play Mode Edits");

            appliedProjectAssets = ApplyProjectAssetSnapshots(projectAssets);
            appliedPrefabAssets = ApplyPrefabAssetSnapshots(snapshot.prefabAssets);
            HashSet<string> appliedPrefabInstanceRoots = new HashSet<string>(StringComparer.Ordinal);

            if (hasScenes)
            {
                for (int i = 0; i < snapshot.scenes.Length; i++)
                {
                    SceneSnapshot sceneSnapshot = snapshot.scenes[i];
                    if (sceneSnapshot == null || string.IsNullOrEmpty(sceneSnapshot.scenePath))
                        continue;

                    Scene scene = EditorSceneManager.GetSceneByPath(sceneSnapshot.scenePath);
                    if (!scene.IsValid() || !scene.isLoaded)
                        continue;

                    Dictionary<string, Transform> lookup = BuildHierarchyLookup(scene);
                    bool sceneChanged = false;

                    if (sceneSnapshot.objects == null || sceneSnapshot.objects.Length == 0)
                        continue;

                    GameObjectSnapshot[] sortedObjects = SortSnapshotsByHierarchyDepth(sceneSnapshot.objects);
                    for (int o = 0; o < sortedObjects.Length; o++)
                    {
                        GameObjectSnapshot objectSnapshot = sortedObjects[o];
                        if (objectSnapshot == null || string.IsNullOrEmpty(objectSnapshot.hierarchyPath))
                            continue;

                        if (!lookup.TryGetValue(objectSnapshot.hierarchyPath, out Transform targetTransform))
                        {
                            targetTransform = CreateMissingTransform(scene, objectSnapshot, lookup);
                            if (targetTransform == null)
                                continue;

                            createdObjects++;
                            lookup[objectSnapshot.hierarchyPath] = targetTransform;
                        }

                        if (ApplyGameObjectSnapshot(targetTransform.gameObject, objectSnapshot, appliedPrefabInstanceRoots))
                        {
                            appliedObjects++;
                            sceneChanged = true;
                        }
                    }

                    if (!sceneChanged)
                        continue;

                    EditorSceneManager.MarkSceneDirty(scene);
                    if (EditorSceneManager.SaveScene(scene))
                        savedScenes++;
                }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            DeleteSnapshotFile();

            if (appliedObjects > 0 || createdObjects > 0 || appliedProjectAssets > 0 || appliedPrefabAssets > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[Play Mode Saver] Applied {appliedObjects} object change(s), created {createdObjects} missing object(s), " +
                    $"saved {savedScenes} scene(s), updated {appliedProjectAssets} data/material/text asset(s), " +
                    $"and {appliedPrefabAssets} prefab asset(s).");
            }
        }

        private static int ApplyPrefabAssetSnapshots(PrefabAssetSnapshot[] prefabAssetSnapshots)
        {
            if (prefabAssetSnapshots == null || prefabAssetSnapshots.Length == 0)
                return 0;

            int applied = 0;
            for (int i = 0; i < prefabAssetSnapshots.Length; i++)
            {
                PrefabAssetSnapshot prefabSnapshot = prefabAssetSnapshots[i];
                if (prefabSnapshot == null || string.IsNullOrEmpty(prefabSnapshot.assetPath))
                    continue;

                if (PlayModeEditPlayerExclusions.ShouldSkipPrefabAssetCapture(prefabSnapshot.assetPath))
                    continue;

                GameObject prefabRoot;
                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(prefabSnapshot.assetPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Play Mode Saver] Could not apply prefab snapshot for '{prefabSnapshot.assetPath}': {exception.Message}");
                    continue;
                }

                if (prefabRoot == null)
                    continue;

                try
                {
                    Dictionary<string, Transform> lookup = BuildHierarchyLookupFromRoot(prefabRoot.transform);
                    bool prefabChanged = false;

                    GameObjectSnapshot[] sortedObjects = SortSnapshotsByHierarchyDepth(prefabSnapshot.objects);
                    for (int o = 0; o < sortedObjects.Length; o++)
                    {
                        GameObjectSnapshot objectSnapshot = sortedObjects[o];
                        if (objectSnapshot == null || string.IsNullOrEmpty(objectSnapshot.hierarchyPath))
                            continue;

                        if (!lookup.TryGetValue(objectSnapshot.hierarchyPath, out Transform targetTransform))
                        {
                            targetTransform = CreateMissingTransformInPrefab(prefabRoot, objectSnapshot, lookup);
                            if (targetTransform == null)
                                continue;

                            lookup[objectSnapshot.hierarchyPath] = targetTransform;
                        }

                        if (ApplyGameObjectSnapshot(targetTransform.gameObject, objectSnapshot, null))
                            prefabChanged = true;
                    }

                    if (!prefabChanged)
                        continue;

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabSnapshot.assetPath);
                    applied++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            return applied;
        }

        private static Dictionary<string, Transform> BuildHierarchyLookupFromRoot(Transform root)
        {
            Dictionary<string, Transform> lookup = new Dictionary<string, Transform>();
            IndexHierarchy(root, lookup);
            return lookup;
        }

        private static Transform CreateMissingTransformInPrefab(
            GameObject prefabRoot,
            GameObjectSnapshot snapshot,
            Dictionary<string, Transform> lookup)
        {
            string parentPath = GetParentHierarchyPath(snapshot.hierarchyPath);
            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath) && !lookup.TryGetValue(parentPath, out parent))
                return null;

            if (parent == null)
                parent = prefabRoot.transform;

            string objectName = GetLeafHierarchyName(snapshot.hierarchyPath);
            GameObject gameObject = snapshot.hasRectTransform
                ? new GameObject(objectName, typeof(RectTransform))
                : new GameObject(objectName);

            gameObject.transform.SetParent(parent, false);
            EnsureComponentsExist(gameObject, snapshot.componentProperties);
            return gameObject.transform;
        }

        private static ProjectAssetSnapshot[] ConvertLegacyScriptableObjectSnapshots(ScriptableObjectSnapshot[] legacySnapshots)
        {
            if (legacySnapshots == null || legacySnapshots.Length == 0)
                return Array.Empty<ProjectAssetSnapshot>();

            ProjectAssetSnapshot[] converted = new ProjectAssetSnapshot[legacySnapshots.Length];
            for (int i = 0; i < legacySnapshots.Length; i++)
            {
                converted[i] = new ProjectAssetSnapshot
                {
                    assetPath = legacySnapshots[i].assetPath,
                    assetType = legacySnapshots[i].assetType,
                    properties = legacySnapshots[i].properties
                };
            }

            return converted;
        }

        private static GameObjectSnapshot[] SortSnapshotsByHierarchyDepth(GameObjectSnapshot[] objects)
        {
            GameObjectSnapshot[] sorted = (GameObjectSnapshot[])objects.Clone();
            Array.Sort(sorted, (left, right) =>
                GetHierarchyDepth(left.hierarchyPath).CompareTo(GetHierarchyDepth(right.hierarchyPath)));
            return sorted;
        }

        private static int GetHierarchyDepth(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
                return 0;

            int depth = 1;
            for (int i = 0; i < hierarchyPath.Length; i++)
            {
                if (hierarchyPath[i] == '/')
                    depth++;
            }

            return depth;
        }

        private static Transform CreateMissingTransform(
            Scene scene,
            GameObjectSnapshot snapshot,
            Dictionary<string, Transform> lookup)
        {
            string parentPath = GetParentHierarchyPath(snapshot.hierarchyPath);
            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath) && !lookup.TryGetValue(parentPath, out parent))
                return null;

            string objectName = GetLeafHierarchyName(snapshot.hierarchyPath);
            GameObject gameObject = snapshot.hasRectTransform
                ? new GameObject(objectName, typeof(RectTransform))
                : new GameObject(objectName);

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Play Mode Saved Object");

            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            else
                SceneManager.MoveGameObjectToScene(gameObject, scene);

            EnsureComponentsExist(gameObject, snapshot.componentProperties);
            return gameObject.transform;
        }

        private static void EnsureComponentsExist(GameObject target, ComponentPropertySnapshot[] componentSnapshots)
        {
            if (componentSnapshots == null)
                return;

            for (int i = 0; i < componentSnapshots.Length; i++)
            {
                ComponentPropertySnapshot componentSnapshot = componentSnapshots[i];
                if (componentSnapshot == null || string.IsNullOrEmpty(componentSnapshot.componentType))
                    continue;

                Type componentType = Type.GetType(componentSnapshot.componentType);
                if (componentType == null || target.GetComponent(componentType) != null)
                    continue;

                Undo.AddComponent(target, componentType);
            }
        }

        private static string GetParentHierarchyPath(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
                return string.Empty;

            int lastSlash = hierarchyPath.LastIndexOf('/');
            return lastSlash <= 0 ? string.Empty : hierarchyPath.Substring(0, lastSlash);
        }

        private static string GetLeafHierarchyName(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
                return "PlayModeSavedObject";

            int lastSlash = hierarchyPath.LastIndexOf('/');
            return lastSlash < 0 ? hierarchyPath : hierarchyPath.Substring(lastSlash + 1);
        }

        private static int ApplyProjectAssetSnapshots(ProjectAssetSnapshot[] projectAssetSnapshots)
        {
            if (projectAssetSnapshots == null || projectAssetSnapshots.Length == 0)
                return 0;

            int applied = 0;
            for (int i = 0; i < projectAssetSnapshots.Length; i++)
            {
                ProjectAssetSnapshot snapshot = projectAssetSnapshots[i];
                if (snapshot == null || string.IsNullOrEmpty(snapshot.assetPath))
                    continue;

                if (ShouldSkipProjectAsset(snapshot.assetPath))
                    continue;

                Type assetType = string.IsNullOrEmpty(snapshot.assetType)
                    ? typeof(UnityEngine.Object)
                    : Type.GetType(snapshot.assetType) ?? typeof(UnityEngine.Object);

                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(snapshot.assetPath, assetType);
                if (asset == null)
                    continue;

                SerializedObject serializedObject = new SerializedObject(asset);
                Undo.RecordObject(asset, "Apply Play Mode Project Asset");

                if (!ApplyProperties(serializedObject, snapshot.properties))
                    continue;

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                applied++;
            }

            return applied;
        }

        private static Dictionary<string, Transform> BuildHierarchyLookup(Scene scene)
        {
            Dictionary<string, Transform> lookup = new Dictionary<string, Transform>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                IndexHierarchy(roots[i].transform, lookup);
            return lookup;
        }

        private static void IndexHierarchy(Transform transform, Dictionary<string, Transform> lookup)
        {
            if (transform == null)
                return;

            string path = GetHierarchyPath(transform);
            lookup[path] = transform;

            for (int i = 0; i < transform.childCount; i++)
                IndexHierarchy(transform.GetChild(i), lookup);
        }

        private static bool ApplyGameObjectSnapshot(
            GameObject target,
            GameObjectSnapshot snapshot,
            HashSet<string> appliedPrefabInstanceRoots)
        {
            if (PlayModeEditPlayerExclusions.ShouldSkipApply(target.transform))
                return false;

            bool changed = false;
            Transform transform = target.transform;

            Undo.RecordObject(transform, "Apply Play Mode Transform");
            if (snapshot.hasRectTransform && transform is RectTransform rectTransform)
            {
                if (rectTransform.anchorMin != snapshot.anchorMin)
                {
                    rectTransform.anchorMin = snapshot.anchorMin;
                    changed = true;
                }

                if (rectTransform.anchorMax != snapshot.anchorMax)
                {
                    rectTransform.anchorMax = snapshot.anchorMax;
                    changed = true;
                }

                if (rectTransform.pivot != snapshot.pivot)
                {
                    rectTransform.pivot = snapshot.pivot;
                    changed = true;
                }

                if (rectTransform.anchoredPosition != snapshot.anchoredPosition)
                {
                    rectTransform.anchoredPosition = snapshot.anchoredPosition;
                    changed = true;
                }

                if (rectTransform.anchoredPosition3D != snapshot.anchoredPosition3D)
                {
                    rectTransform.anchoredPosition3D = snapshot.anchoredPosition3D;
                    changed = true;
                }

                if (rectTransform.sizeDelta != snapshot.sizeDelta)
                {
                    rectTransform.sizeDelta = snapshot.sizeDelta;
                    changed = true;
                }

                if (rectTransform.offsetMin != snapshot.offsetMin)
                {
                    rectTransform.offsetMin = snapshot.offsetMin;
                    changed = true;
                }

                if (rectTransform.offsetMax != snapshot.offsetMax)
                {
                    rectTransform.offsetMax = snapshot.offsetMax;
                    changed = true;
                }

                if (rectTransform.localRotation != snapshot.localRotation)
                {
                    rectTransform.localRotation = snapshot.localRotation;
                    changed = true;
                }

                if (rectTransform.localScale != snapshot.localScale)
                {
                    rectTransform.localScale = snapshot.localScale;
                    changed = true;
                }
            }
            else
            {
                if (transform.localPosition != snapshot.localPosition)
                {
                    transform.localPosition = snapshot.localPosition;
                    changed = true;
                }

                if (transform.localRotation != snapshot.localRotation)
                {
                    transform.localRotation = snapshot.localRotation;
                    changed = true;
                }

                if (transform.localScale != snapshot.localScale)
                {
                    transform.localScale = snapshot.localScale;
                    changed = true;
                }
            }

            if (target.activeSelf != snapshot.activeSelf)
            {
                target.SetActive(snapshot.activeSelf);
                changed = true;
            }

            Undo.RecordObject(target, "Apply Play Mode GameObject");
            if (!string.IsNullOrEmpty(snapshot.tag) && !string.Equals(target.tag, snapshot.tag, StringComparison.Ordinal))
            {
                try
                {
                    target.tag = snapshot.tag;
                    changed = true;
                }
                catch (UnityException exception)
                {
                    Debug.LogWarning($"[Play Mode Saver] Could not apply tag '{snapshot.tag}' on '{target.name}': {exception.Message}");
                }
            }

            if (target.layer != snapshot.layer)
            {
                target.layer = snapshot.layer;
                changed = true;
            }

            if (target.isStatic != snapshot.isStatic)
            {
                target.isStatic = snapshot.isStatic;
                changed = true;
            }

            if (snapshot.componentProperties != null)
                changed |= ApplyComponentProperties(target, snapshot.componentProperties);

            changed |= TryApplyPrefabInstanceOverrides(target, appliedPrefabInstanceRoots);

            if (changed)
                EditorUtility.SetDirty(target);

            return changed;
        }

        private static bool TryApplyPrefabInstanceOverrides(
            GameObject target,
            HashSet<string> appliedPrefabInstanceRoots)
        {
            if (appliedPrefabInstanceRoots == null || !PrefabUtility.IsPartOfPrefabInstance(target))
                return false;

            GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
            if (outermostRoot == null)
                return false;

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(outermostRoot);
            if (string.IsNullOrEmpty(prefabPath) || !IsProjectAssetPath(prefabPath))
                return false;

            if (appliedPrefabInstanceRoots.Contains(prefabPath))
                return false;

            if (!PrefabUtility.HasPrefabInstanceAnyOverrides(outermostRoot, false))
                return false;

            PrefabUtility.ApplyPrefabInstance(outermostRoot, InteractionMode.UserAction);
            appliedPrefabInstanceRoots.Add(prefabPath);
            return true;
        }

        private static bool ApplyComponentProperties(GameObject target, ComponentPropertySnapshot[] componentSnapshots)
        {
            bool changed = false;

            for (int i = 0; i < componentSnapshots.Length; i++)
            {
                ComponentPropertySnapshot componentSnapshot = componentSnapshots[i];
                if (componentSnapshot == null || string.IsNullOrEmpty(componentSnapshot.componentType))
                    continue;

                Type componentType = Type.GetType(componentSnapshot.componentType);
                if (componentType == null)
                    continue;

                Component component = target.GetComponent(componentType);
                if (component == null)
                    component = Undo.AddComponent(target, componentType);

                if (component == null)
                    continue;

                SerializedObject serializedObject = new SerializedObject(component);
                Undo.RecordObject(component, "Apply Play Mode Component");

                if (!ApplyProperties(serializedObject, componentSnapshot.properties))
                    continue;

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
                changed = true;

                if (PrefabUtility.IsPartOfPrefabInstance(target))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }

            return changed;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            if (transform.parent == null)
                return transform.name;

            return GetHierarchyPath(transform.parent) + "/" + transform.name;
        }

        private static void DeleteSnapshotFile()
        {
            if (File.Exists(SnapshotPath))
                File.Delete(SnapshotPath);
        }

        [Serializable]
        private class PlayModeSnapshot
        {
            public string capturedUtc;
            public SceneSnapshot[] scenes;
            public ProjectAssetSnapshot[] projectAssets;
            public PrefabAssetSnapshot[] prefabAssets;
            public ScriptableObjectSnapshot[] scriptableObjects;
        }

        [Serializable]
        private class PrefabAssetSnapshot
        {
            public string assetPath;
            public GameObjectSnapshot[] objects;
        }

        [Serializable]
        private class SceneSnapshot
        {
            public string scenePath;
            public GameObjectSnapshot[] objects;
        }

        [Serializable]
        private class GameObjectSnapshot
        {
            public string hierarchyPath;
            public bool activeSelf;
            public string tag;
            public int layer;
            public bool isStatic;
            public bool hasRectTransform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector3 anchoredPosition3D;
            public Vector2 sizeDelta;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
            public ComponentPropertySnapshot[] componentProperties;
        }

        [Serializable]
        private class ProjectAssetSnapshot
        {
            public string assetPath;
            public string assetType;
            public PropertySnapshot[] properties;
        }

        [Serializable]
        private class ScriptableObjectSnapshot
        {
            public string assetPath;
            public string assetType;
            public PropertySnapshot[] properties;
        }

        [Serializable]
        private class ComponentPropertySnapshot
        {
            public string componentType;
            public PropertySnapshot[] properties;
        }

        [Serializable]
        private class PropertySnapshot
        {
            public string propertyPath;
            public string value;
        }
    }
}
