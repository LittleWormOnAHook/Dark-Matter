#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Crafting;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Genesis Studio > Player > Pickup Items: global pickup tunables (DMPickupProfile), the library of world pickup
    /// prefabs with their per-prefab editables, and the Item Data creator (ItemData + world pickup prefab).
    /// </summary>
    public sealed class DMStudioPickupItemsPanel
    {
        static readonly string[] Sections = { "Settings", "Library", "Item / Pickup Creator" };
        static readonly string[] KindLabels = { "All", "Items", "Blueprints" };
        static readonly string[] LibraryFolders =
        {
            "Assets/_Project/Prefabs/Items",
            "Assets/_Project/Prefabs/Crafting",
            "Assets/_Project/Prefabs/Tools",
            "Assets/_Project/Prefabs/Weapons",
        };

        sealed class Entry
        {
            public GameObject Prefab;
            public string Path;
            public MonoBehaviour Pickup;
            public string Label;
            public string Detail;
            public bool IsBlueprint;
        }

        readonly ItemDataCreatorPanel creator = new ItemDataCreatorPanel();
        readonly List<Entry> library = new List<Entry>();
        int section;
        int kind;
        string search = string.Empty;
        bool scanned;
        string expandedPath;
        UnityEditor.Editor profileEditor;
        UnityEditor.Editor pickupEditor;
        UnityEngine.Object pickupEditorTarget;

        public void Dispose()
        {
            DestroyEditor(ref profileEditor);
            DestroyEditor(ref pickupEditor);
            pickupEditorTarget = null;
        }

        public void Draw()
        {
            section = GUILayout.Toolbar(section, Sections, GUILayout.Height(24f));
            EditorGUILayout.Space(8f);
            switch (section)
            {
                case 0:
                    DrawSettings();
                    break;
                case 1:
                    DrawLibrary();
                    break;
                default:
                    creator.Draw();
                    break;
            }
        }

        void DrawSettings()
        {
            DMPickupProfile profile = AssetDatabase.LoadAssetAtPath<DMPickupProfile>(DMPickupProfile.AssetPath);
            if (profile == null)
            {
                EditorGUILayout.HelpBox("No pickup profile at " + DMPickupProfile.AssetPath + ".", MessageType.Warning);
                if (GUILayout.Button("Create Pickup Profile", GUILayout.Height(28f)))
                    profile = CreateProfile();
                if (profile == null)
                    return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Profile", GUILayout.Height(22f), GUILayout.Width(110f)))
                EditorGUIUtility.PingObject(profile);
            EditorGUILayout.LabelField("Global values for every item and blueprint pickup.", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            UnityEditor.Editor.CreateCachedEditor(profile, null, ref profileEditor);
            profileEditor.OnInspectorGUI();
        }

        static DMPickupProfile CreateProfile()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Interaction"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Interaction");
            var profile = ScriptableObject.CreateInstance<DMPickupProfile>();
            AssetDatabase.CreateAsset(profile, DMPickupProfile.AssetPath);
            AssetDatabase.SaveAssets();
            return profile;
        }

        void DrawLibrary()
        {
            if (!scanned)
                Scan();

            EditorGUILayout.BeginHorizontal();
            kind = GUILayout.Toolbar(kind, KindLabels, GUILayout.Width(240f));
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Rescan", GUILayout.Width(70f)))
                Scan();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                library.Count + " pickup prefabs in Items, Crafting, Tools and Weapons. Expand one to edit its pickup values.",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            for (int i = 0; i < library.Count; i++)
            {
                Entry entry = library[i];
                if (entry.Prefab == null)
                    continue;
                if ((kind == 1 && entry.IsBlueprint) || (kind == 2 && !entry.IsBlueprint))
                    continue;
                if (!string.IsNullOrEmpty(search)
                    && entry.Label.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0
                    && entry.Detail.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool open = expandedPath == entry.Path;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                bool nowOpen = EditorGUILayout.Foldout(open, entry.Label + (entry.IsBlueprint ? "  (Blueprint)" : string.Empty), true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(entry.Detail, EditorStyles.miniLabel, GUILayout.Width(200f));
                if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                    EditorGUIUtility.PingObject(entry.Prefab);
                if (GUILayout.Button("Open", GUILayout.Width(48f)))
                    AssetDatabase.OpenAsset(entry.Prefab);
                EditorGUILayout.EndHorizontal();
                if (nowOpen != open)
                    expandedPath = nowOpen ? entry.Path : null;

                if (nowOpen && entry.Pickup != null)
                {
                    if (pickupEditorTarget != entry.Pickup)
                    {
                        DestroyEditor(ref pickupEditor);
                        pickupEditor = UnityEditor.Editor.CreateEditor(entry.Pickup);
                        pickupEditorTarget = entry.Pickup;
                    }

                    EditorGUI.BeginChangeCheck();
                    pickupEditor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck())
                        EditorUtility.SetDirty(entry.Prefab);
                }

                EditorGUILayout.EndVertical();
            }
        }

        void Scan()
        {
            library.Clear();
            scanned = true;
            var folders = new List<string>();
            foreach (string folder in LibraryFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                    folders.Add(folder);
            }

            if (folders.Count == 0)
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;
                ItemPickup item = prefab.GetComponentInChildren<ItemPickup>(true);
                RecipePickup recipe = item == null ? prefab.GetComponentInChildren<RecipePickup>(true) : null;
                if (item == null && recipe == null)
                    continue;

                library.Add(new Entry
                {
                    Prefab = prefab,
                    Path = path,
                    Pickup = item != null ? (MonoBehaviour)item : recipe,
                    Label = prefab.name,
                    IsBlueprint = recipe != null,
                    Detail = item != null
                        ? (item.itemData != null ? item.itemData.name : "no ItemData") + " x" + item.amount
                        : "blueprint " + recipe.RecipeId,
                });
            }

            library.Sort((a, b) => string.Compare(a.Label, b.Label, System.StringComparison.OrdinalIgnoreCase));
        }

        static void DestroyEditor(ref UnityEditor.Editor editor)
        {
            if (editor != null)
                UnityEngine.Object.DestroyImmediate(editor);
            editor = null;
        }
    }
}
#endif
