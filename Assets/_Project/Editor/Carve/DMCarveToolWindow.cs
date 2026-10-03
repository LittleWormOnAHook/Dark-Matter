using System.Collections.Generic;
using Project.SurfaceCarve;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.SurfaceCarve
{
    /// <summary>
    /// Edit Mode carve brush for rocks. Click = one cut, hold + drag = continuous cut.
    /// No colliders needed: hits are found against the rendered mesh.
    /// </summary>
    public class DMCarveToolWindow : EditorWindow
    {
        public const string MenuPath = "Tools/Dark Matter Genesis/Combat/Surface Carve Tool";
        public const string SaveRoot = "Assets/_Project/Art/Carve";
        private const string Pref = "DMG.SurfaceCarve.";

        private static bool carving;
        private static readonly Dictionary<Mesh, PickMesh> PickCache = new Dictionary<Mesh, PickMesh>();

        private DMCarveStyle style = DMCarveStyle.Fracture;
        private float radius = 0.35f;
        private float depth = 0.45f;
        private float roughness = 1f;
        private bool onlyRocks = true;
        private bool limitToSelection;
        private bool autoSave = true;
        private Vector2 scroll;

        private bool hasHit;
        private Vector3 hitPoint;
        private Vector3 hitNormal;
        private GameObject hitObject;

        private bool stroking;
        private Vector3 lastStamp;
        private int undoGroup;
        private readonly HashSet<DMCarvable> strokeTargets = new HashSet<DMCarvable>();
        private System.Random rng = new System.Random();

        private sealed class PickMesh
        {
            public Vector3[] Vertices;
            public int[] Triangles;
        }

        [MenuItem(MenuPath, false, 40)]
        public static void Open()
        {
            var w = GetWindow<DMCarveToolWindow>();
            w.titleContent = new GUIContent("Surface Carve");
            w.minSize = new Vector2(300f, 360f);
            w.Show();
        }

        public static void ClearPickCache()
        {
            PickCache.Clear();
        }

        private void OnEnable()
        {
            style = (DMCarveStyle)EditorPrefs.GetInt(Pref + "style", 0);
            radius = EditorPrefs.GetFloat(Pref + "radius", 0.35f);
            depth = EditorPrefs.GetFloat(Pref + "depth", 0.45f);
            roughness = EditorPrefs.GetFloat(Pref + "roughness", 1f);
            onlyRocks = EditorPrefs.GetBool(Pref + "onlyRocks", true);
            limitToSelection = EditorPrefs.GetBool(Pref + "limitSel", false);
            autoSave = EditorPrefs.GetBool(Pref + "autoSave", true);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            carving = false;
            SavePrefs();
            SceneView.RepaintAll();
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(Pref + "style", (int)style);
            EditorPrefs.SetFloat(Pref + "radius", radius);
            EditorPrefs.SetFloat(Pref + "depth", depth);
            EditorPrefs.SetFloat(Pref + "roughness", roughness);
            EditorPrefs.SetBool(Pref + "onlyRocks", onlyRocks);
            EditorPrefs.SetBool(Pref + "limitSel", limitToSelection);
            EditorPrefs.SetBool(Pref + "autoSave", autoSave);
        }

        private void OnGUI()
        {
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Surface Carve", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scene view: click = one cut, hold + drag = continuous cut. Shift+Scroll = radius, Ctrl+Scroll = depth, Esc = stop.\n" +
                "Cutters have no collider; deeper = more rock removed. Ammo damage per type lives in Genesis Studio > Combat > Surface Damage.",
                MessageType.None);

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = carving ? new Color(1f, 0.55f, 0.35f) : prev;
            if (GUILayout.Button(carving ? "Carving ON (click to stop)" : "Start Carving", GUILayout.Height(30f)))
            {
                carving = !carving;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = prev;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Preset", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(style == DMCarveStyle.Fracture, "Fracture", EditorStyles.miniButtonLeft) && style != DMCarveStyle.Fracture)
                ApplyPreset(DMCarveStyle.Fracture);
            if (GUILayout.Toggle(style == DMCarveStyle.Erosion, "Erosion", EditorStyles.miniButtonMid) && style != DMCarveStyle.Erosion)
                ApplyPreset(DMCarveStyle.Erosion);
            if (GUILayout.Toggle(style == DMCarveStyle.Blast, "Blast", EditorStyles.miniButtonRight) && style != DMCarveStyle.Blast)
                ApplyPreset(DMCarveStyle.Blast);
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            radius = EditorGUILayout.Slider(new GUIContent("Radius (m)", "Size of the cutter."), radius, 0.02f, 5f);
            depth = EditorGUILayout.Slider(new GUIContent("Depth", "How far the cutter sinks into the surface (x radius). Deeper removes more."), depth, 0.05f, 1f);
            roughness = EditorGUILayout.Slider(new GUIContent("Roughness", "Noise on the cutter shape."), roughness, 0f, 2f);
            onlyRocks = EditorGUILayout.Toggle(new GUIContent("Only Rocks", "Only carve objects with a Carvable Surface or a rock-like name/tag (Surface Damage settings)."), onlyRocks);
            limitToSelection = EditorGUILayout.Toggle(new GUIContent("Limit To Selection", "Only carve selected objects."), limitToSelection);
            autoSave = EditorGUILayout.Toggle(new GUIContent("Auto Save Meshes", "Save carved meshes/materials to " + SaveRoot + " after each stroke."), autoSave);
            if (EditorGUI.EndChangeCheck())
            {
                SavePrefs();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Selection", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Make Selected Carvable"))
                MakeSelectedCarvable();
            if (GUILayout.Button("Reset Selected (restore original mesh)"))
                ResetSelected();
            if (GUILayout.Button("Save Carved Assets (selected)"))
                SaveSelected();
            if (GUILayout.Button("Enable Read/Write on Selected Models"))
                EnableReadWriteOnSelected();

            DrawSelectionStatus();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Open Surface Damage Settings"))
                PingSettings();

            EditorGUILayout.EndScrollView();
        }

        private void ApplyPreset(DMCarveStyle s)
        {
            style = s;
            switch (s)
            {
                case DMCarveStyle.Fracture:
                    depth = 0.45f; roughness = 1f;
                    break;
                case DMCarveStyle.Erosion:
                    depth = 0.3f; roughness = 0.8f;
                    break;
                case DMCarveStyle.Blast:
                    depth = 0.35f; roughness = 1.2f;
                    radius = Mathf.Max(radius, 0.6f);
                    break;
            }

            SavePrefs();
            SceneView.RepaintAll();
        }

        private void DrawSelectionStatus()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return;
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return;

            var lines = new List<string>();
            DMCarvable c = go.GetComponent<DMCarvable>();
            Mesh src = c != null && c.SourceMesh != null ? c.SourceMesh : mf.sharedMesh;
            lines.Add(c != null ? (c.IsCarved ? "Carvable (carved)" : "Carvable") : "Not carvable yet (added on first cut)");
            long idx = 0;
            for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++)
                idx += mf.sharedMesh.GetIndexCount(sm);
            lines.Add("Triangles: " + (idx / 3).ToString("N0"));
            if (!src.isReadable)
                lines.Add("Read/Write OFF: Play Mode / ammo carving needs it.");
            if ((GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.BatchingStatic) != 0)
                lines.Add("Batching Static ON: Play Mode carving is skipped.");
            if (go.GetComponent<LODGroup>() != null || go.GetComponentInParent<LODGroup>() != null)
                lines.Add("LOD Group: only the LOD you carve changes (carve LOD0).");
            EditorGUILayout.HelpBox(string.Join("\n", lines), MessageType.Info);
        }

        // ---------------- Scene view ----------------

        private void OnSceneGUI(SceneView view)
        {
            if (!carving)
                return;

            Event e = Event.current;
            int id = GUIUtility.GetControlID("DMSurfaceCarve".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(id);

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                carving = false;
                EndStroke();
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.ScrollWheel && (e.shift || e.control))
            {
                float delta = Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x) ? e.delta.y : e.delta.x;
                float step = delta > 0f ? -1f : 1f;
                if (e.shift)
                    radius = Mathf.Clamp(radius * (1f + 0.08f * step), 0.02f, 5f);
                else
                    depth = Mathf.Clamp(depth + 0.03f * step, 0.05f, 1f);
                SavePrefs();
                Repaint();
                e.Use();
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            {
                UpdateHit(e.mousePosition);
                view.Repaint();
            }

            if (e.alt)
                return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        BeginStroke();
                        if (hasHit)
                            Stamp();
                        e.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (e.button == 0 && GUIUtility.hotControl == id && stroking)
                    {
                        float spacing = Mathf.Max(0.005f, radius * DMCarveSettings.Active.dragStampSpacing);
                        if (hasHit && (hitPoint - lastStamp).sqrMagnitude >= spacing * spacing)
                            Stamp();
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (e.button == 0 && GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        EndStroke();
                        e.Use();
                    }

                    break;
                case EventType.Repaint:
                    DrawPreview();
                    break;
            }
        }

        private void DrawPreview()
        {
            if (!hasHit)
                return;

            Color c = style == DMCarveStyle.Blast ? new Color(1f, 0.45f, 0.2f)
                : style == DMCarveStyle.Erosion ? new Color(0.45f, 0.8f, 1f)
                : new Color(1f, 0.85f, 0.35f);
            Vector3 n = hitNormal;
            Vector3 center = hitPoint - n * (radius * depth);
            float mouthRadius = radius * Mathf.Sqrt(Mathf.Max(0.05f, 1f - (1f - depth) * (1f - depth)));

            using (new Handles.DrawingScope(c))
            {
                Handles.DrawWireDisc(hitPoint, n, mouthRadius, 2f);
                Handles.DrawWireDisc(hitPoint, n, radius, 1f);
                Handles.DrawDottedLine(hitPoint, center, 3f);
                Handles.DrawWireDisc(center, n, radius * 0.15f, 2f);
            }

            Handles.Label(hitPoint + n * (radius * 0.3f), style + "  r " + radius.ToString("0.00") + "  d " + depth.ToString("0.00"), EditorStyles.whiteMiniLabel);
        }

        private void UpdateHit(Vector2 mousePos)
        {
            hasHit = false;
            hitObject = null;
            GameObject picked = HandleUtility.PickGameObject(mousePos, false);
            if (picked == null || !CanCarve(picked))
                return;

            MeshFilter mf = picked.GetComponent<MeshFilter>();
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (RaycastMesh(mf, ray, out Vector3 p, out Vector3 n))
            {
                hasHit = true;
                hitPoint = p;
                hitNormal = n;
                hitObject = picked;
            }
        }

        private bool CanCarve(GameObject go)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || go.GetComponent<MeshRenderer>() == null)
                return false;
            if (limitToSelection && !Selection.Contains(go))
                return false;
            if (onlyRocks && go.GetComponent<DMCarvable>() == null && !DMCarveSettings.Active.MatchesAutoCarvable(go))
                return false;
            return true;
        }

        private static bool RaycastMesh(MeshFilter mf, Ray worldRay, out Vector3 point, out Vector3 normal)
        {
            point = normal = Vector3.zero;
            Mesh mesh = mf.sharedMesh;
            if (!PickCache.TryGetValue(mesh, out PickMesh pm) || pm == null)
            {
                pm = new PickMesh { Vertices = mesh.vertices, Triangles = mesh.triangles };
                PickCache[mesh] = pm;
            }

            Transform t = mf.transform;
            Vector3 o = t.InverseTransformPoint(worldRay.origin);
            Vector3 d = t.InverseTransformVector(worldRay.direction);

            float best = float.MaxValue;
            int bestTri = -1;
            Vector3[] v = pm.Vertices;
            int[] tri = pm.Triangles;
            for (int i = 0; i + 2 < tri.Length; i += 3)
            {
                Vector3 a = v[tri[i]], b = v[tri[i + 1]], c = v[tri[i + 2]];
                Vector3 e1 = b - a, e2 = c - a;
                Vector3 pv = Vector3.Cross(d, e2);
                float det = Vector3.Dot(e1, pv);
                if (Mathf.Abs(det) < 1e-12f)
                    continue;
                float inv = 1f / det;
                Vector3 tv = o - a;
                float u = Vector3.Dot(tv, pv) * inv;
                if (u < 0f || u > 1f)
                    continue;
                Vector3 qv = Vector3.Cross(tv, e1);
                float w = Vector3.Dot(d, qv) * inv;
                if (w < 0f || u + w > 1f)
                    continue;
                float dist = Vector3.Dot(e2, qv) * inv;
                if (dist > 0f && dist < best)
                {
                    best = dist;
                    bestTri = i;
                }
            }

            if (bestTri < 0)
                return false;

            Vector3 la = v[tri[bestTri]], lb = v[tri[bestTri + 1]], lc = v[tri[bestTri + 2]];
            point = t.TransformPoint(o + d * best);
            Vector3 wn = Vector3.Cross(t.TransformPoint(lb) - t.TransformPoint(la), t.TransformPoint(lc) - t.TransformPoint(la));
            normal = wn.sqrMagnitude > 1e-12f ? wn.normalized : -worldRay.direction;
            if (Vector3.Dot(normal, worldRay.direction) > 0f)
                normal = -normal;
            return true;
        }

        private void BeginStroke()
        {
            stroking = true;
            strokeTargets.Clear();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Surface Carve");
            undoGroup = Undo.GetCurrentGroup();
            lastStamp = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }

        private void Stamp()
        {
            if (hitObject == null)
                return;

            DMCarvable carvable = hitObject.GetComponent<DMCarvable>();
            if (carvable == null)
                carvable = Undo.AddComponent<DMCarvable>(hitObject);
            if (carvable == null)
                return;

            if (strokeTargets.Add(carvable))
                RecordUndo(carvable);

            var damage = DMSurfaceDamageSettings.Make(style, radius, depth, roughness, 0, 0.35f);
            int seed = rng.Next(1, int.MaxValue);
            Mesh before = carvable.GetComponent<MeshFilter>().sharedMesh;
            if (carvable.CarveAtSurface(hitPoint, hitNormal, damage, seed, false))
            {
                PickCache.Remove(before);
                Mesh after = carvable.GetComponent<MeshFilter>().sharedMesh;
                if (after != null)
                    PickCache.Remove(after);
                EditorUtility.SetDirty(carvable);
                if (carvable.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(carvable.gameObject.scene);
            }

            lastStamp = hitPoint;
        }

        private static void RecordUndo(DMCarvable c)
        {
            var objs = new List<Object> { c, c.GetComponent<MeshFilter>() };
            MeshRenderer mr = c.GetComponent<MeshRenderer>();
            if (mr != null) objs.Add(mr);
            MeshCollider mc = c.GetComponent<MeshCollider>();
            if (mc != null) objs.Add(mc);
            if (c.CarvedMesh != null) objs.Add(c.CarvedMesh);
            Undo.RegisterCompleteObjectUndo(objs.ToArray(), "Surface Carve");
        }

        private void EndStroke()
        {
            if (!stroking)
                return;
            stroking = false;
            Undo.CollapseUndoOperations(undoGroup);
            foreach (DMCarvable c in strokeTargets)
            {
                if (c == null)
                    continue;
                c.RefreshCollider();
                if (autoSave)
                    SaveCarvedAssets(c);
            }

            strokeTargets.Clear();
        }

        // ---------------- Buttons ----------------

        private static void MakeSelectedCarvable()
        {
            int n = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<MeshFilter>() == null || go.GetComponent<DMCarvable>() != null)
                    continue;
                Undo.AddComponent<DMCarvable>(go);
                n++;
            }

            Debug.Log("[Surface Carve] Made " + n + " object(s) carvable.");
        }

        private static void ResetSelected()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                DMCarvable c = go.GetComponent<DMCarvable>();
                if (c == null)
                    continue;
                RecordUndo(c);
                c.ResetCarve();
                c.RefreshCollider();
                EditorUtility.SetDirty(c);
                if (go.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(go.scene);
            }

            ClearPickCache();
        }

        private static void SaveSelected()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                DMCarvable c = go.GetComponent<DMCarvable>();
                if (c != null)
                    SaveCarvedAssets(c);
            }
        }

        public static void SaveCarvedAssets(DMCarvable c)
        {
            if (c == null || c.CarvedMesh == null)
                return;

            string meshFolder = EnsureFolder(SaveRoot + "/Meshes");
            string matFolder = EnsureFolder(SaveRoot + "/Materials");
            Mesh mesh = c.CarvedMesh;
            if (!AssetDatabase.Contains(mesh))
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(meshFolder + "/" + Safe(c.gameObject.name) + "_carved.asset");
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }

            MeshRenderer mr = c.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material m in mr.sharedMaterials)
                {
                    if (m == null || AssetDatabase.Contains(m))
                        continue;
                    string path = AssetDatabase.GenerateUniqueAssetPath(matFolder + "/" + Safe(m.name) + ".mat");
                    AssetDatabase.CreateAsset(m, path);
                }
            }

            EditorUtility.SetDirty(c);
        }

        private static void EnableReadWriteOnSelected()
        {
            var done = new HashSet<string>();
            foreach (GameObject go in Selection.gameObjects)
            {
                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    DMCarvable c = mf.GetComponent<DMCarvable>();
                    Mesh mesh = c != null && c.SourceMesh != null ? c.SourceMesh : mf.sharedMesh;
                    if (mesh == null)
                        continue;
                    string path = AssetDatabase.GetAssetPath(mesh);
                    if (string.IsNullOrEmpty(path) || !done.Add(path))
                        continue;
                    if (AssetImporter.GetAtPath(path) is ModelImporter mi && !mi.isReadable)
                    {
                        mi.isReadable = true;
                        mi.SaveAndReimport();
                        Debug.Log("[Surface Carve] Read/Write enabled: " + path);
                    }
                }
            }
        }

        private static void PingSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<DMCarveSettings>(DMCarveSettings.AssetPath);
            if (s == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(DMCarveSettings.AssetPath).Replace('\\', '/'));
                s = CreateInstance<DMCarveSettings>();
                AssetDatabase.CreateAsset(s, DMCarveSettings.AssetPath);
            }

            Selection.activeObject = s;
            EditorGUIUtility.PingObject(s);
        }

        private static string EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return folder;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
            return folder;
        }

        private static string Safe(string s)
        {
            foreach (char ch in System.IO.Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s.Replace(' ', '_');
        }
    }
}
