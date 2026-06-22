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
        private const string SnapshotFileName = "play-mode-snapshot.json";
        private const string MenuPath = SurvivalPioneerEditorMenus.Maintenance + "Persist Play Mode Edits";

        private static bool pendingApply;

        static PlayModeEditPersistence()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, false);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        [MenuItem(MenuPath, false, 3)]
        public static void Toggle()
        {
            Enabled = !Enabled;
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
            if (!Enabled)
                return;

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    CaptureOpenScenes();
                    pendingApply = true;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if (!pendingApply)
                        return;

                    pendingApply = false;
                    EditorApplication.delayCall += ApplyCapturedSnapshots;
                    break;
            }
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

        private static void CaptureOpenScenes()
        {
            PlayModeSnapshot snapshot = new PlayModeSnapshot
            {
                capturedUtc = DateTime.UtcNow.ToString("o"),
                scenes = Array.Empty<SceneSnapshot>()
            };

            List<SceneSnapshot> sceneList = new List<SceneSnapshot>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                    continue;

                List<GameObjectSnapshot> objectList = new List<GameObjectSnapshot>();
                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    CaptureHierarchy(roots[r].transform, objectList);

                if (objectList.Count == 0)
                    continue;

                sceneList.Add(new SceneSnapshot
                {
                    scenePath = scene.path,
                    objects = objectList.ToArray()
                });
            }

            if (sceneList.Count == 0)
            {
                DeleteSnapshotFile();
                return;
            }

            snapshot.scenes = sceneList.ToArray();
            string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
            File.WriteAllText(SnapshotPath, json);
        }

        private static void CaptureHierarchy(Transform transform, List<GameObjectSnapshot> output)
        {
            if (transform == null)
                return;

            GameObjectSnapshot entry = new GameObjectSnapshot
            {
                hierarchyPath = GetHierarchyPath(transform),
                activeSelf = transform.gameObject.activeSelf,
                localPosition = transform.localPosition,
                localRotation = transform.localRotation,
                localScale = transform.localScale,
                componentProperties = CaptureComponentProperties(transform.gameObject)
            };
            output.Add(entry);

            for (int i = 0; i < transform.childCount; i++)
                CaptureHierarchy(transform.GetChild(i), output);
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
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                List<PropertySnapshot> properties = new List<PropertySnapshot>();

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.propertyPath == "m_Script")
                        continue;

                    if (!TryExportProperty(iterator, out string value))
                        continue;

                    properties.Add(new PropertySnapshot
                    {
                        propertyPath = iterator.propertyPath,
                        value = value
                    });
                }

                if (properties.Count == 0)
                    continue;

                captured.Add(new ComponentPropertySnapshot
                {
                    componentType = component.GetType().AssemblyQualifiedName,
                    properties = properties.ToArray()
                });
            }

            return captured.Count > 0 ? captured.ToArray() : Array.Empty<ComponentPropertySnapshot>();
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

            if (snapshot?.scenes == null || snapshot.scenes.Length == 0)
            {
                DeleteSnapshotFile();
                return;
            }

            int appliedObjects = 0;
            int savedScenes = 0;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Play Mode Edits");

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

                if (sceneSnapshot.objects == null)
                    continue;

                for (int o = 0; o < sceneSnapshot.objects.Length; o++)
                {
                    GameObjectSnapshot objectSnapshot = sceneSnapshot.objects[o];
                    if (objectSnapshot == null || string.IsNullOrEmpty(objectSnapshot.hierarchyPath))
                        continue;

                    if (!lookup.TryGetValue(objectSnapshot.hierarchyPath, out Transform targetTransform))
                        continue;

                    if (ApplyGameObjectSnapshot(targetTransform.gameObject, objectSnapshot))
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

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            DeleteSnapshotFile();

            if (appliedObjects > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[Play Mode Edits] Applied {appliedObjects} object change(s) across {savedScenes} scene(s).");
            }
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

        private static bool ApplyGameObjectSnapshot(GameObject target, GameObjectSnapshot snapshot)
        {
            bool changed = false;
            Transform transform = target.transform;

            Undo.RecordObject(transform, "Apply Play Mode Transform");
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

            if (target.activeSelf != snapshot.activeSelf)
            {
                target.SetActive(snapshot.activeSelf);
                changed = true;
            }

            if (snapshot.componentProperties != null)
                changed |= ApplyComponentProperties(target, snapshot.componentProperties);

            if (changed)
                EditorUtility.SetDirty(target);

            return changed;
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
                    continue;

                SerializedObject serializedObject = new SerializedObject(component);
                Undo.RecordObject(component, "Apply Play Mode Component");

                bool componentChanged = false;
                PropertySnapshot[] properties = componentSnapshot.properties;
                for (int p = 0; p < properties.Length; p++)
                {
                    PropertySnapshot propertySnapshot = properties[p];
                    if (propertySnapshot == null || string.IsNullOrEmpty(propertySnapshot.propertyPath))
                        continue;

                    SerializedProperty property = serializedObject.FindProperty(propertySnapshot.propertyPath);
                    if (property == null)
                        continue;

                    if (TryImportProperty(property, propertySnapshot.value))
                        componentChanged = true;
                }

                if (!componentChanged)
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

        private static bool TryExportProperty(SerializedProperty property, out string value)
        {
            value = null;
            if (property == null)
                return false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    value = property.intValue.ToString();
                    return true;
                case SerializedPropertyType.Boolean:
                    value = property.boolValue ? "1" : "0";
                    return true;
                case SerializedPropertyType.Float:
                    value = property.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.String:
                    value = property.stringValue ?? string.Empty;
                    return true;
                case SerializedPropertyType.Color:
                    Color color = property.colorValue;
                    value = $"{color.r},{color.g},{color.b},{color.a}";
                    return true;
                case SerializedPropertyType.Vector2:
                    Vector2 v2 = property.vector2Value;
                    value = $"{v2.x},{v2.y}";
                    return true;
                case SerializedPropertyType.Vector3:
                    Vector3 v3 = property.vector3Value;
                    value = $"{v3.x},{v3.y},{v3.z}";
                    return true;
                case SerializedPropertyType.Vector4:
                    Vector4 v4 = property.vector4Value;
                    value = $"{v4.x},{v4.y},{v4.z},{v4.w}";
                    return true;
                case SerializedPropertyType.Quaternion:
                    Quaternion q = property.quaternionValue;
                    value = $"{q.x},{q.y},{q.z},{q.w}";
                    return true;
                case SerializedPropertyType.Enum:
                    value = property.enumValueIndex.ToString();
                    return true;
                case SerializedPropertyType.Vector2Int:
                    Vector2Int v2i = property.vector2IntValue;
                    value = $"{v2i.x},{v2i.y}";
                    return true;
                case SerializedPropertyType.Vector3Int:
                    Vector3Int v3i = property.vector3IntValue;
                    value = $"{v3i.x},{v3i.y},{v3i.z}";
                    return true;
                case SerializedPropertyType.Rect:
                    Rect rect = property.rectValue;
                    value = $"{rect.x},{rect.y},{rect.width},{rect.height}";
                    return true;
                case SerializedPropertyType.Bounds:
                    Bounds bounds = property.boundsValue;
                    value = $"{bounds.center.x},{bounds.center.y},{bounds.center.z}|{bounds.size.x},{bounds.size.y},{bounds.size.z}";
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryImportProperty(SerializedProperty property, string value)
        {
            if (property == null || value == null)
                return false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (!int.TryParse(value, out int intValue))
                        return false;
                    if (property.intValue == intValue)
                        return false;
                    property.intValue = intValue;
                    return true;
                case SerializedPropertyType.Boolean:
                    bool boolValue = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (property.boolValue == boolValue)
                        return false;
                    property.boolValue = boolValue;
                    return true;
                case SerializedPropertyType.Float:
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                        return false;
                    if (Mathf.Approximately(property.floatValue, floatValue))
                        return false;
                    property.floatValue = floatValue;
                    return true;
                case SerializedPropertyType.String:
                    if (property.stringValue == value)
                        return false;
                    property.stringValue = value;
                    return true;
                case SerializedPropertyType.Color:
                    if (!TryParseFloatCsv(value, 4, out float[] colorParts))
                        return false;
                    Color color = new Color(colorParts[0], colorParts[1], colorParts[2], colorParts[3]);
                    if (property.colorValue == color)
                        return false;
                    property.colorValue = color;
                    return true;
                case SerializedPropertyType.Vector2:
                    if (!TryParseFloatCsv(value, 2, out float[] v2Parts))
                        return false;
                    Vector2 vector2 = new Vector2(v2Parts[0], v2Parts[1]);
                    if (property.vector2Value == vector2)
                        return false;
                    property.vector2Value = vector2;
                    return true;
                case SerializedPropertyType.Vector3:
                    if (!TryParseFloatCsv(value, 3, out float[] v3Parts))
                        return false;
                    Vector3 vector3 = new Vector3(v3Parts[0], v3Parts[1], v3Parts[2]);
                    if (property.vector3Value == vector3)
                        return false;
                    property.vector3Value = vector3;
                    return true;
                case SerializedPropertyType.Vector4:
                    if (!TryParseFloatCsv(value, 4, out float[] v4Parts))
                        return false;
                    Vector4 vector4 = new Vector4(v4Parts[0], v4Parts[1], v4Parts[2], v4Parts[3]);
                    if (property.vector4Value == vector4)
                        return false;
                    property.vector4Value = vector4;
                    return true;
                case SerializedPropertyType.Quaternion:
                    if (!TryParseFloatCsv(value, 4, out float[] qParts))
                        return false;
                    Quaternion quaternion = new Quaternion(qParts[0], qParts[1], qParts[2], qParts[3]);
                    if (property.quaternionValue == quaternion)
                        return false;
                    property.quaternionValue = quaternion;
                    return true;
                case SerializedPropertyType.Enum:
                    if (!int.TryParse(value, out int enumIndex))
                        return false;
                    if (property.enumValueIndex == enumIndex)
                        return false;
                    property.enumValueIndex = enumIndex;
                    return true;
                case SerializedPropertyType.Vector2Int:
                    if (!TryParseIntCsv(value, 2, out int[] v2iParts))
                        return false;
                    Vector2Int vector2Int = new Vector2Int(v2iParts[0], v2iParts[1]);
                    if (property.vector2IntValue == vector2Int)
                        return false;
                    property.vector2IntValue = vector2Int;
                    return true;
                case SerializedPropertyType.Vector3Int:
                    if (!TryParseIntCsv(value, 3, out int[] v3iParts))
                        return false;
                    Vector3Int vector3Int = new Vector3Int(v3iParts[0], v3iParts[1], v3iParts[2]);
                    if (property.vector3IntValue == vector3Int)
                        return false;
                    property.vector3IntValue = vector3Int;
                    return true;
                case SerializedPropertyType.Rect:
                    if (!TryParseFloatCsv(value, 4, out float[] rectParts))
                        return false;
                    Rect rect = new Rect(rectParts[0], rectParts[1], rectParts[2], rectParts[3]);
                    if (property.rectValue == rect)
                        return false;
                    property.rectValue = rect;
                    return true;
                case SerializedPropertyType.Bounds:
                {
                    string[] boundsGroups = value.Split('|');
                    if (boundsGroups.Length != 2
                        || !TryParseFloatCsv(boundsGroups[0], 3, out float[] centerParts)
                        || !TryParseFloatCsv(boundsGroups[1], 3, out float[] sizeParts))
                        return false;

                    Bounds bounds = new Bounds(
                        new Vector3(centerParts[0], centerParts[1], centerParts[2]),
                        new Vector3(sizeParts[0], sizeParts[1], sizeParts[2]));
                    if (property.boundsValue.center == bounds.center && property.boundsValue.size == bounds.size)
                        return false;
                    property.boundsValue = bounds;
                    return true;
                }
                default:
                    return false;
            }
        }

        private static bool TryParseFloatCsv(string value, int expectedCount, out float[] parts)
        {
            parts = null;
            if (string.IsNullOrEmpty(value))
                return false;

            string[] tokens = value.Split(',');
            if (tokens.Length != expectedCount)
                return false;

            parts = new float[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!float.TryParse(tokens[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parts[i]))
                    return false;
            }

            return true;
        }

        private static bool TryParseIntCsv(string value, int expectedCount, out int[] parts)
        {
            parts = null;
            if (string.IsNullOrEmpty(value))
                return false;

            string[] tokens = value.Split(',');
            if (tokens.Length != expectedCount)
                return false;

            parts = new int[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!int.TryParse(tokens[i], out parts[i]))
                    return false;
            }

            return true;
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
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public ComponentPropertySnapshot[] componentProperties;
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
