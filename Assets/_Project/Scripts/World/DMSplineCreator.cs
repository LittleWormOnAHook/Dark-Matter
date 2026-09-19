using System.Collections.Generic;
using MalbersAnimations.PathCreation;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Path Creator–style Bézier authoring (via <see cref="PathCreator"/>) plus one prefab per anchor
    /// and optional sagging electrical mesh along the vertex path.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PathCreator))]
    [AddComponentMenu("Dark Matter/World/Spline Creator")]
    public class DMSplineCreator : MonoBehaviour
    {
        public enum CreatorMode
        {
            ElectricalLine = 0,
            ObjectPlacerOnly = 1
        }

        [SerializeField] private CreatorMode mode = CreatorMode.ElectricalLine;

        [Header("Anchor asset")]
        [SerializeField] private GameObject anchorPrefab;
        [SerializeField] [Min(2)] private int anchorPointCount = 3;
        [SerializeField] private float anchorSpacing = 8f;
        [SerializeField] private DMSplinePrefabUpAxis prefabUpAxis = DMSplinePrefabUpAxis.PositiveY;
        [SerializeField] private DMSplinePrefabUpAxis prefabForwardAxis = DMSplinePrefabUpAxis.PositiveZ;
        [SerializeField] private bool alignPrefabToSurfaceNormal = true;
        [SerializeField] private bool alignPrefabFacingPathTangent = true;
        [SerializeField] private bool autoWireAttachFromRendererTop;
        [SerializeField] private float anchorHeightOffset;

        public enum LineRoutingMode
        {
            /// <summary>Survey wire between anchor attach points (recommended for beacons/stakes).</summary>
            AnchorToAnchor = 0,
            /// <summary>Dense mesh along Path Creator vertex path (can look noisy on long paths).</summary>
            FollowVertexPath = 1
        }

        [Header("Snap (anchors)")]
        [SerializeField] private LayerMask snapMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float snapRayStartHeight = 40f;
        [SerializeField] private float snapRayLength = 120f;

        [Header("Electrical line mesh")]
        [SerializeField] private LineRoutingMode lineRouting = LineRoutingMode.AnchorToAnchor;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color linePreviewColor = new Color(0.35f, 0.35f, 0.38f, 1f);
        [SerializeField] private float lineWidth = 0.12f;
        [Tooltip("Rotates the cable cross-section (90 = typical Line Renderer style ribbon).")]
        [SerializeField] private float lineCrossSectionTwistDegrees = 90f;
        [SerializeField] private int segmentsPerAnchorSpan = 10;
        [SerializeField] private float sagMeters = 0.35f;
        [Tooltip("Wire height along prefab up axis from anchor pivot (e.g. top of beacon).")]
        [SerializeField] private float lineAttachOffsetAlongUp = 2.5f;
        [Tooltip("When off, wire runs anchor 0→1→…→last only (open). When on, adds last→first segment (closed loop).")]
        [SerializeField] private bool wireConnectEndToStart;
        [SerializeField] private bool addDiscAtAnchors;
        [SerializeField] private float discRadius = 0.35f;
        [SerializeField] private int discSegments = 24;

        [Header("Edit preview (Scene view only)")]
        [SerializeField] private bool showEditModeAnchorPreview = true;
        [SerializeField] private float editPreviewPivotSphereRadius = 0.4f;
        [SerializeField] private float editPreviewWireHookSphereRadius = 0.26f;

        [Header("Live update")]
        [SerializeField] private bool lockObjectsToPathAnchors = true;
        [SerializeField] private bool autoRebuildLineMesh = true;

        [Header("Rope wiggle (preview / runtime)")]
        [SerializeField] private bool enableRopeWiggle;
        [SerializeField] private bool ropeWiggleInEditMode = true;
        [SerializeField] private Vector3 ropeWiggleAmplitudeDirectional = new Vector3(0.06f, 0.015f, 0.008f);
        [SerializeField] private float ropeWiggleFrequency = 1.4f;
        [SerializeField] private float ropeWiggleTravelSpeed = 1.1f;

        [SerializeField] private List<DMSplineAnchorSettings> anchorSettings = new List<DMSplineAnchorSettings>();

        [SerializeField] private Transform anchorsRoot;
        [SerializeField] private Transform lineMeshRoot;

        [SerializeField] private PathCreator pathCreator;
        [SerializeField] private DMSplineRopeWiggle ropeWiggle;

        private bool suppressPathCallbacks;
        private bool isRebuildingElectricalMesh;
        private Vector3[] ropeRestVertices;
        private Vector3[] ropeWiggleTangents;
        private Vector3[] ropeWiggleBinormals;
        private Vector3[] ropeWiggleUps;
        private Vector3[] syncCachePathLocal;
        private Vector3[] syncCacheObjectLocal;
        private Vector3[] syncCacheAttachWorld;
        private bool[] ropeSamplePinned;
        private int[] ropePinnedStripSampleIndices;

        public CreatorMode Mode => mode;
        public GameObject AnchorPrefab => anchorPrefab;
        public int DesiredAnchorCount => anchorPointCount;
        public PathCreator PathCreator => pathCreator != null ? pathCreator : pathCreator = GetComponent<PathCreator>();

        public void EnsurePathInitialized()
        {
            PathCreator pc = PathCreator;
            if (pc == null)
                return;
            pc.InitializeEditorData(false);
        }

        public void SetMode(CreatorMode value) => mode = value;

        public void SetAnchorPrefab(GameObject prefab)
        {
            anchorPrefab = prefab;
            SyncAnchorObjects(true);
        }

        public int TargetAnchorCount => Mathf.Max(2, anchorPointCount);

        public int PlacedAnchorCount
        {
            get
            {
                EnsureRoots();
                return anchorsRoot != null ? anchorsRoot.childCount : 0;
            }
        }

        public int AnchorCount
        {
            get
            {
                if (!TryGetBezierPath(out BezierPath bezier))
                    return TargetAnchorCount;
                return bezier.NumAnchorPoints;
            }
        }

        public int LineAnchorCount => Mathf.Max(PlacedAnchorCount, TargetAnchorCount, AnchorCount);

        public bool ShowEditModeAnchorPreview => showEditModeAnchorPreview;
        public float EditPreviewPivotSphereRadius => editPreviewPivotSphereRadius;
        public float EditPreviewWireHookSphereRadius => editPreviewWireHookSphereRadius;
        public float LineAttachOffsetAlongUp => lineAttachOffsetAlongUp;
        public bool WireConnectEndToStart => wireConnectEndToStart;

        public void SetDesiredAnchorCount(int count)
        {
            anchorPointCount = Mathf.Max(2, count);
            ApplyAnchorCountToBezierPath();
        }

        /// <summary>Resize Bézier anchors, then place/update anchor prefab objects.</summary>
        public void ApplyAnchorCountAndPrefab(bool replacePrefabs)
        {
            ApplyAnchorCountToBezierPath();
            EnsureBezierMatchesTargetAnchorCount();
            SyncAnchorObjects(replacePrefabs || anchorPrefab != null);
            PlaceAnchorsOnPathAndOrient();
            RefreshSyncCacheFromScene();
            if (mode == CreatorMode.ElectricalLine && autoRebuildLineMesh)
                RebuildElectricalMesh();
        }

        /// <summary>
        /// Snaps anchors to path points, orients prefab axis at each path anchor, then writes back to the Bézier path.
        /// </summary>
        public void PlaceAnchorsOnPathAndOrient()
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            PushBezierAnchorsToAnchorTransforms();
            SnapAllAnchorsToSurface();

            int count = LineAnchorCount;
            for (int i = 0; i < count; i++)
                OrientAnchorAtPathPoint(i);

            CalibrateWireAttachForUnpinnedAnchors();

            PushAnchorTransformsToBezier();
        }

        public void EnsureBezierMatchesTargetAnchorCount()
        {
            if (!TryGetBezierPath(out BezierPath bezier))
                return;

            if (bezier.NumAnchorPoints != TargetAnchorCount)
                ApplyAnchorCountToBezierPath();
        }

        public void CalibrateAllWireAttachOffsetsFromRenderers()
        {
            int count = PlacedAnchorCount;
            for (int i = 0; i < count; i++)
                CalibrateWireAttachOffsetFromRenderer(i, forcePin: true);
        }

        public void CalibrateWireAttachForUnpinnedAnchors()
        {
            int count = PlacedAnchorCount;
            for (int i = 0; i < count; i++)
            {
                DMSplineAnchorSettings settings = GetAnchorSettingsSafe(i);
                if (settings.wireAttachPinned || HasPrefabWireAttachPoint(i))
                    continue;

                if (autoWireAttachFromRendererTop)
                    CalibrateWireAttachOffsetFromRenderer(i, forcePin: true);
                else
                    PinDefaultAttachOffset(i);
            }
        }

        public bool HasPrefabWireAttachPoint(int anchorIndex)
        {
            return ResolveWireAttachTransform(GetAnchorTransform(anchorIndex)) != null;
        }

        public void CalibrateWireAttachOffsetFromRenderer(int anchorIndex, bool forcePin = true)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return;

            if (ResolveWireAttachTransform(anchor) != null)
                return;

            Renderer[] renderers = anchor.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                PinDefaultAttachOffset(anchorIndex);
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int r = 1; r < renderers.Length; r++)
                worldBounds.Encapsulate(renderers[r].bounds);

            EnsureAnchorSettingsSize(Mathf.Max(anchorIndex + 1, TargetAnchorCount));
            DMSplineAnchorSettings settings = anchorSettings[anchorIndex];
            Vector3 modelUp = DMSplineAxisUtility.ResolveUpAxis(prefabUpAxis, settings);
            modelUp.Normalize();

            float maxAlongUp = float.NegativeInfinity;
            Vector3[] corners = GetBoundsCorners(worldBounds);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 local = anchor.InverseTransformPoint(corners[c]);
                float proj = Vector3.Dot(local, modelUp);
                if (proj > maxAlongUp)
                    maxAlongUp = proj;
            }

            if (float.IsNegativeInfinity(maxAlongUp))
            {
                PinDefaultAttachOffset(anchorIndex);
                return;
            }

            settings.useCustomAttachLocalOffset = true;
            settings.attachLocalOffset = modelUp * maxAlongUp;
            if (forcePin)
                settings.wireAttachPinned = true;
            anchorSettings[anchorIndex] = settings;
        }

        private void PinDefaultAttachOffset(int anchorIndex)
        {
            EnsureAnchorSettingsSize(Mathf.Max(anchorIndex + 1, TargetAnchorCount));
            DMSplineAnchorSettings settings = anchorSettings[anchorIndex];
            if (settings.wireAttachPinned || settings.useCustomAttachLocalOffset)
                return;

            Vector3 modelUp = DMSplineAxisUtility.ResolveUpAxis(prefabUpAxis, settings);
            float offset = lineAttachOffsetAlongUp + settings.lineAttachOffset;
            settings.useCustomAttachLocalOffset = true;
            settings.attachLocalOffset = modelUp * offset;
            settings.wireAttachPinned = true;
            anchorSettings[anchorIndex] = settings;
        }

        private static Transform ResolveWireAttachTransform(Transform anchorRoot)
        {
            if (anchorRoot == null)
                return null;

            DMSplineWireAttachPoint marker = anchorRoot.GetComponentInChildren<DMSplineWireAttachPoint>(true);
            if (marker != null)
                return marker.transform;

            Transform named = anchorRoot.Find("WireAttach");
            return named;
        }

        private static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            return new[]
            {
                c + new Vector3(e.x, e.y, e.z),
                c + new Vector3(e.x, e.y, -e.z),
                c + new Vector3(e.x, -e.y, e.z),
                c + new Vector3(e.x, -e.y, -e.z),
                c + new Vector3(-e.x, e.y, e.z),
                c + new Vector3(-e.x, e.y, -e.z),
                c + new Vector3(-e.x, -e.y, e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
            };
        }

        public void OrientAnchorAtPathPoint(int anchorIndex)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return;

            DMSplineAnchorSettings settings = GetAnchorSettingsSafe(anchorIndex);
            Vector3 modelUp = DMSplineAxisUtility.ResolveUpAxis(prefabUpAxis, settings);
            Vector3 modelForward = DMSplineAxisUtility.ToVector(prefabForwardAxis);

            Vector3 worldUp = alignPrefabToSurfaceNormal ? anchor.up : Vector3.up;
            if (worldUp.sqrMagnitude < 0.0001f)
                worldUp = Vector3.up;
            worldUp.Normalize();

            Vector3 worldForward = alignPrefabFacingPathTangent
                ? GetAnchorPathTangentWorld(anchorIndex)
                : transform.forward;
            worldForward = Vector3.ProjectOnPlane(worldForward, worldUp);
            if (worldForward.sqrMagnitude < 0.0001f)
                worldForward = Vector3.Cross(worldUp, transform.forward);
            worldForward.Normalize();

            anchor.rotation = DMSplineAxisUtility.RotationFromUpAndForward(
                modelUp,
                modelForward,
                worldUp,
                worldForward);
        }

        public Vector3 GetAnchorPathTangentWorld(int anchorIndex)
        {
            if (!TryGetBezierPath(out BezierPath bezier))
                return transform.forward;

            int anchorCount = AnchorCount;
            Vector3 localTangent;
            int pointIndex = anchorIndex * 3;
            if (anchorIndex <= 0)
            {
                int next = Mathf.Min(3, bezier.NumPoints - 1);
                localTangent = bezier.GetPoint(next) - bezier.GetPoint(0);
            }
            else if (anchorIndex >= anchorCount - 1)
            {
                int prev = (anchorCount - 2) * 3;
                prev = Mathf.Clamp(prev, 0, bezier.NumPoints - 1);
                localTangent = bezier.GetPoint(bezier.NumPoints - 1) - bezier.GetPoint(prev);
            }
            else
            {
                localTangent = bezier.GetPoint((anchorIndex + 1) * 3) - bezier.GetPoint((anchorIndex - 1) * 3);
            }

            if (localTangent.sqrMagnitude < 0.0001f)
                localTangent = Vector3.forward;
            return transform.TransformDirection(localTangent.normalized);
        }

        public IReadOnlyList<DMSplineAnchorSettings> AnchorSettings => anchorSettings;

        public Transform GetAnchorTransform(int anchorIndex)
        {
            EnsureRoots();
            if (anchorsRoot == null || anchorIndex < 0 || anchorIndex >= anchorsRoot.childCount)
                return null;

            return anchorsRoot.GetChild(anchorIndex);
        }

        public Color GetAnchorColor(int anchorIndex)
        {
            EnsureAnchorSettingsSize();
            if (anchorIndex < 0 || anchorIndex >= anchorSettings.Count)
                return linePreviewColor;
            return anchorSettings[anchorIndex].gizmoColor;
        }

        public void SetAnchorColor(int anchorIndex, Color color)
        {
            EnsureAnchorSettingsSize();
            if (anchorIndex < 0 || anchorIndex >= anchorSettings.Count)
                return;
            DMSplineAnchorSettings s = anchorSettings[anchorIndex];
            s.gizmoColor = color;
            anchorSettings[anchorIndex] = s;
        }

        public void SetPinnedWireAttachLocalOffset(int anchorIndex, Vector3 localOffset)
        {
            EnsureAnchorSettingsSize(Mathf.Max(anchorIndex + 1, TargetAnchorCount));
            if (anchorIndex < 0 || anchorIndex >= anchorSettings.Count)
                return;

            DMSplineAnchorSettings settings = anchorSettings[anchorIndex];
            settings.useCustomAttachLocalOffset = true;
            settings.attachLocalOffset = localOffset;
            settings.wireAttachPinned = true;
            anchorSettings[anchorIndex] = settings;
        }

        public void SetPinnedWireAttachWorldPosition(int anchorIndex, Vector3 worldPosition)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return;

            SetPinnedWireAttachLocalOffset(anchorIndex, anchor.InverseTransformPoint(worldPosition));
        }

        /// <summary>
        /// Applies <see cref="lineAttachOffsetAlongUp"/> (+ per-anchor offset) to every wire hook, then rebuilds the cable.
        /// </summary>
        public void ApplyDefaultAttachHeightToAllAnchors()
        {
            EnsureRoots();
            int count = PlacedAnchorCount;
            if (count == 0)
                return;

            EnsureAnchorSettingsSize(count);
            for (int i = 0; i < count; i++)
                ApplyDefaultAttachHeightToAnchor(i);

            InvalidateAttachCache();
            RefreshSyncCacheFromScene();
            if (mode == CreatorMode.ElectricalLine)
                RebuildElectricalMesh();
        }

        public void ApplyDefaultAttachHeightToAnchor(int anchorIndex)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return;

            EnsureAnchorSettingsSize(Mathf.Max(anchorIndex + 1, TargetAnchorCount));
            DMSplineAnchorSettings settings = anchorSettings[anchorIndex];
            Vector3 modelUp = DMSplineAxisUtility.ResolveUpAxis(prefabUpAxis, settings).normalized;
            float alongUp = lineAttachOffsetAlongUp + settings.lineAttachOffset;

            Transform wireChild = ResolveWireAttachTransform(anchor);
            if (wireChild != null && wireChild != anchor)
            {
                RecordTransformUndo(wireChild);
                Vector3 local = wireChild.localPosition;
                Vector3 radial = local - modelUp * Vector3.Dot(local, modelUp);
                wireChild.localPosition = radial + modelUp * alongUp;
                return;
            }

            settings.useCustomAttachLocalOffset = true;
            settings.attachLocalOffset = modelUp * alongUp;
            settings.wireAttachPinned = true;
            anchorSettings[anchorIndex] = settings;
        }

        public void SetEditPreviewSphereRadii(float pivotRadius, float wireHookRadius)
        {
            editPreviewPivotSphereRadius = Mathf.Max(0.05f, pivotRadius);
            editPreviewWireHookSphereRadius = Mathf.Max(0.05f, wireHookRadius);
        }

        private static void RecordTransformUndo(Transform t)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(t, "Apply Attach Height");
