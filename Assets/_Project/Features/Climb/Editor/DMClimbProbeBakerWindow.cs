using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Project.Features.Climb.Editor
{
    /// <summary>
    /// Editor tool to bake AAA-style grab probes onto climbable rocks/cliffs.
    /// Menu: Dark Matter Genesis / Climb / Probe Baker
    /// </summary>
    public sealed class DMClimbProbeBakerWindow : EditorWindow
    {
        private const string PrefRadius = "DMClimbProbeBaker.radius";
        private const string PrefGizmoScale = "DMClimbProbeBaker.gizmoScale";
        private const string PrefGizmoR = "DMClimbProbeBaker.gizmoR";
        private const string PrefGizmoG = "DMClimbProbeBaker.gizmoG";
        private const string PrefGizmoB = "DMClimbProbeBaker.gizmoB";
        private const string PrefGizmoA = "DMClimbProbeBaker.gizmoA";
        private const string PrefSelR = "DMClimbProbeBaker.selR";
        private const string PrefSelG = "DMClimbProbeBaker.selG";
        private const string PrefSelB = "DMClimbProbeBaker.selB";
        private const string PrefSelA = "DMClimbProbeBaker.selA";
        private const string PrefMinDist = "DMClimbProbeBaker.minDist";
        private const string PrefGrid = "DMClimbProbeBaker.grid";
        private const string PrefManual = "DMClimbProbeBaker.manual";
        private const string PrefHandSpan = "DMClimbProbeBaker.handSpan";

        private const string ClimbableTag = "Climbable";
        private const string ClimbableLayerName = "Climbable";
        private const int ClimbableLayerFallback = 23;

        private GameObject _target;
        private DMClimbProbeSet _probeSet;
        private float _defaultRadius = 0.12f;
        private float _gizmoScale = 1f;
        private Color _gizmoColor = new Color(0.2f, 0.85f, 1f, 0.85f);
        private Color _selectedColor = new Color(1f, 0.15f, 0.12f, 0.95f);
        private float _minDistance = 0.5f;
        private int _gridResolution = 8;
        private bool _manualPlace;
        private float _handSpan = 0.5f;
        /// <summary>Surfaces at or under this slope from up are walkable — no climb probes (matches profile walkMax ~75).</summary>
        private float _walkableMaxSlopeDeg = 75f;
        private bool _staggerRows = true;
        private int _selectedIndex = -1;
        private int _nextPairId;
        private Vector2 _scroll;
        private bool _draggingHandle;

        [MenuItem("Dark Matter Genesis/Climb/Probe Baker")]
        public static void Open()
        {
            var win = GetWindow<DMClimbProbeBakerWindow>("Climb Probe Baker");
            win.minSize = new Vector2(340f, 460f);
            win.Show();
        }

        private void OnEnable()
        {
            _defaultRadius = EditorPrefs.GetFloat(PrefRadius, 0.12f);
            _gizmoScale = EditorPrefs.GetFloat(PrefGizmoScale, 1f);
            _gizmoColor = new Color(
                EditorPrefs.GetFloat(PrefGizmoR, 0.2f),
                EditorPrefs.GetFloat(PrefGizmoG, 0.85f),
                EditorPrefs.GetFloat(PrefGizmoB, 1f),
                EditorPrefs.GetFloat(PrefGizmoA, 0.85f));
            _selectedColor = new Color(
                EditorPrefs.GetFloat(PrefSelR, 1f),
                EditorPrefs.GetFloat(PrefSelG, 0.15f),
                EditorPrefs.GetFloat(PrefSelB, 0.12f),
                EditorPrefs.GetFloat(PrefSelA, 0.95f));
            _minDistance = EditorPrefs.GetFloat(PrefMinDist, 0.5f);
            _gridResolution = EditorPrefs.GetInt(PrefGrid, 8);
            _manualPlace = EditorPrefs.GetBool(PrefManual, false);
            _handSpan = EditorPrefs.GetFloat(PrefHandSpan, 0.5f);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SavePrefs();
            SyncEditorSelection(-1);
        }

        private void SavePrefs()
        {
            EditorPrefs.SetFloat(PrefRadius, _defaultRadius);
            EditorPrefs.SetFloat(PrefGizmoScale, _gizmoScale);
            EditorPrefs.SetFloat(PrefGizmoR, _gizmoColor.r);
            EditorPrefs.SetFloat(PrefGizmoG, _gizmoColor.g);
            EditorPrefs.SetFloat(PrefGizmoB, _gizmoColor.b);
            EditorPrefs.SetFloat(PrefGizmoA, _gizmoColor.a);
            EditorPrefs.SetFloat(PrefSelR, _selectedColor.r);
            EditorPrefs.SetFloat(PrefSelG, _selectedColor.g);
            EditorPrefs.SetFloat(PrefSelB, _selectedColor.b);
            EditorPrefs.SetFloat(PrefSelA, _selectedColor.a);
            EditorPrefs.SetFloat(PrefMinDist, _minDistance);
            EditorPrefs.SetInt(PrefGrid, _gridResolution);
            EditorPrefs.SetBool(PrefManual, _manualPlace);
            EditorPrefs.SetFloat(PrefHandSpan, _handSpan);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Climb Probe Baker", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bake / place surface-snapped grab probes. Manual Place: click mesh to add. Click a probe sphere to select; drag the handle to move (stays on surface). Apply Climbable sets tag+layer 23.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _target = (GameObject)EditorGUILayout.ObjectField("Target", _target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                ResolveProbeSet(addIfMissing: false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selection"))
                {
                    if (Selection.activeGameObject != null)
                    {
                        _target = Selection.activeGameObject;
                        ResolveProbeSet(addIfMissing: false);
                    }
                }
                if (GUILayout.Button("Ensure ProbeSet"))
                    ResolveProbeSet(addIfMissing: true);
            }

            EditorGUILayout.Space(6);
            _defaultRadius = EditorGUILayout.Slider("Probe Radius", _defaultRadius, 0.04f, 0.4f);
            _gridResolution = EditorGUILayout.IntSlider("Bake Grid", _gridResolution, 4, 24);
            _handSpan = EditorGUILayout.Slider("L/R Pair Distance (m)", _handSpan, 0.25f, 1.2f);
            _minDistance = EditorGUILayout.Slider("Distance Between Pairs (m)", _minDistance, 0.2f, 2.5f);
        _walkableMaxSlopeDeg = EditorGUILayout.Slider("Walkable Max Slope (deg)", _walkableMaxSlopeDeg, 45f, 85f);
        _staggerRows = EditorGUILayout.ToggleLeft("Stagger rows (brick / hex style — better diagonals & strafe)", _staggerRows);
        EditorGUILayout.HelpBox("No probes on walkable tops / flats (angle from up <= Walkable Max Slope). Climb faces only. Manual place still allowed.", MessageType.Info);
            EditorGUILayout.HelpBox($"Bake places TWO probes per sample, { _handSpan:F2}m apart (L+R). \"Distance Between Pairs\" spaces those stance pairs apart on the mesh. Manual place = singles.", MessageType.Info);
            _gizmoScale = EditorGUILayout.Slider("Gizmo Size", _gizmoScale, 0.25f, 3f);
            _gizmoColor = EditorGUILayout.ColorField("Gizmo Color", _gizmoColor);
            _selectedColor = EditorGUILayout.ColorField("Selected Probe Color", _selectedColor);

            if (_probeSet != null && GUI.changed)
                ApplyGizmoStyle();

            EditorGUILayout.Space(6);
            _manualPlace = EditorGUILayout.ToggleLeft("Manual Place (Scene View click on mesh)", _manualPlace);
            if (_manualPlace)
                EditorGUILayout.HelpBox("Click the asset mesh to add a surface-snapped probe. Click an existing probe sphere to select it instead.", MessageType.None);

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_target == null))
            {
                if (GUILayout.Button("Bake Probes", GUILayout.Height(28)))
                {
                    ResolveProbeSet(addIfMissing: true);
                    StripLightProbeGroupsUnderTarget();
                    BakeProbes();
                    ApplyClimbableMarking();
                }
                if (GUILayout.Button("Apply Climbable (tag + layer 23)", GUILayout.Height(24)))
                    ApplyClimbableMarking();
                if (GUILayout.Button("Strip Light Probe Groups under target", GUILayout.Height(22)))
                    StripLightProbeGroupsUnderTarget();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Clear"))
                    {
                        ResolveProbeSet(addIfMissing: false);
                        ClearProbes();
                    }
                    if (GUILayout.Button("Add Manual at Hit"))
                    {
                        ResolveProbeSet(addIfMissing: true);
                        AddManualAtSceneViewCenter();
                    }
                    if (GUILayout.Button("Delete Selected"))
                        DeleteSelected();
                }
            }

            EditorGUILayout.Space(8);
            if (_probeSet != null)
            {
                EditorGUILayout.LabelField($"Probes: {_probeSet.Count}", EditorStyles.boldLabel);
                for (int i = 0; i < _probeSet.Count; i++)
                {
                    var p = _probeSet.Probes[i];
                    bool sel = i == _selectedIndex;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string handTag = p.hand == DMClimbProbeSet.HandSide.Left ? "L" : (p.hand == DMClimbProbeSet.HandSide.Right ? "R" : "-");
                    string pairTag = p.pairId >= 0 ? $"p{p.pairId}" : "";
                    string label = p.isManual
                        ? $"[{i}] {p.type}* {handTag}"
                        : $"[{i}] {p.type} {handTag}{pairTag}";
                        if (GUILayout.Toggle(sel, label, "Button", GUILayout.Width(120)))
                        {
                            _selectedIndex = i;
                            SyncEditorSelection(i);
                            SceneView.RepaintAll();
                        }
                        EditorGUILayout.LabelField($"r={p.radius:F2}  nY={p.localNormal.y:F2}");
                    }
                }
                EditorGUILayout.LabelField("* = manual - move handle only while selected", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("No DMClimbProbeSet on target yet. Bake or Ensure ProbeSet.", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
            if (GUI.changed)
                SavePrefs();
        }

        private void SyncEditorSelection(int index)
        {
            if (_probeSet != null)
                _probeSet.EditorSelectedIndex = index;
        }

        private void ResolveProbeSet(bool addIfMissing)
        {
            _probeSet = null;
            if (_target == null)
                return;

            _probeSet = _target.GetComponent<DMClimbProbeSet>();
            if (_probeSet == null && addIfMissing)
            {
                Undo.AddComponent<DMClimbProbeSet>(_target);
                _probeSet = _target.GetComponent<DMClimbProbeSet>();
                EditorUtility.SetDirty(_target);
            }
            if (_probeSet != null)
            {
                ApplyGizmoStyle();
                SyncEditorSelection(_selectedIndex);
            }
        }

        private void ApplyGizmoStyle()
        {
            if (_probeSet == null)
                return;
            Undo.RecordObject(_probeSet, "Climb Probe Gizmo Style");
            _probeSet.GizmoColor = _gizmoColor;
            _probeSet.SelectedGizmoColor = _selectedColor;
            _probeSet.GizmoScale = _gizmoScale;
            EditorUtility.SetDirty(_probeSet);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Sets tag "Climbable" and layer Climbable (23) on the target root and MeshColliders
        /// used for climbing under it. Does not rewrite unrelated child layers/tags.
        /// </summary>
        private void ApplyClimbableMarking()
        {
            if (_target == null)
                return;

            EnsureClimbableTagExists();
            int layer = ResolveClimbableLayer();
            if (layer < 0)
            {
                Debug.LogWarning("[Climb Probe Baker] Climbable layer not found; expected name 'Climbable' (canon index 23).");
                return;
            }

            int tagged = 0;
            Undo.RecordObject(_target, "Apply Climbable Tag/Layer");
            if (!string.IsNullOrEmpty(ClimbableTag))
            {
                _target.tag = ClimbableTag;
                tagged++;
            }
            _target.layer = layer;
            EditorUtility.SetDirty(_target);

            // Prefer MeshColliders already collected for bake / Climbable layer children.
            CollectBakeTargets(out List<Collider> colliders, out _);
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;
                GameObject go = col.gameObject;
                if (!seen.Add(go))
                    continue;
                Undo.RecordObject(go, "Apply Climbable Tag/Layer");
                if (!string.IsNullOrEmpty(ClimbableTag))
                    go.tag = ClimbableTag;
                go.layer = layer;
                EditorUtility.SetDirty(go);
                tagged++;
            }

            // If no colliders yet, still mark MeshFilters that look like climb mesh (same root / direct renderers).
            if (colliders.Count == 0)
            {
                var mfs = _target.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < mfs.Length; i++)
                {
                    MeshFilter mf = mfs[i];
                    if (mf == null || mf.sharedMesh == null)
                        continue;
                    // Skip tiny detail / particle-ish meshes by vertex count heuristic.
                    if (mf.sharedMesh.vertexCount < 8)
                        continue;
                    GameObject go = mf.gameObject;
                    if (!seen.Add(go))
                        continue;
                    Undo.RecordObject(go, "Apply Climbable Tag/Layer");
                    if (!string.IsNullOrEmpty(ClimbableTag))
                        go.tag = ClimbableTag;
                    go.layer = layer;
                    EditorUtility.SetDirty(go);
                    tagged++;
                }
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(_target);
            Debug.Log($"[Climb Probe Baker] Apply Climbable: tag='{ClimbableTag}', layer={layer} ({ClimbableLayerName}) on root + {tagged - 1} mesh/collider object(s) under '{_target.name}'.");
            SceneView.RepaintAll();
        }

                private void StripLightProbeGroupsUnderTarget()
        {
            if (_target == null)
            {
                EditorUtility.DisplayDialog("Climb Probe Baker", "Assign a Target root first.", "OK");
                return;
            }
            var groups = _target.GetComponentsInChildren<LightProbeGroup>(true);
            int removed = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null)
                    continue;
                Undo.DestroyObjectImmediate(groups[i]);
                removed++;
            }
            // Also clear Light Probe Proxy Volumes if any.
            var lppvs = _target.GetComponentsInChildren<LightProbeProxyVolume>(true);
            for (int i = 0; i < lppvs.Length; i++)
            {
                if (lppvs[i] == null)
                    continue;
                Undo.DestroyObjectImmediate(lppvs[i]);
                removed++;
            }
            EditorUtility.SetDirty(_target);
            Debug.Log($"[Climb Probe Baker] Stripped {removed} LightProbeGroup/LPPV component(s) under '{_target.name}'.");
            SceneView.RepaintAll();
        }

        private static void EnsureClimbableTagExists()
        {
            // Project already defines Climbable in TagManager; InternalEditorUtility can add if missing.
            string[] tags = InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == ClimbableTag)
                    return;
            }
            try
            {
                InternalEditorUtility.AddTag(ClimbableTag);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Climb Probe Baker] Could not add tag '{ClimbableTag}': {ex.Message}");
            }
        }

        private static int ResolveClimbableLayer()
        {
            int byName = LayerMask.NameToLayer(ClimbableLayerName);
            if (byName >= 0)
                return byName;
            // Canon fallback index 23 if named lookup fails but slot matches.
            string nameAt23 = LayerMask.LayerToName(ClimbableLayerFallback);
            if (nameAt23 == ClimbableLayerName || string.IsNullOrEmpty(nameAt23) == false && nameAt23.IndexOf("Climb", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return ClimbableLayerFallback;
            if (string.IsNullOrEmpty(LayerMask.LayerToName(ClimbableLayerFallback)))
                return -1;
            return byName;
        }

        private void ClearProbes()
        {
            if (_probeSet == null)
                return;
            Undo.RecordObject(_probeSet, "Clear Climb Probes");
            _probeSet.ClearProbes();
            _selectedIndex = -1;
            SyncEditorSelection(-1);
            EditorUtility.SetDirty(_probeSet);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_probeSet);
            SceneView.RepaintAll();
        }

        private void DeleteSelected()
        {
            if (_probeSet == null || _selectedIndex < 0)
                return;
            Undo.RecordObject(_probeSet, "Delete Climb Probe");
            _probeSet.RemoveProbe(_selectedIndex);
            _selectedIndex = -1;
            SyncEditorSelection(-1);
            EditorUtility.SetDirty(_probeSet);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_probeSet);
            SceneView.RepaintAll();
        }

        private void BakeProbes()
        {
            if (_probeSet == null || _target == null)
                return;

            Undo.RecordObject(_probeSet, "Bake Climb Probes");
            _probeSet.ClearProbes();
            _selectedIndex = -1;
            SyncEditorSelection(-1);

            Bounds worldBounds = CollectBakeTargets(out List<Collider> colliders, out List<MeshFilter> meshes);
            if (colliders.Count == 0 && meshes.Count == 0)
            {
                Debug.LogWarning("[Climb Probe Baker] No MeshCollider/MeshFilter under target.");
                return;
            }

            // Expand slightly so rays start outside.
            worldBounds.Expand(0.35f);
            int grid = Mathf.Clamp(_gridResolution, 4, 24);
            float span = Mathf.Clamp(_handSpan, 0.2f, 1.2f);
            // Min spacing between stance centers (~span) so rows don't smear L/R pairs together.
            float minDist = Mathf.Max(0.05f, _minDistance); // independent of L/R pair distance
            float minDistSqr = minDist * minDist;

            var kept = new List<(Vector3 worldPos, Vector3 worldN, DMClimbProbeSet.ProbeType type)>();
            float topY = worldBounds.max.y;
            float lipBand = Mathf.Max(0.25f, worldBounds.size.y * 0.12f);

            // Side faces only — walkable tops do not get probes (perf + no top-grid clutter).
            TrySampleFace(worldBounds, Vector3.right, grid, colliders, meshes, kept, minDistSqr, topY, lipBand);
            TrySampleFace(worldBounds, Vector3.left, grid, colliders, meshes, kept, minDistSqr, topY, lipBand);
            TrySampleFace(worldBounds, Vector3.forward, grid, colliders, meshes, kept, minDistSqr, topY, lipBand);
            TrySampleFace(worldBounds, Vector3.back, grid, colliders, meshes, kept, minDistSqr, topY, lipBand);

            Transform t = _probeSet.transform;
            List<Collider> snapCols = colliders;
            _nextPairId = 0;
            int pairsAdded = 0;
            int singlesFallback = 0;
            for (int i = 0; i < kept.Count; i++)
            {
                Vector3 center = kept[i].worldPos;
                Vector3 n = kept[i].worldN.normalized;
                if (!TryPlaceHandPair(center, n, span, snapCols, kept[i].type, t))
                {
                    // Rare: wall-right degenerate or both ±normal rays miss.
                    Vector3 localPos = t.InverseTransformPoint(center);
                    Vector3 localN = t.InverseTransformDirection(n).normalized;
                    _probeSet.AddProbe(localPos, localN, _defaultRadius, kept[i].type, isManual: false, pairId: -1, hand: DMClimbProbeSet.HandSide.None);
                    singlesFallback++;
                }
                else
                    pairsAdded++;
            }
            Debug.Log($"[Climb Probe Baker] Baked {pairsAdded} L/R pairs ({pairsAdded * 2} probes) + {singlesFallback} single fallbacks on '{_target.name}' (span={span:F2}m, stanceMin={minDist:F2}m, walkableSkip<={_walkableMaxSlopeDeg:F0}deg, stagger={_staggerRows}, no top face).");

            ApplyGizmoStyle();
            EditorUtility.SetDirty(_probeSet);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_probeSet);
            SceneView.RepaintAll();
        }

        private Bounds CollectBakeTargets(out List<Collider> colliders, out List<MeshFilter> meshes)
        {
            colliders = new List<Collider>();
            meshes = new List<MeshFilter>();
            Bounds b = new Bounds(_target.transform.position, Vector3.one * 0.1f);
            bool has = false;

            int climbableLayer = LayerMask.NameToLayer(ClimbableLayerName);
            if (climbableLayer < 0)
                climbableLayer = ClimbableLayerFallback;

            var allCols = _target.GetComponentsInChildren<MeshCollider>(true);
            var preferred = new List<MeshCollider>();
            var fallback = new List<MeshCollider>();
            for (int i = 0; i < allCols.Length; i++)
            {
                MeshCollider mc = allCols[i];
                if (mc == null || !mc.enabled)
                    continue;
                if (climbableLayer >= 0 && mc.gameObject.layer == climbableLayer)
                    preferred.Add(mc);
                else
                    fallback.Add(mc);
            }
            var useCols = preferred.Count > 0 ? preferred : fallback;
            for (int i = 0; i < useCols.Count; i++)
            {
                MeshCollider mc = useCols[i];
                colliders.Add(mc);
                if (!has)
                {
                    b = mc.bounds;
                    has = true;
                }
                else
                    b.Encapsulate(mc.bounds);
            }

            var mfs = _target.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < mfs.Length; i++)
            {
                MeshFilter mf = mfs[i];
                if (mf == null || mf.sharedMesh == null)
                    continue;
                if (climbableLayer >= 0 && preferred.Count > 0 && mf.gameObject.layer != climbableLayer)
                    continue;
                meshes.Add(mf);
                Bounds mb = mf.sharedMesh.bounds;
                Vector3 worldCenter = mf.transform.TransformPoint(mb.center);
                Vector3 worldSize = Vector3.Scale(mb.size, mf.transform.lossyScale);
                Bounds wb = new Bounds(worldCenter, worldSize);
                if (!has)
                {
                    b = wb;
                    has = true;
                }
                else
                    b.Encapsulate(wb);
            }

            return b;
        }

        private void TrySampleFace(
            Bounds worldBounds,
            Vector3 faceOut,
            int grid,
            List<Collider> colliders,
            List<MeshFilter> meshes,
            List<(Vector3 worldPos, Vector3 worldN, DMClimbProbeSet.ProbeType type)> kept,
            float minDistSqr,
            float topY,
            float lipBand)
        {
            Vector3 u = Vector3.Cross(faceOut, Mathf.Abs(faceOut.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(faceOut, u).normalized;

            Vector3 faceCenter = worldBounds.center + Vector3.Scale(faceOut, worldBounds.extents);
            faceCenter += faceOut * 0.2f;

            float uExtent = Vector3.Dot(worldBounds.extents, new Vector3(Mathf.Abs(u.x), Mathf.Abs(u.y), Mathf.Abs(u.z))) + 0.05f;
            float vExtent = Vector3.Dot(worldBounds.extents, new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z))) + 0.05f;
            float rayLen = worldBounds.size.magnitude + 1.5f;

            for (int iu = 0; iu < grid; iu++)
            {
                for (int iv = 0; iv < grid; iv++)
                {
                    float tu = grid == 1 ? 0.5f : iu / (float)(grid - 1);
                    float tv = grid == 1 ? 0.5f : iv / (float)(grid - 1);
                    float cellU = grid > 1 ? (uExtent * 2f) / (grid - 1) : 0f;
                    Vector3 origin = faceCenter
                        + u * Mathf.Lerp(-uExtent, uExtent, tu)
                        + v * Mathf.Lerp(-vExtent, vExtent, tv);
                    // Brick/hex: odd V-rows shift by half a cell along U (world), so stagger is obvious in Scene.
                    if (_staggerRows && grid > 1 && (iv & 1) == 1)
                        origin += u * (cellU * 0.5f);
                    Vector3 dir = -faceOut;

                    if (!RaycastBake(origin, dir, rayLen, colliders, meshes, out RaycastHit hit))
                        continue;

                    Vector3 n = hit.normal.normalized;
                    if (n.y < -0.35f)
                        continue;
                    // Walkable tops / flats: no climb probes (player walks these).
                    float slopeFromUp = Vector3.Angle(Vector3.up, n);
                    if (slopeFromUp <= _walkableMaxSlopeDeg)
                        continue;
                    Vector3 fromCenter = (hit.point - worldBounds.center).normalized;
                    float outward = Vector3.Dot(n, fromCenter);
                    bool upwardIsh = n.y > 0.15f;
                    bool outwardIsh = outward > 0.15f;
                    if (!upwardIsh && !outwardIsh && n.y < 0.05f)
                        continue;

                    bool tooClose = false;
                    for (int k = 0; k < kept.Count; k++)
                    {
                        if ((kept[k].worldPos - hit.point).sqrMagnitude < minDistSqr)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose)
                        continue;

                    var type = DMClimbProbeSet.ProbeType.Face;
                    if (hit.point.y >= topY - lipBand && n.y > 0.2f)
                        type = DMClimbProbeSet.ProbeType.Lip;

                    kept.Add((hit.point, n, type));
                }
            }
        }

        private static bool RaycastBake(
            Vector3 origin,
            Vector3 dir,
            float rayLen,
            List<Collider> colliders,
            List<MeshFilter> meshes,
            out RaycastHit hit)
        {
            hit = default;
            bool any = false;
            float best = float.MaxValue;

            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;
                if (!col.Raycast(new Ray(origin, dir), out RaycastHit cand, rayLen))
                    continue;
                if (cand.distance >= best)
                    continue;
                best = cand.distance;
                hit = cand;
                any = true;
            }

            if (any)
                return true;

            if (Physics.Raycast(origin, dir, out RaycastHit phys, rayLen, ~0, QueryTriggerInteraction.Ignore))
            {
                for (int i = 0; i < meshes.Count; i++)
                {
                    if (meshes[i] == null)
                        continue;
                    if (phys.collider != null &&
                        (phys.collider.transform == meshes[i].transform ||
                         phys.collider.transform.IsChildOf(meshes[i].transform) ||
                         meshes[i].transform.IsChildOf(phys.collider.transform)))
                    {
                        hit = phys;
                        return true;
                    }
                }
            }

            return any;
        }


        /// <summary>
        /// From a center surface hit, place Left/Right holds spaced by stance span along wall-right.
        /// Lateral separation is force-locked; mesh stick uses ±normal rays only (no ClosestPoint collapse).
        /// Returns false only if wall-right is degenerate or both rays miss entirely.
        /// </summary>
        private bool TryPlaceHandPair(
            Vector3 centerHit,
            Vector3 centerNormal,
            float span,
            List<Collider> colliders,
            DMClimbProbeSet.ProbeType type,
            Transform t)
        {
            Vector3 n = centerNormal.sqrMagnitude > 0.0001f ? centerNormal.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, n);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.Cross(Vector3.forward, n);
            if (right.sqrMagnitude < 0.0001f)
                right = t != null ? t.right : Vector3.right;
            right.Normalize();

            Vector3 up = Vector3.Cross(n, right);
            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.up;
            else
                up.Normalize();

            span = Mathf.Clamp(span, 0.2f, 1.5f);
            float half = span * 0.5f;

            // Exact L/R targets — never collapse. Rays only slide along normal onto the mesh.
            Vector3 leftPos = centerHit - right * half;
            Vector3 rightPos = centerHit + right * half;
            Vector3 leftN = n;
            Vector3 rightN = n;

            if (RaycastAlongNormal(leftPos, n, colliders, out Vector3 leftSnap, out Vector3 leftSnapN))
            {
                float depth = Vector3.Dot(leftSnap - (centerHit - right * half), n);
                float upOff = Vector3.Dot(leftSnap - (centerHit - right * half), up);
                leftPos = centerHit - right * half + n * depth + up * upOff;
                if (leftSnapN.sqrMagnitude > 0.0001f)
                    leftN = leftSnapN.normalized;
            }
            if (RaycastAlongNormal(rightPos, n, colliders, out Vector3 rightSnap, out Vector3 rightSnapN))
            {
                float depth = Vector3.Dot(rightSnap - (centerHit + right * half), n);
                float upOff = Vector3.Dot(rightSnap - (centerHit + right * half), up);
                rightPos = centerHit + right * half + n * depth + up * upOff;
                if (rightSnapN.sqrMagnitude > 0.0001f)
                    rightN = rightSnapN.normalized;
            }

            // Final lateral lock (distance == span along wall-right).
            Vector3 mid = (leftPos + rightPos) * 0.5f;
            float midDepth = Vector3.Dot(mid - centerHit, n);
            float midUp = Vector3.Dot(mid - centerHit, up);
            mid = centerHit + n * midDepth + up * midUp;
            leftPos = mid - right * half;
            rightPos = mid + right * half;

            int pairId = _nextPairId++;
            Vector3 leftLocal = t.InverseTransformPoint(leftPos);
            Vector3 leftLocalN = t.InverseTransformDirection(leftN).normalized;
            Vector3 rightLocal = t.InverseTransformPoint(rightPos);
            Vector3 rightLocalN = t.InverseTransformDirection(rightN).normalized;

            _probeSet.AddProbe(leftLocal, leftLocalN, _defaultRadius, type, isManual: false, pairId: pairId, hand: DMClimbProbeSet.HandSide.Left);
            _probeSet.AddProbe(rightLocal, rightLocalN, _defaultRadius, type, isManual: false, pairId: pairId, hand: DMClimbProbeSet.HandSide.Right);
            return true;
        }

        private static void RelockStancePair(
            Vector3 centerHit,
            Vector3 n,
            Vector3 right,
            Vector3 up,
            float half,
            bool leftHit,
            Vector3 leftSnap,
            Vector3 leftSnapN,
            bool rightHit,
            Vector3 rightSnap,
            Vector3 rightSnapN,
            out Vector3 leftPos,
            out Vector3 leftN,
            out Vector3 rightPos,
            out Vector3 rightN)
        {
            Vector3 mid = centerHit;
            if (leftHit && rightHit)
                mid = (leftSnap + rightSnap) * 0.5f;
            else if (leftHit)
                mid = leftSnap + right * half;
            else if (rightHit)
                mid = rightSnap - right * half;

            float depthL = 0f, upL = 0f, depthR = 0f, upR = 0f;
            if (leftHit)
            {
                Vector3 baseL = mid - right * half;
                Vector3 off = leftSnap - baseL;
                depthL = Vector3.Dot(off, n);
                upL = Vector3.Dot(off, up);
            }
            if (rightHit)
            {
                Vector3 baseR = mid + right * half;
                Vector3 off = rightSnap - baseR;
                depthR = Vector3.Dot(off, n);
                upR = Vector3.Dot(off, up);
            }

            leftPos = mid - right * half + n * depthL + up * upL;
            rightPos = mid + right * half + n * depthR + up * upR;
            leftN = leftHit && leftSnapN.sqrMagnitude > 0.0001f ? leftSnapN.normalized : n;
            rightN = rightHit && rightSnapN.sqrMagnitude > 0.0001f ? rightSnapN.normalized : n;
        }

        /// <summary>
        /// Stick approx to mesh with raycast along ±normal only (approx+n*0.4 toward -n, fallback +n).
        /// Does not use ClosestPoint — avoids L/R lateral collapse on thin/curved colliders.
        /// </summary>
        private static bool RaycastAlongNormal(
            Vector3 approx,
            Vector3 hintNormal,
            List<Collider> colliders,
            out Vector3 pos,
            out Vector3 normal)
        {
            pos = approx;
            normal = hintNormal.sqrMagnitude > 0.0001f ? hintNormal.normalized : Vector3.up;
            if (colliders == null || colliders.Count == 0)
                return false;

            Vector3 hint = normal;
            float best = float.MaxValue;
            bool any = false;

            Vector3[] origins = { approx + hint * 0.4f, approx - hint * 0.4f };
            Vector3[] dirs = { -hint, hint };
            float[] bias = { 0f, 0.0005f }; // prefer into-surface (-n) over fallback

            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;
                for (int k = 0; k < origins.Length; k++)
                {
                    if (!col.Raycast(new Ray(origins[k], dirs[k]), out RaycastHit hit, 1.2f))
                        continue;
                    float score = (hit.point - approx).sqrMagnitude + bias[k];
                    if (score >= best)
                        continue;
                    best = score;
                    pos = hit.point;
                    normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : hint;
                    any = true;
                }
            }

            return any;
        }

        private static bool ClosestOnColliders(Vector3 approx, List<Collider> colliders, out Vector3 closest)
        {
            closest = approx;
            float best = float.MaxValue;
            bool any = false;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;
                Vector3 cp = col.ClosestPoint(approx);
                float d = (cp - approx).sqrMagnitude;
                if (d >= best)
                    continue;
                best = d;
                closest = cp;
                any = true;
            }
            return any;
        }

        /// <summary>Project an offset point back onto bake MeshColliders along +/- hint normal.</summary>
        private static bool SnapOffsetToSurface(
            Vector3 approx,
            Vector3 hintNormal,
            List<Collider> colliders,
            out Vector3 pos,
            out Vector3 normal)
        {
            // Manual / misc: prefer ±normal ray; do not feed ClosestPoint as a seed (lateral collapse risk).
            return RaycastAlongNormal(approx, hintNormal, colliders, out pos, out normal);
        }

        private void AddManualAtSceneViewCenter()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null || _probeSet == null)
                return;
            Ray ray = HandleUtility.GUIPointToWorldRay(new Vector2(sv.position.width * 0.5f, sv.position.height * 0.5f));
            TryAddProbeFromRay(ray);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_target == null)
                return;

            if (_probeSet == null)
                _probeSet = _target.GetComponent<DMClimbProbeSet>();
            if (_probeSet == null)
                return;

            SyncEditorSelection(_selectedIndex);
            Event e = Event.current;
            if (e == null)
                return;

            // Draw pickable spheres + move handles for selected / manual probes.
            DrawProbeSceneOverlays(sceneView);

            // Only steal Scene clicks in Manual Place. Otherwise Unity must keep normal object picking.
            // (Previously AddDefaultControl ran whenever probes existed and blocked selecting other assets.)
            bool bakerFocused = EditorWindow.focusedWindow == this;
            if (e.type == EventType.Layout && _manualPlace)
            {
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && !e.control && !e.command)
            {
                if (EditorWindow.mouseOverWindow != sceneView)
                    return;
                // Don't steal when already dragging a handle.
                if (GUIUtility.hotControl != 0 && _draggingHandle)
                    return;

                // Probe pick / place only while baker is focused or Manual Place is on.
                if (!bakerFocused && !_manualPlace)
                    return;

                int hitProbe = PickProbeIndex(e.mousePosition);
                if (hitProbe >= 0)
                {
                    _selectedIndex = hitProbe;
                    SyncEditorSelection(hitProbe);
                    e.Use();
                    Repaint();
                    sceneView.Repaint();
                    return;
                }

                if (_manualPlace)
                {
                    ResolveProbeSet(addIfMissing: true);
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    if (TryAddProbeFromRay(ray))
                    {
                        e.Use();
                        sceneView.Repaint();
                        Repaint();
                    }
                }
            }
        }

        private void DrawProbeSceneOverlays(SceneView sceneView)
        {
            if (_probeSet == null || _probeSet.Count == 0)
                return;

            float scale = Mathf.Max(0.05f, _gizmoScale);
            for (int i = 0; i < _probeSet.Count; i++)
            {
                if (!_probeSet.GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out _))
                    continue;

                bool isSel = i == _selectedIndex;
                DMClimbProbeSet.Probe p = _probeSet.Probes[i];
                bool showHandle = isSel; // one PositionHandle at a time (selected only)

                Color col = isSel ? _selectedColor : _gizmoColor;
                Handles.color = col;
                float rad = Mathf.Max(0.02f, r) * scale;
                // Solid disc facing camera for pick reliability + selected highlight.
                Handles.DrawSolidDisc(pos, sceneView.camera != null ? sceneView.camera.transform.forward : n, rad * (isSel ? 1.05f : 0.85f));
                Handles.DrawWireDisc(pos, n, rad);
                Handles.DrawLine(pos, pos + n * (rad * 2.2f));

                if (!showHandle)
                    continue;

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(pos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    _draggingHandle = true;
                    _selectedIndex = i;
                    SyncEditorSelection(i);
                    if (ProjectOntoSurface(newPos, n, out Vector3 snappedPos, out Vector3 snappedN))
                    {
                        Undo.RecordObject(_probeSet, "Move Climb Probe");
                        _probeSet.SetProbeWorldPose(i, snappedPos, snappedN, markManual: true);
                        EditorUtility.SetDirty(_probeSet);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(_probeSet);
                    }
                    else
                    {
                        // Soft fallback: keep handle pos projected via ClosestPoint without normal update if possible.
                        Undo.RecordObject(_probeSet, "Move Climb Probe");
                        Vector3 soft = SoftClosestOnTarget(newPos);
                        _probeSet.SetProbeWorldPose(i, soft, n, markManual: true);
                        EditorUtility.SetDirty(_probeSet);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(_probeSet);
                    }
                    sceneView.Repaint();
                    Repaint();
                }
            }

            if (Event.current != null && Event.current.type == EventType.MouseUp)
                _draggingHandle = false;
        }

        private int PickProbeIndex(Vector2 guiPoint)
        {
            if (_probeSet == null)
                return -1;
            float scale = Mathf.Max(0.05f, _gizmoScale);
            int best = -1;
            float bestDist = 18f; // pixels
            for (int i = 0; i < _probeSet.Count; i++)
            {
                if (!_probeSet.GetWorldPose(i, out Vector3 pos, out _, out float r, out _))
                    continue;
                float rad = Mathf.Max(0.02f, r) * scale;
                float d = HandleUtility.DistanceToCircle(pos, rad);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        private bool ProjectOntoSurface(Vector3 approxWorld, Vector3 hintNormal, out Vector3 pos, out Vector3 normal)
        {
            pos = approxWorld;
            normal = hintNormal.sqrMagnitude > 0.0001f ? hintNormal.normalized : Vector3.up;
            if (_target == null)
                return false;

            CollectBakeTargets(out List<Collider> colliders, out _);
            if (colliders.Count == 0)
            {
                // Try any collider under target.
                var childColliders = _target.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < childColliders.Length; i++)
                {
                    if (childColliders[i] != null && childColliders[i].enabled)
                        colliders.Add(childColliders[i]);
                }
            }
            if (colliders.Count == 0)
                return false;

            Vector3 hint = hintNormal.sqrMagnitude > 0.0001f ? hintNormal.normalized : Vector3.up;
            // Candidate seeds: ClosestPoint on each collider, then raycast back along +/- hint and from camera.
            Vector3 bestPoint = approxWorld;
            Vector3 bestNormal = hint;
            float bestScore = float.MaxValue;
            bool hitSurface = false;

            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;

                Vector3 cp = col.ClosestPoint(approxWorld);
                float dist = (cp - approxWorld).sqrMagnitude;
                // Ray from outside along -hint through cp to recover normal.
                Vector3[] origins =
                {
                    cp + hint * 0.35f,
                    cp - hint * 0.35f,
                    approxWorld + hint * 0.5f,
                    approxWorld - hint * 0.5f,
                };
                Vector3[] dirs =
                {
                    -hint,
                    hint,
                    -hint,
                    hint,
                };
                for (int k = 0; k < origins.Length; k++)
                {
                    if (!col.Raycast(new Ray(origins[k], dirs[k]), out RaycastHit hit, 2f))
                        continue;
                    float score = (hit.point - approxWorld).sqrMagnitude + dist * 0.01f;
                    if (score >= bestScore)
                        continue;
                    bestScore = score;
                    bestPoint = hit.point;
                    bestNormal = hit.normal.normalized;
                    hitSurface = true;
                }

                // If ClosestPoint alone is closer and we have no ray yet, keep it.
                if (!hitSurface && dist < bestScore)
                {
                    bestScore = dist;
                    bestPoint = cp;
                    bestNormal = hint;
                    hitSurface = true;
                }
            }

            // Camera ray through approx point as extra vote.
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                Vector2 gui = HandleUtility.WorldToGUIPoint(approxWorld);
                Ray camRay = HandleUtility.GUIPointToWorldRay(gui);
                for (int i = 0; i < colliders.Count; i++)
                {
                    if (colliders[i] == null)
                        continue;
                    if (!colliders[i].Raycast(camRay, out RaycastHit hit, 500f))
                        continue;
                    float score = (hit.point - approxWorld).sqrMagnitude;
                    if (score >= bestScore)
                        continue;
                    bestScore = score;
                    bestPoint = hit.point;
                    bestNormal = hit.normal.normalized;
                    hitSurface = true;
                }
            }

            if (!hitSurface)
                return false;
            pos = bestPoint;
            normal = bestNormal;
            return true;
        }

        private Vector3 SoftClosestOnTarget(Vector3 approxWorld)
        {
            CollectBakeTargets(out List<Collider> colliders, out _);
            if (colliders.Count == 0)
                return approxWorld;
            Vector3 best = approxWorld;
            float bestDist = float.MaxValue;
            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] == null)
                    continue;
                Vector3 cp = colliders[i].ClosestPoint(approxWorld);
                float d = (cp - approxWorld).sqrMagnitude;
                if (d >= bestDist)
                    continue;
                bestDist = d;
                best = cp;
            }
            return best;
        }

        private bool TryAddProbeFromRay(Ray ray)
        {
            if (_probeSet == null || _target == null)
                return false;

            CollectBakeTargets(out List<Collider> colliders, out _);
            RaycastHit best = default;
            bool any = false;
            float bestDist = float.MaxValue;

            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] == null)
                    continue;
                if (!colliders[i].Raycast(ray, out RaycastHit cand, 500f))
                    continue;
                if (cand.distance >= bestDist)
                    continue;
                bestDist = cand.distance;
                best = cand;
                any = true;
            }

            if (!any)
            {
                if (Physics.Raycast(ray, out RaycastHit phys, 500f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (phys.collider != null &&
                        (phys.collider.transform == _target.transform ||
                         phys.collider.transform.IsChildOf(_target.transform)))
                    {
                        best = phys;
                        any = true;
                    }
                }
            }

            if (!any)
                return false;

            Undo.RecordObject(_probeSet, "Add Climb Probe");
            Vector3 localPos = _probeSet.transform.InverseTransformPoint(best.point);
            Vector3 localN = _probeSet.transform.InverseTransformDirection(best.normal).normalized;
            var type = DMClimbProbeSet.ProbeType.Face;
            Renderer[] rends = _target.GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                    b.Encapsulate(rends[i].bounds);
                if (best.point.y >= b.max.y - Mathf.Max(0.25f, b.size.y * 0.12f) && best.normal.y > 0.2f)
                    type = DMClimbProbeSet.ProbeType.Lip;
            }

            _probeSet.AddProbe(localPos, localN, _defaultRadius, type, isManual: true, pairId: -1, hand: DMClimbProbeSet.HandSide.None);
            ApplyGizmoStyle();
            EditorUtility.SetDirty(_probeSet);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_probeSet);
            _selectedIndex = _probeSet.Count - 1;
            SyncEditorSelection(_selectedIndex);
            Debug.Log($"[Climb Probe Baker] Manual probe #{_selectedIndex} ({type}) on '{_target.name}'.");
            return true;
        }
    }
}
