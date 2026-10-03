#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>Library asset, icons, prefab writing and scene placement for the Rocks and Cliffs studio.</summary>
    public static class PcgStudioLibraryUtil
    {
        /// <summary>DragAndDrop generic-data key carrying a library entry id.</summary>
        public const string DragKey = "GenesisPCG.StudioLibraryEntry";

        /// <summary>Placing from the library rolls a new variation (reseed-on-paste rules: locked seeds are kept).</summary>
        public static bool NewVariationOnPlace
        {
            get => EditorPrefs.GetBool("GenesisPCG.Studio.NewVariationOnPlace", true);
            set => EditorPrefs.SetBool("GenesisPCG.Studio.NewVariationOnPlace", value);
        }

        public static PcgAssetLibrary Load(bool create)
        {
            var lib = AssetDatabase.LoadAssetAtPath<PcgAssetLibrary>(PcgStudioExtensions.LibraryPath);
            if (lib != null || !create) return lib;
            PcgStudioExtensions.EnsureFolder(PcgStudioExtensions.LibraryFolder);
            lib = ScriptableObject.CreateInstance<PcgAssetLibrary>();
            AssetDatabase.CreateAsset(lib, PcgStudioExtensions.LibraryPath);
            AssetDatabase.SaveAssetIfDirty(lib);
            return lib;
        }

        public static void Save(PcgAssetLibrary lib)
        {
            if (lib == null) return;
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssetIfDirty(lib);
            lib.NotifyChanged();
        }

        // ------------------------------------------------------------------------------------------------
        // Entries

        public static PcgAssetLibrary.Entry AddOrUpdate(PcgAssetLibrary lib, GameObject prefab, string category, DmRockPreset preset,
                                                        DmRockStyle style, int seed, float yaw, float pitch, float zoom)
        {
            if (lib == null || prefab == null) return null;
            Undo.RecordObject(lib, "Save to PCG Library");
            PcgAssetLibrary.Entry e = lib.FindByPrefab(prefab);
            if (e == null)
            {
                e = new PcgAssetLibrary.Entry { displayName = prefab.name, createdUtc = DateTime.UtcNow.ToString("o") };
                lib.entries.Add(e);
            }
            e.prefab = prefab;
            e.category = string.IsNullOrEmpty(category) ? "Rocks" : category;
            e.preset = preset;
            e.style = style;
            e.seed = seed;
            e.iconYaw = yaw; e.iconPitch = pitch; e.iconZoom = zoom;
            Save(lib);
            return e;
        }

        public static void Rename(PcgAssetLibrary lib, PcgAssetLibrary.Entry e, string name)
        {
            if (lib == null || e == null) return;
            Undo.RecordObject(lib, "Rename PCG Library Entry");
            e.displayName = (name ?? "").Trim();
            Save(lib);
        }

        /// <summary>Removes the entry (and its icon PNG, which the library owns). The prefab is never deleted.</summary>
        public static void Remove(PcgAssetLibrary lib, PcgAssetLibrary.Entry e)
        {
            if (lib == null || e == null) return;
            string icon = e.icon != null ? AssetDatabase.GetAssetPath(e.icon) : null;
            Undo.RecordObject(lib, "Remove PCG Library Entry");
            lib.entries.Remove(e);
            Save(lib);
            if (!string.IsNullOrEmpty(icon) && icon.StartsWith(PcgStudioExtensions.IconFolder + "/", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(icon);
        }

        // ------------------------------------------------------------------------------------------------
        // Icons

        /// <summary>Writes <paramref name="tex"/> as the entry's 512 PNG icon (imported uncompressed-quality, no mips) and assigns it.</summary>
        public static Texture2D SaveIcon(PcgAssetLibrary lib, PcgAssetLibrary.Entry e, Texture2D tex)
        {
            if (lib == null || e == null || tex == null) return null;
            PcgStudioExtensions.EnsureFolder(PcgStudioExtensions.IconFolder);
            string path = $"{PcgStudioExtensions.IconFolder}/Icon_{e.id}.png";
            string full = Path.GetFullPath(path);
            bool existed = File.Exists(full);
            File.WriteAllBytes(full, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (!existed && AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.sRGBTexture = true;
                ti.alphaIsTransparency = false;
                ti.maxTextureSize = PcgIconView.IconSize;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Undo.RecordObject(lib, "PCG Library Icon");
            e.icon = icon;
            Save(lib);
            return icon;
        }

        /// <summary>Renders <paramref name="prefab"/> offscreen from the given orbit (no on-screen view needed).</summary>
        public static Texture2D RenderPrefabIcon(GameObject prefab, float yaw, float pitch, float zoom, out bool complete)
        {
            complete = false;
            if (prefab == null) return null;
            var view = new PcgIconView();
            try
            {
                view.SetSubject(() => InstantiateForPreview(prefab));
                view.SetView(yaw, pitch, zoom);
                return view.Capture(out complete);
            }
            finally { view.Release(); }
        }

        /// <summary>Re-renders an entry's icon from its saved angle. Retries a few times while shaders compile asynchronously.</summary>
        public static void RerenderIcon(PcgAssetLibrary lib, PcgAssetLibrary.Entry e, int attempts = 8, Action<bool> done = null)
        {
            if (lib == null || e == null || e.prefab == null) { done?.Invoke(false); return; }
            Texture2D tex = RenderPrefabIcon(e.prefab, e.iconYaw, e.iconPitch, e.iconZoom, out bool complete);
            if (tex == null) { done?.Invoke(false); return; }
            try { SaveIcon(lib, e, tex); }
            finally { Object.DestroyImmediate(tex); }
            if (complete || attempts <= 1) { done?.Invoke(complete); return; }
            double at = EditorApplication.timeSinceStartup + 0.75;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (EditorApplication.timeSinceStartup < at || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                EditorApplication.update -= tick;
                RerenderIcon(lib, e, attempts - 1, done);
            };
            EditorApplication.update += tick;
        }

        /// <summary>
        /// Inactive, hidden copy of <paramref name="prefab"/> for a preview scene: rocks show their live build on flat ground,
        /// never bake, never raycast the open scenes and never get a per-rock blend asset.
        /// </summary>
        public static GameObject InstantiateForPreview(GameObject prefab)
        {
            if (prefab == null) return null;
            GameObject holder = EditorUtility.CreateGameObjectWithHideFlags("PcgStudioPreview", HideFlags.HideAndDontSave);
            holder.SetActive(false);
            var inst = (GameObject)Object.Instantiate(prefab, holder.transform, false);
            inst.name = prefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            PcgSeededPasteWatcher.Suppress(inst);
            foreach (Transform t in holder.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.HideAndDontSave;
            foreach (DmRockCombiner c in holder.GetComponentsInChildren<DmRockCombiner>(true))
                MakePreviewSafe(c, true);
            return holder;
        }

        /// <summary>Preview-scene rules for a combiner (call while it is inactive).</summary>
        public static void MakePreviewSafe(DmRockCombiner c, bool unbake)
        {
            if (c == null) return;
            c.flatGroundForPreview = true;
            var so = new SerializedObject(c);
            Material blend = c.BlendMaterial;
            if (blend == null && c.MatchTerrain) blend = DmRockBlendMaterials.DefaultTemplate();
            if (blend != null && DmRockBlendMaterials.IsPerRock(blend)) blend = DmRockBlendMaterials.SourceTemplate(blend);
            so.FindProperty("blendMaterial").objectReferenceValue = blend;
            so.FindProperty("m_matchTerrain").boolValue = false;
            if (unbake) so.FindProperty("bake").FindPropertyRelative("baked").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------------------------------------
        // Prefabs

        /// <summary>
        /// Bakes <paramref name="c"/> and writes a prefab at <paramref name="path"/>. The baked mesh is COPIED next to the
        /// prefab (Baked/ meshes are cleaned when no scene uses them), the prefab root sits at the origin and gets its own
        /// placement stamp. The scene rock itself is not changed beyond the bake.
        /// </summary>
        public static GameObject SaveRockPrefab(DmRockCombiner c, string path, out string error)
        {
            error = null;
            if (c == null) { error = "No rock."; return null; }
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) { error = "Bad prefab path."; return null; }
            if (!DmRockBaker.Bake(c, false, out DmRockBaker.Report rep) || !c.IsBaked || c.BakeData.mesh == null)
            {
                error = "Bake failed" + (rep != null && !string.IsNullOrEmpty(rep.note) ? ": " + rep.note : ".");
                return null;
            }
            string folder = path.Substring(0, path.LastIndexOf('/'));
            PcgStudioExtensions.EnsureFolder(folder);
            string bakedPath = AssetDatabase.GetAssetPath(c.BakeData.mesh);
            string meshPath = path.Substring(0, path.Length - ".prefab".Length) + "_Mesh.asset";
            if (AssetDatabase.LoadMainAssetAtPath(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
            if (!AssetDatabase.CopyAsset(bakedPath, meshPath)) { error = "Could not copy the baked mesh to " + meshPath; return null; }
            var map = new Dictionary<Mesh, Mesh>();
            // CopyAsset renames the main object after the new file: map it explicitly, sub-assets (LODs / collider) by name.
            if (AssetDatabase.LoadMainAssetAtPath(bakedPath) is Mesh srcMain && AssetDatabase.LoadMainAssetAtPath(meshPath) is Mesh dstMain)
                map[srcMain] = dstMain;
            var dst = AssetDatabase.LoadAllAssetsAtPath(meshPath).OfType<Mesh>().Where(m => !AssetDatabase.IsMainAsset(m)).ToList();
            foreach (Mesh s in AssetDatabase.LoadAllAssetsAtPath(bakedPath).OfType<Mesh>())
            {
                if (map.ContainsKey(s)) continue;
                Mesh d = dst.FirstOrDefault(x => x.name == s.name);
                if (d != null) map[s] = d;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(c.gameObject, path, out bool ok);
            if (!ok || saved == null) { error = "SaveAsPrefabAsset failed for " + path; return null; }
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                contents.transform.localPosition = Vector3.zero;
                contents.transform.localRotation = Quaternion.identity;
                Mesh Remap(Mesh m) => m != null && map.TryGetValue(m, out Mesh r) ? r : m;
                foreach (MeshFilter mf in contents.GetComponentsInChildren<MeshFilter>(true)) mf.sharedMesh = Remap(mf.sharedMesh);
                foreach (MeshCollider mc in contents.GetComponentsInChildren<MeshCollider>(true)) mc.sharedMesh = Remap(mc.sharedMesh);
                foreach (DmRockCombiner rc in contents.GetComponentsInChildren<DmRockCombiner>(true))
                {
                    var so = new SerializedObject(rc);
                    SerializedProperty b = so.FindProperty("bake");
                    SerializedProperty pm = b.FindPropertyRelative("mesh");
                    pm.objectReferenceValue = Remap(pm.objectReferenceValue as Mesh);
                    SerializedProperty pc = b.FindPropertyRelative("colliderMesh");
                    pc.objectReferenceValue = Remap(pc.objectReferenceValue as Mesh);
                    SerializedProperty lods = b.FindPropertyRelative("lodMeshes");
                    for (int i = 0; i < lods.arraySize; i++)
                    {
                        SerializedProperty el = lods.GetArrayElementAtIndex(i);
                        el.objectReferenceValue = Remap(el.objectReferenceValue as Mesh);
                    }
                    if (rc.transform == contents.transform)
                    {
                        b.FindPropertyRelative("position").vector3Value = Vector3.zero;
                        b.FindPropertyRelative("rotation").quaternionValue = Quaternion.identity;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    rc.NewPlacementStamp();
                }
                saved = PrefabUtility.SaveAsPrefabAsset(contents, path, out ok);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            if (!ok) { error = "Re-saving the prefab failed."; return null; }
            foreach (MeshFilter mf in saved.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh == null || AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(DmRockBaker.BakedRoot + "/", StringComparison.Ordinal))
                {
                    error = $"Prefab mesh on {mf.name} still points at the scene bake ({AssetDatabase.GetAssetPath(mf.sharedMesh)}).";
                    Debug.LogWarning("Rock Studio: " + error, saved);
                    return saved;
                }
            return saved;
        }

        // ------------------------------------------------------------------------------------------------
        // Scene placement

        /// <summary>Instantiates the entry's prefab at <paramref name="point"/> (prefab link kept), new variation, snapped, bake requested.</summary>
        public static GameObject Place(PcgAssetLibrary.Entry e, Vector3 point, bool select)
        {
            if (e == null || e.prefab == null) return null;
            Scene scene = SceneManager.GetActiveScene();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(e.prefab, scene);
            if (go == null) return null;
            PcgSeededPasteWatcher.Suppress(go); // handled here (one reseed, one snap)
            go.transform.position = point;
            Undo.RegisterCreatedObjectUndo(go, "Place From PCG Library");
            foreach (IPcgSeeded s in go.GetComponentsInChildren<IPcgSeeded>(true))
            {
                if (NewVariationOnPlace) PcgSeededPasteWatcher.ReseedAsCopy(s, "Place From PCG Library");
                else { Undo.RecordObject((Component)s, "Place From PCG Library"); s.NewPlacementStamp(); }
            }
            var root = go.GetComponent<IPcgSeeded>();
            PcgSurfaceSnapSettings snap = root != null ? root.SnapSettings : PcgSurfaceSnapSettings.Default;
            PcgSeededPasteWatcher.SnapWithUndo(go.transform, snap, null, "Place From PCG Library");
            foreach (DmRockCombiner rc in go.GetComponentsInChildren<DmRockCombiner>(true))
                DmRockBakeScheduler.Request(rc, 0.3f, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (select) Selection.activeGameObject = go;
            return go;
        }

        public static PcgSurfaceSnapSettings SnapSettingsOf(GameObject prefab)
        {
            var s = prefab != null ? prefab.GetComponent<IPcgSeeded>() : null;
            return s != null && s.SnapSettings != null ? s.SnapSettings : PcgSurfaceSnapSettings.Default;
        }
    }

    /// <summary>Scene View drop target for library tiles (drag from the grid, drop on the ground).</summary>
    [InitializeOnLoad]
    internal static class PcgStudioSceneDrop
    {
        static PcgStudioSceneDrop()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sv)
        {
            Event e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (!(DragAndDrop.GetGenericData(PcgStudioLibraryUtil.DragKey) is string id)) return;
            PcgAssetLibrary lib = PcgStudioLibraryUtil.Load(false);
            PcgAssetLibrary.Entry entry = lib != null ? lib.Find(id) : null;
            if (entry == null || entry.prefab == null) return;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hit = PcgSurfaceSnap.RaycastSurface(ray, PcgStudioLibraryUtil.SnapSettingsOf(entry.prefab), -1, null, out PcgSurfaceSnap.Result r);
            DragAndDrop.visualMode = hit ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (e.type == EventType.DragPerform && hit)
            {
                DragAndDrop.AcceptDrag();
                PcgStudioLibraryUtil.Place(entry, r.point, true);
                DragAndDrop.SetGenericData(PcgStudioLibraryUtil.DragKey, null);
            }
            e.Use();
        }
    }
}
#endif