#endif
        }

        private void Reset()
        {
            pathCreator = GetComponent<PathCreator>();
            EnsureRoots();
        }

        private void OnEnable()
        {
            pathCreator = GetComponent<PathCreator>();
            EnsurePathInitialized();
            if (pathCreator != null)
                pathCreator.pathUpdated += HandlePathUpdated;

            if (!TryGetBezierPath(out _))
                return;

            ApplyWireLoopToBezierPath();

            if (mode == CreatorMode.ElectricalLine && autoRebuildLineMesh)
                RebuildElectricalMesh();
        }

        private void OnDisable()
        {
            if (pathCreator != null)
                pathCreator.pathUpdated -= HandlePathUpdated;
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !ShouldRunEditModeLiveUpdate())
                return;
#endif
            TickLiveAnchorAndMeshUpdate();
            ApplyRopeWiggleMotion();
        }

#if UNITY_EDITOR
        private bool ShouldRunEditModeLiveUpdate()
        {
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
                return false;

            if (UnityEditor.Selection.activeGameObject == gameObject)
                return true;

            if (anchorsRoot == null)
                EnsureRoots();

            Transform selected = UnityEditor.Selection.activeTransform;
            return selected != null && anchorsRoot != null && selected.IsChildOf(anchorsRoot);
        }
#endif

        private void TickLiveAnchorAndMeshUpdate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
                return;
#endif
            if (isRebuildingElectricalMesh || suppressPathCallbacks)
                return;

            if (lockObjectsToPathAnchors)
                SyncAnchorPathBinding();

            TryRebuildIfAttachPointsMoved();
        }

        public void SyncAnchorPathBinding()
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            EnsureBezierMatchesTargetAnchorCount();
            EnsureSyncCacheSize();
            bool pathChanged = false;
            suppressPathCallbacks = true;
            try
            {
                int count = PlacedAnchorCount;
                for (int i = 0; i < count; i++)
                {
                    int pointIndex = i * 3;
                    if (pointIndex >= bezier.NumPoints)
                        break;

                    Transform anchor = anchorsRoot.GetChild(i);
                    Vector3 pathLocal = bezier.GetPoint(pointIndex);
                    Vector3 objectLocal = anchor.localPosition;

                    if (float.IsPositiveInfinity(syncCachePathLocal[i].x))
                    {
                        syncCachePathLocal[i] = pathLocal;
                        syncCacheObjectLocal[i] = objectLocal;
                        continue;
                    }

                    bool pathMoved = (pathLocal - syncCachePathLocal[i]).sqrMagnitude > 0.0000001f;
                    bool objectMoved = (objectLocal - syncCacheObjectLocal[i]).sqrMagnitude > 0.0000001f;

                    // Object drag wins over path handle drift so stakes stay where you put them.
                    if (objectMoved)
                    {
                        bezier.MovePoint(pointIndex, objectLocal, true);
                        pathLocal = objectLocal;
                        pathChanged = true;
                    }
                    else if (pathMoved)
                    {
                        anchor.localPosition = pathLocal;
                        objectLocal = pathLocal;
                        pathChanged = true;
                    }

                    syncCachePathLocal[i] = pathLocal;
                    syncCacheObjectLocal[i] = objectLocal;
                }

                if (pathChanged)
                    bezier.NotifyPathModified();
            }
            finally
            {
                suppressPathCallbacks = false;
            }

            RefreshSyncCacheFromScene();
        }

        /// <summary>Rebuild wire mesh when anchor attach points move (position, rotation, or scale).</summary>
        public void TryRebuildIfAttachPointsMoved()
        {
            if (mode != CreatorMode.ElectricalLine || !autoRebuildLineMesh)
                return;

            if (isRebuildingElectricalMesh || suppressPathCallbacks)
                return;

            EnsureRoots();
            int count = PlacedAnchorCount;
            if (count < 2)
                return;

            EnsureAttachCacheSize(count);
            bool attachMoved = false;
            for (int i = 0; i < count; i++)
            {
                Vector3 attachWorld = GetLineAttachWorldPosition(i);
                if (float.IsPositiveInfinity(syncCacheAttachWorld[i].x))
                {
                    syncCacheAttachWorld[i] = attachWorld;
                    continue;
                }

                if ((attachWorld - syncCacheAttachWorld[i]).sqrMagnitude > 0.0000001f)
                    attachMoved = true;

                syncCacheAttachWorld[i] = attachWorld;
            }

            if (attachMoved)
                RebuildElectricalMesh();
        }

        public void RefreshAttachCacheFromScene()
        {
            EnsureRoots();
            int count = PlacedAnchorCount;
            EnsureAttachCacheSize(count);
            for (int i = 0; i < count; i++)
                syncCacheAttachWorld[i] = GetLineAttachWorldPosition(i);
        }

        private void EnsureAttachCacheSize(int count)
        {
            if (syncCacheAttachWorld == null || syncCacheAttachWorld.Length != count)
            {
                syncCacheAttachWorld = new Vector3[count];
                for (int i = 0; i < count; i++)
                    syncCacheAttachWorld[i] = Vector3.positiveInfinity;
            }
        }

        private void InvalidateAttachCache()
        {
            if (syncCacheAttachWorld == null)
                return;

            for (int i = 0; i < syncCacheAttachWorld.Length; i++)
                syncCacheAttachWorld[i] = Vector3.positiveInfinity;
        }

        public void RefreshSyncCacheFromScene()
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            EnsureSyncCacheSize();
            int count = PlacedAnchorCount;
            for (int i = 0; i < count; i++)
            {
                int pointIndex = i * 3;
                if (pointIndex >= bezier.NumPoints)
                    break;

                syncCachePathLocal[i] = bezier.GetPoint(pointIndex);
                syncCacheObjectLocal[i] = anchorsRoot.GetChild(i).localPosition;
            }
        }

        private void EnsureSyncCacheSize()
        {
            int count = Mathf.Max(PlacedAnchorCount, AnchorCount);
            if (syncCachePathLocal == null || syncCachePathLocal.Length != count)
            {
                syncCachePathLocal = new Vector3[count];
                syncCacheObjectLocal = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    syncCachePathLocal[i] = Vector3.positiveInfinity;
                    syncCacheObjectLocal[i] = Vector3.positiveInfinity;
                }
            }
        }

        private void HandlePathUpdated()
        {
            if (suppressPathCallbacks || isRebuildingElectricalMesh)
                return;

            EnsureRoots();
            if (anchorsRoot == null)
                return;

            if (TryGetBezierPath(out BezierPath bezier) && bezier.IsClosed != wireConnectEndToStart)
                ApplyWireLoopToBezierPath();

            ResizeAnchorChildren(LineAnchorCount);
            EnsureAnchorSettingsSize(LineAnchorCount);
            RebindAnchorIndices();

            // When stakes drive the path, do not snap objects back to Bézier on every path refresh.
            if (!lockObjectsToPathAnchors)
                PushBezierAnchorsToAnchorTransforms();

            RefreshSyncCacheFromScene();
            if (mode == CreatorMode.ElectricalLine && autoRebuildLineMesh)
                RebuildElectricalMesh();
        }

        private void RebindAnchorIndices()
        {
            if (anchorsRoot == null)
                return;

            for (int i = 0; i < anchorsRoot.childCount; i++)
            {
                DMSplineAnchorBind bind = anchorsRoot.GetChild(i).GetComponent<DMSplineAnchorBind>();
                if (bind != null)
                    bind.Bind(this, i);
            }
        }

        private void ApplyAnchorCountToBezierPath()
        {
            if (!TryGetBezierPath(out BezierPath bezier))
                return;

            int target = TargetAnchorCount;
            suppressPathCallbacks = true;
            try
            {
                while (bezier.NumAnchorPoints < target)
                {
                    Vector3 lastAnchor = bezier.GetPoint(bezier.NumPoints - 1);
                    Vector3 dir = transform.forward;
                    if (bezier.NumAnchorPoints >= 2)
                    {
                        Vector3 prevAnchor = bezier.GetPoint(bezier.NumPoints - 4);
                        dir = lastAnchor - prevAnchor;
                    }

                    if (dir.sqrMagnitude < 0.0001f)
                        dir = Vector3.forward * anchorSpacing;
                    else
                        dir = dir.normalized * anchorSpacing;

                    bezier.AddSegmentToEnd(lastAnchor + dir);
                }

                while (bezier.NumAnchorPoints > target && bezier.NumAnchorPoints > 2)
                    bezier.DeleteSegment(bezier.NumPoints - 1);
            }
            finally
            {
                suppressPathCallbacks = false;
            }

            ResizeAnchorChildren(target);
            EnsureAnchorSettingsSize(target);
            RebindAnchorIndices();
            PushBezierAnchorsToAnchorTransforms();
            PathCreator.TriggerPathUpdate();
        }

        private bool TryGetBezierPath(out BezierPath bezier)
        {
            bezier = null;
            PathCreator pc = PathCreator;
            if (pc == null)
                return false;
            pc.InitializeEditorData(false);
            bezier = pc.bezierPath;
            return bezier != null;
        }

        public void RebuildAll()
        {
            EnsurePathInitialized();
            EnsureRoots();
            SyncAnchorObjects(true);
            SnapAllAnchorsToSurface();
            PushAnchorTransformsToBezier();
            if (mode == CreatorMode.ElectricalLine)
                RebuildElectricalMesh();
            else
                ClearElectricalMesh();
        }

        public void SnapAllAnchorsToSurface()
        {
            EnsureRoots();
            for (int i = 0; i < AnchorCount; i++)
                SnapAnchorToSurface(i);
            PushAnchorTransformsToBezier();
        }

        public void SnapAnchorToSurface(int anchorIndex)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return;

            Vector3 world = anchor.position;
            Vector3 origin = world + Vector3.up * snapRayStartHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, snapRayLength, snapMask, QueryTriggerInteraction.Ignore))
            {
                anchor.position = hit.point + hit.normal * anchorHeightOffset;
            }

            OrientAnchorAtPathPoint(anchorIndex);
        }

        public Vector3 GetLineAttachLocalOffset(int anchorIndex)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            Transform wireAttach = ResolveWireAttachTransform(anchor);
            if (wireAttach != null && anchor != null)
                return anchor.InverseTransformPoint(wireAttach.position);

            DMSplineAnchorSettings settings = GetAnchorSettingsSafe(anchorIndex);
            if (settings.useCustomAttachLocalOffset || settings.wireAttachPinned)
                return settings.attachLocalOffset;

            Vector3 modelUp = DMSplineAxisUtility.ResolveUpAxis(prefabUpAxis, settings);
            float offset = lineAttachOffsetAlongUp + settings.lineAttachOffset;
            return modelUp * offset;
        }

        public Vector3 GetLineAttachWorldPosition(int anchorIndex)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return transform.position;

            return anchor.TransformPoint(GetLineAttachLocalOffset(anchorIndex));
        }

        public Vector3 GetAnchorUpWorld(int anchorIndex)
        {
            Transform anchor = GetAnchorTransform(anchorIndex);
            if (anchor == null)
                return Vector3.up;

            DMSplineAnchorSettings settings = GetAnchorSettingsSafe(anchorIndex);
            Vector3 modelUp = DMSplineAxisUtility.ResolveUpAxis(prefabUpAxis, settings);
            return anchor.TransformDirection(modelUp).normalized;
        }

        private DMSplineAnchorSettings GetAnchorSettingsSafe(int anchorIndex)
        {
            EnsureAnchorSettingsSize();
            if (anchorIndex < 0 || anchorIndex >= anchorSettings.Count)
                return new DMSplineAnchorSettings();
            return anchorSettings[anchorIndex];
        }

        /// <summary>After moving anchor objects in the scene, write positions into the Bézier path.</summary>
        public void PushAnchorTransformsToBezier()
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            int count = PlacedAnchorCount;
            suppressPathCallbacks = true;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Transform anchor = anchorsRoot.GetChild(i);
                    int pointIndex = i * 3;
                    if (pointIndex >= bezier.NumPoints)
                        break;
                    bezier.MovePoint(pointIndex, anchor.localPosition, true);
                }

                bezier.NotifyPathModified();
            }
            finally
            {
                suppressPathCallbacks = false;
            }

            PathCreator.TriggerPathUpdate();
        }

        /// <summary>After editing the Bézier path, move anchor objects to match anchor points.</summary>
        public void PushBezierAnchorsToAnchorTransforms()
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            suppressPathCallbacks = true;
            try
            {
                int count = PlacedAnchorCount;
                for (int i = 0; i < count; i++)
                {
                    int pointIndex = i * 3;
                    if (pointIndex >= bezier.NumPoints)
                        break;
                    anchorsRoot.GetChild(i).localPosition = bezier.GetPoint(pointIndex);
                }
            }
            finally
            {
                suppressPathCallbacks = false;
            }
        }

        /// <summary>Path anchor positions drive object pivots (keeps stakes/beacons on the spline).</summary>
        public void SyncAnchorsFromPathAnchors()
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            bool moved = false;
            suppressPathCallbacks = true;
            try
            {
                int count = PlacedAnchorCount;
                for (int i = 0; i < count; i++)
                {
                    int pointIndex = i * 3;
                    if (pointIndex >= bezier.NumPoints)
                        break;

                    Transform anchor = anchorsRoot.GetChild(i);
                    Vector3 target = bezier.GetPoint(pointIndex);
                    if ((anchor.localPosition - target).sqrMagnitude > 0.0000001f)
                    {
                        anchor.localPosition = target;
                        moved = true;
                    }
                }
            }
            finally
            {
                suppressPathCallbacks = false;
            }

            if (moved)
                TryRebuildIfAttachPointsMoved();
        }

        public void PushAnchorIndexToBezier(int anchorIndex)
        {
            if (!TryGetBezierPath(out BezierPath bezier) || anchorsRoot == null)
                return;

            if (anchorIndex < 0 || anchorIndex >= anchorsRoot.childCount)
                return;

            int pointIndex = anchorIndex * 3;
            if (pointIndex >= bezier.NumPoints)
                return;

            suppressPathCallbacks = true;
            try
            {
                bezier.MovePoint(pointIndex, anchorsRoot.GetChild(anchorIndex).localPosition, true);
                bezier.NotifyPathModified();
            }
            finally
            {
                suppressPathCallbacks = false;
            }

            PathCreator.TriggerPathUpdate();
        }

        public void SyncAnchorObjects(bool replacePrefabs)
        {
            if (!TryGetBezierPath(out _))
                return;

            EnsureRoots();
            EnsureBezierMatchesTargetAnchorCount();
            int anchorCount = TargetAnchorCount;
            EnsureAnchorSettingsSize(anchorCount);
            ResizeAnchorChildren(anchorCount);

            for (int i = 0; i < anchorCount; i++)
            {
                Transform anchor = anchorsRoot.GetChild(i);
                EnsureAnchorBind(anchor, i);

                if (anchorPrefab == null)
                {
                    anchor.name = $"Anchor_{i:00}";
                    continue;
                }

                GameObject source = GetPrefabSource(anchor.gameObject);
                bool isPrefabInstance = source == anchorPrefab;
                bool needsPrefab = replacePrefabs || !isPrefabInstance;

                if (!needsPrefab)
                {
                    anchor.name = $"Anchor_{i:00}_{anchorPrefab.name}";
                    continue;
                }

                Vector3 localPos = anchor.localPosition;
                Quaternion localRot = anchor.localRotation;
                Vector3 localScale = anchor.localScale;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(anchor.gameObject);
                else
#endif
                    Destroy(anchor.gameObject);

                GameObject instance = InstantiateAnchorPrefab(anchorPrefab, anchorsRoot);
                if (instance == null)
                    continue;

                instance.transform.SetSiblingIndex(i);
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                instance.transform.localScale = localScale;
                instance.name = $"Anchor_{i:00}_{anchorPrefab.name}";
                EnsureAnchorBind(instance.transform, i);
            }

            PushBezierAnchorsToAnchorTransforms();
            EnsureSyncCacheSize();
        }

        private void EnsureAnchorBind(Transform anchor, int index)
        {
            DMSplineAnchorBind bind = anchor.GetComponent<DMSplineAnchorBind>();
            if (bind == null)
                bind = anchor.gameObject.AddComponent<DMSplineAnchorBind>();
            bind.Bind(this, index);
        }

        public void RebuildElectricalMesh()
        {
            if (isRebuildingElectricalMesh)
                return;

            isRebuildingElectricalMesh = true;
            suppressPathCallbacks = true;
            try
            {
                RebuildElectricalMeshInternal();
            }
            finally
            {
                suppressPathCallbacks = false;
                isRebuildingElectricalMesh = false;
            }
        }

        private void RebuildElectricalMeshInternal()
        {
            EnsureRoots();
            int lineAnchors = PlacedAnchorCount;
            if (lineMeshRoot == null || PathCreator == null || lineAnchors < 2)
            {
                ClearElectricalMesh();
                return;
            }

            Vector3 localSag = lineMeshRoot.InverseTransformDirection(Vector3.down);
            DMSplineElectricalMeshBuilder.BuildResult buildResult;

            if (lineRouting == LineRoutingMode.AnchorToAnchor)
            {
                var attachLocal = new List<Vector3>(lineAnchors);
                var attachNormals = new List<Vector3>(lineAnchors);
                for (int i = 0; i < lineAnchors; i++)
                {
                    attachLocal.Add(lineMeshRoot.InverseTransformPoint(GetLineAttachWorldPosition(i)));
                    attachNormals.Add(lineMeshRoot.InverseTransformDirection(GetAnchorUpWorld(i)));
                }

                buildResult = DMSplineElectricalMeshBuilder.Build(
                    attachLocal,
                    attachNormals,
                    lineWidth,
                    segmentsPerAnchorSpan,
                    sagMeters,
                    localSag,
                    addDiscAtAnchors,
                    discRadius,
                    discSegments,
                    lineCrossSectionTwistDegrees,
                    wireConnectEndToStart);
            }
            else
            {
                VertexPath vertexPath = PathCreator != null ? PathCreator.path : null;
                if (vertexPath == null || vertexPath.NumPoints < 2)
                {
                    ClearElectricalMesh();
                    return;
                }

                var localSamples = new List<Vector3>(vertexPath.NumPoints);
                var localUps = new List<Vector3>(vertexPath.NumPoints);
                for (int i = 0; i < vertexPath.NumPoints; i++)
                {
                    localSamples.Add(lineMeshRoot.InverseTransformPoint(transform.TransformPoint(vertexPath.GetPoint(i))));
                    localUps.Add(lineMeshRoot.InverseTransformDirection(transform.up));
                }

                var discCenters = new List<Vector3>();
                var discNormals = new List<Vector3>();
                if (addDiscAtAnchors)
                {
                    for (int i = 0; i < lineAnchors; i++)
                    {
                        discCenters.Add(lineMeshRoot.InverseTransformPoint(GetLineAttachWorldPosition(i)));
                        discNormals.Add(lineMeshRoot.InverseTransformDirection(GetAnchorUpWorld(i)));
                    }
                }

                buildResult = DMSplineElectricalMeshBuilder.BuildAlongPolyline(
                    localSamples,
                    localUps,
                    lineWidth,
                    sagMeters,
                    localSag,
                    addDiscAtAnchors,
                    discCenters,
                    discNormals,
                    discRadius,
                    discSegments,
                    lineCrossSectionTwistDegrees);
            }

            Mesh mesh = buildResult.Mesh;
            ropePinnedStripSampleIndices = buildResult.PinnedStripSampleIndices;

            if (mesh == null)
            {
                ClearElectricalMesh();
                return;
            }

            MeshFilter filter = lineMeshRoot.GetComponent<MeshFilter>();
            if (filter == null)
                filter = lineMeshRoot.gameObject.AddComponent<MeshFilter>();

            MeshRenderer renderer = lineMeshRoot.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = lineMeshRoot.gameObject.AddComponent<MeshRenderer>();

            Mesh previous = filter.sharedMesh;
            filter.sharedMesh = mesh;
            if (lineMaterial != null)
                renderer.sharedMaterial = lineMaterial;
#if UNITY_EDITOR
            else if (!Application.isPlaying && renderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                    renderer.sharedMaterial = new Material(shader);
            }
#endif

#if UNITY_EDITOR
            if (!Application.isPlaying && previous != null && previous != mesh)
                DestroyImmediate(previous);
#endif
            CacheRopeRestPose(mesh);
        }

        private void CacheRopeRestPose(Mesh mesh)
        {
            if (mesh == null)
            {
                ropeRestVertices = null;
                ropeWiggleBinormals = null;
                return;
            }

            ropeRestVertices = mesh.vertices;
            int sampleCount = ropeRestVertices.Length / 2;
            ropeWiggleTangents = new Vector3[sampleCount];
            ropeWiggleBinormals = new Vector3[sampleCount];
            ropeWiggleUps = new Vector3[sampleCount];
            for (int s = 0; s < sampleCount; s++)
            {
                int vi = s * 2;
                Vector3 center = (ropeRestVertices[vi] + ropeRestVertices[vi + 1]) * 0.5f;
                Vector3 tangent = Vector3.forward;
                if (s < sampleCount - 1)
                {
                    int nvi = (s + 1) * 2;
                    Vector3 next = (ropeRestVertices[nvi] + ropeRestVertices[nvi + 1]) * 0.5f;
                    tangent = next - center;
                }
                else if (s > 0)
                {
                    int pvi = (s - 1) * 2;
                    Vector3 prev = (ropeRestVertices[pvi] + ropeRestVertices[pvi + 1]) * 0.5f;
                    tangent = center - prev;
                }

                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = Vector3.forward;
                else
                    tangent.Normalize();

                Vector3 up = Vector3.up;
                Vector3 binormal = Vector3.Cross(tangent, up);
                if (binormal.sqrMagnitude < 0.0001f)
                    binormal = Vector3.Cross(tangent, Vector3.right);
                binormal.Normalize();
                up = Vector3.Cross(binormal, tangent).normalized;

                ropeWiggleTangents[s] = tangent;
                ropeWiggleBinormals[s] = binormal;
                ropeWiggleUps[s] = up;
            }

            EnsureRopeWiggleComponent();
            ropeSamplePinned = BuildPinnedSampleMask(sampleCount, ropePinnedStripSampleIndices);
            ropeWiggle?.ApplyRestPose(ropeRestVertices, ropeWiggleBinormals);
            RefreshAttachCacheFromScene();
        }

        private static bool[] BuildPinnedSampleMask(int sampleCount, int[] pinnedIndices)
        {
            if (sampleCount <= 0)
                return null;

            var mask = new bool[sampleCount];
            if (pinnedIndices == null)
                return mask;

            for (int i = 0; i < pinnedIndices.Length; i++)
            {
                int index = pinnedIndices[i];
                if (index >= 0 && index < sampleCount)
                    mask[index] = true;
            }

            return mask;
        }

        private void EnsureRopeWiggleComponent()
        {
            if (lineMeshRoot == null)
                return;

            if (ropeWiggle == null)
                ropeWiggle = lineMeshRoot.GetComponent<DMSplineRopeWiggle>();
            if (ropeWiggle == null)
                ropeWiggle = lineMeshRoot.gameObject.AddComponent<DMSplineRopeWiggle>();

            MeshFilter filter = lineMeshRoot.GetComponent<MeshFilter>();
            ropeWiggle.Bind(this, filter);
        }

        private void ApplyRopeWiggleMotion()
        {
            if (!enableRopeWiggle || ropeRestVertices == null || ropeWiggleBinormals == null)
                return;

            if (!Application.isPlaying && !ropeWiggleInEditMode)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying && !ShouldRunEditModeLiveUpdate())
                return;
#endif

            if (isRebuildingElectricalMesh)
                return;

            EnsureRopeWiggleComponent();
            if (ropeWiggle == null)
                return;

            float time = Time.realtimeSinceStartup;
            ropeWiggle.ApplyAnimatedPose(
                ropeRestVertices,
                ropeWiggleTangents,
                ropeWiggleBinormals,
                ropeWiggleUps,
                ropeSamplePinned,
                time,
                ropeWiggleAmplitudeDirectional,
                ropeWiggleFrequency,
                ropeWiggleTravelSpeed);
        }

        /// <summary>Rebuild cable geometry only (width, sag, segments). Wire attach points stay pinned.</summary>
        public void RebuildLineMeshAppearance()
        {
            if (mode == CreatorMode.ElectricalLine)
                RebuildElectricalMesh();
        }

        public void SetWireConnectEndToStart(bool connectLoop)
        {
            wireConnectEndToStart = connectLoop;
            ApplyWireLoopToBezierPath();
            if (mode == CreatorMode.ElectricalLine)
                RebuildElectricalMesh();
        }

        /// <summary>Keeps Path Creator Bézier closed flag aligned with survey wire loop setting.</summary>
        public void ApplyWireLoopToBezierPath()
        {
            if (!TryGetBezierPath(out BezierPath bezier))
                return;

            if (bezier.IsClosed == wireConnectEndToStart)
                return;

            suppressPathCallbacks = true;
            try
            {
                bezier.IsClosed = wireConnectEndToStart;
                bezier.NotifyPathModified();
            }
            finally
            {
                suppressPathCallbacks = false;
            }
        }

        private void ClearElectricalMesh()
        {
            if (lineMeshRoot == null)
                return;
            MeshFilter filter = lineMeshRoot.GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = null;
        }

        private void EnsureRoots()
        {
            anchorsRoot = EnsureChild(anchorsRoot, "Anchors");
            lineMeshRoot = EnsureChild(lineMeshRoot, "LineMesh");
        }

        private Transform EnsureChild(Transform existing, string name)
        {
            if (existing != null)
                return existing;

            Transform t = transform.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                UndoSafeRegister(go);
                t = go.transform;
                t.SetParent(transform, false);
            }

            return t;
        }

        private void EnsureAnchorSettingsSize(int count = -1)
        {
            if (count < 0)
                count = LineAnchorCount;
            while (anchorSettings.Count < count)
                anchorSettings.Add(new DMSplineAnchorSettings());
            while (anchorSettings.Count > count && count >= 0)
                anchorSettings.RemoveAt(anchorSettings.Count - 1);
        }

        private void ResizeAnchorChildren(int count)
        {
            EnsureRoots();
            while (anchorsRoot.childCount < count)
            {
                var slot = new GameObject($"Anchor_{anchorsRoot.childCount:00}");
                UndoSafeRegister(slot);
                slot.transform.SetParent(anchorsRoot, false);
            }

            while (anchorsRoot.childCount > count)
            {
                Transform child = anchorsRoot.GetChild(anchorsRoot.childCount - 1);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }
        }

        private GameObject InstantiateAnchorPrefab(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance != null)
                {
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                }

                return instance;
            }
#endif
            return Instantiate(prefab, parent);
        }

        private static GameObject GetPrefabSource(GameObject instance)
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance);
#else
            return null;
#endif
        }

        private void UndoSafeRegister(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Spline Creator");
#endif
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (mode != CreatorMode.ElectricalLine)
                return;

            Gizmos.color = new Color(0.55f, 0.75f, 0.95f, 0.9f);
            for (int i = 0; i < AnchorCount; i++)
            {
                Transform anchor = GetAnchorTransform(i);
                if (anchor == null)
                    continue;

                Vector3 attach = GetLineAttachWorldPosition(i);
                Gizmos.DrawLine(anchor.position, attach);
                Gizmos.DrawWireSphere(attach, lineWidth * 2f);
            }
        }

        private void OnValidate()
        {
            anchorPointCount = Mathf.Max(2, anchorPointCount);
            anchorSpacing = Mathf.Max(0.5f, anchorSpacing);
            lineWidth = Mathf.Max(0.01f, lineWidth);
            segmentsPerAnchorSpan = Mathf.Clamp(segmentsPerAnchorSpan, 2, 32);
            sagMeters = Mathf.Max(0f, sagMeters);
            discRadius = Mathf.Max(0.05f, discRadius);
            discSegments = Mathf.Clamp(discSegments, 8, 64);
            editPreviewPivotSphereRadius = Mathf.Max(0.05f, editPreviewPivotSphereRadius);
            editPreviewWireHookSphereRadius = Mathf.Max(0.05f, editPreviewWireHookSphereRadius);
        }
    }
}
