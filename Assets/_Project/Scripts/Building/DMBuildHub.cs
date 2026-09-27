using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Building
{
    /// <summary>
    /// 0926-build-hub: the placed Build Hub. Every other building piece must stand fully inside a hub's build zone:
    /// an axis-aligned box centred on the hub, 50 m wide and 50 m tall by default (25 m above and below the hub),
    /// growing by the profile's step (25 m each way) per upgrade level. Sizes live in Building Studio > Placement.
    /// The zone shows as a translucent grid box (Project/DMBuildZone) only while build mode is on.
    /// 0927-zone-anchor: the zone stays where the hub was FIRST placed. Moving the hub (MoveEquipment) never moves the zone;
    /// the anchor (centre + rotation) is saved with the hub. The box itself stays world axis-aligned.
    /// Added at build time by DMBuildingPlacementController.AttachPoweredParts, like DMBaseGenerator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMBuildHub : MonoBehaviour
    {
        public const string ZoneMaterialResource = "Building/DM_BuildZone";
        public const string ZoneShaderName = "Project/DMBuildZone";
        const float ContainTolerance = 0.02f;

        static readonly List<DMBuildHub> active = new List<DMBuildHub>(4);
        static Material zoneMaterial;
        static Mesh zoneMesh;
        static bool warnedMissingMaterial;

        /// <summary>Every enabled hub in the world.</summary>
        public static IReadOnlyList<DMBuildHub> Active => active;

        /// <summary>A hub was added, removed or changed level.</summary>
        public static event Action Changed;

        [Tooltip("Upgrade level. Each level adds the profile's horizontal step to the zone width/depth and the vertical step to its height.")]
        [SerializeField, Min(0)] int upgradeLevel;

        DMBuildingGhost ghost;
        GameObject zoneVisual;
        bool hasAnchor;
        Vector3 anchorCenter;
        Quaternion anchorRotation = Quaternion.identity;

        public int UpgradeLevel => upgradeLevel;
        public static int MaxLevel => DMBuildingGhostProfile.BuildHubMaxLevel;
        public bool CanUpgrade => upgradeLevel < MaxLevel;
        /// <summary>Zone centre: the anchor from first placement (or the save), not the hub's current position.</summary>
        public Vector3 Center
        {
            get
            {
                EnsureAnchor();
                return anchorCenter;
            }
        }

        /// <summary>Hub rotation at first placement (saved with the anchor; the zone box itself is axis-aligned).</summary>
        public Quaternion AnchorRotation
        {
            get
            {
                EnsureAnchor();
                return anchorRotation;
            }
        }

        /// <summary>The hub's own resting position (ignores the creation bounce). Differs from Center after a move.</summary>
        public Vector3 HubPosition => ghost != null ? DMBuildingCreationFx.RestPosition(ghost) : transform.position;
        public Vector3 ZoneSize => SizeForLevel(upgradeLevel);
        public Bounds Zone => new Bounds(Center, ZoneSize);

        /// <summary>True when at least one hub exists.</summary>
        public static bool AnyHub
        {
            get
            {
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    if (active[i] != null)
                        return true;
                    active.RemoveAt(i);
                }

                return false;
            }
        }

        /// <summary>Zone size (X, Y, Z) at an upgrade level: base size plus one step per level.</summary>
        public static Vector3 SizeForLevel(int level)
        {
            level = Mathf.Max(0, level);
            float horizontal = DMBuildingGhostProfile.BuildHubZoneMeters + DMBuildingGhostProfile.BuildHubZoneStepMeters * level;
            float vertical = DMBuildingGhostProfile.BuildHubZoneHeightMeters + DMBuildingGhostProfile.BuildHubZoneHeightStepMeters * level;
            return new Vector3(horizontal, vertical, horizontal);
        }

        /// <summary>True when the bounds sit fully inside at least one hub's zone.</summary>
        public static bool IsInsideAnyZone(Bounds bounds)
        {
            return ZoneFor(bounds) != null;
        }

        public static bool IsInsideAnyZone(Vector3 point)
        {
            return ZoneFor(point) != null;
        }

        /// <summary>The hub whose zone contains this point (highest level wins), or null.</summary>
        public static DMBuildHub ZoneFor(Vector3 point)
        {
            return ZoneFor(new Bounds(point, Vector3.zero));
        }

        /// <summary>The hub whose zone fully contains these bounds (highest level wins), or null.</summary>
        public static DMBuildHub ZoneFor(Bounds bounds)
        {
            DMBuildHub best = null;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                DMBuildHub hub = active[i];
                if (hub == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (!Contains(hub.Zone, bounds))
                    continue;
                if (best == null || hub.upgradeLevel >= best.upgradeLevel)
                    best = hub;
            }

            return best;
        }

        static bool Contains(Bounds zone, Bounds inner)
        {
            Vector3 zMin = zone.min;
            Vector3 zMax = zone.max;
            Vector3 iMin = inner.min;
            Vector3 iMax = inner.max;
            return iMin.x >= zMin.x - ContainTolerance && iMax.x <= zMax.x + ContainTolerance
                && iMin.y >= zMin.y - ContainTolerance && iMax.y <= zMax.y + ContainTolerance
                && iMin.z >= zMin.z - ContainTolerance && iMax.z <= zMax.z + ContainTolerance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            Changed = null;
            zoneMaterial = null;
            zoneMesh = null;
            warnedMissingMaterial = false;
        }

        // ---- Upgrade API ----

        /// <summary>Raises the zone one level. Returns false (and logs why) at max level or when the bigger zone is blocked.</summary>
        public bool Upgrade()
        {
            if (TryUpgrade(out string reason))
                return true;
            Debug.LogWarning("[DM Build Hub] " + name + " upgrade refused: " + reason + ".");
            return false;
        }

        public bool TryUpgrade(out string reason)
        {
            if (!CanUpgrade)
            {
                reason = "already at max level " + MaxLevel;
                return false;
            }

            return TrySetLevel(upgradeLevel + 1, out reason);
        }

        /// <summary>Sets the upgrade level (clamped to 0..max). Growing is refused (logged) when the bigger zone is blocked.</summary>
        public bool SetLevel(int level)
        {
            if (TrySetLevel(level, out string reason))
                return true;
            Debug.LogWarning("[DM Build Hub] " + name + " level change refused: " + reason + ".");
            return false;
        }

        public bool TrySetLevel(int level, out string reason)
        {
            reason = null;
            int clamped = Mathf.Clamp(level, 0, MaxLevel);
            if (clamped == upgradeLevel)
                return true;
            if (clamped > upgradeLevel && DMBuildingGhostProfile.BuildHubZoneMustBeClear)
            {
                Collider blocker = FindZoneBlocker(Center, SizeForLevel(clamped));
                if (blocker != null)
                {
                    reason = BlockedByText(blocker);
                    return false;
                }
            }

            ApplyLevel(clamped);
            return true;
        }

        /// <summary>Save load: puts the saved zone anchor back (old saves skip this and keep the hub position).</summary>
        public void RestoreAnchor(Vector3 center, Quaternion rotation)
        {
            hasAnchor = true;
            anchorCenter = center;
            anchorRotation = rotation;
            if (zoneVisual != null && zoneVisual.activeSelf)
                SyncVisual();
            Changed?.Invoke();
        }

        /// <summary>First call pins the zone to where the hub stands now (placement / save load); later moves keep it.</summary>
        void EnsureAnchor()
        {
            if (hasAnchor)
                return;
            if (ghost == null)
                ghost = GetComponent<DMBuildingGhost>();
            hasAnchor = true;
            anchorCenter = HubPosition;
            anchorRotation = transform.rotation;
        }

        /// <summary>Save load: puts the saved level back without the clear-zone check.</summary>
        public void RestoreLevel(int level)
        {
            ApplyLevel(Mathf.Clamp(level, 0, MaxLevel));
        }

        void ApplyLevel(int level)
        {
            if (level == upgradeLevel)
                return;
            upgradeLevel = level;
            if (zoneVisual != null && zoneVisual.activeSelf)
                SyncVisual();
            Changed?.Invoke();
        }

        [ContextMenu("Build Zone/Upgrade (+1 level)")]
        void UpgradeFromContextMenu()
        {
            if (Upgrade())
                Debug.Log("[DM Build Hub] " + name + " upgraded to level " + upgradeLevel + " (" + ZoneSize + " m).");
        }

        [ContextMenu("Build Zone/Downgrade (-1 level)")]
        void DowngradeFromContextMenu()
        {
            SetLevel(upgradeLevel - 1);
        }

        [ContextMenu("Build Zone/Reset to level 0")]
        void ResetLevelFromContextMenu()
        {
            SetLevel(0);
        }

        // ---- Clear zone (0926-zone-clear): nothing but terrain may stand inside a hub's zone ----

        static readonly Collider[] blockerHits = new Collider[1024];

        /// <summary>"Build zone blocked by &lt;name&gt;".</summary>
        public static string BlockedByText(Collider blocker)
        {
            return "Build zone blocked by " + (blocker != null ? blocker.gameObject.name : "something");
        }

        /// <summary>
        /// The collider nearest the centre that blocks a zone box, or null when it is clear. Terrain, characters and
        /// creatures (Rigidbody, CharacterController, NavMeshAgent, Player/Enemy/CompanionAI/Animal/BodyPart/HeadTrack
        /// layers), dropped items, built pieces (and the placement preview) and Ignore Raycast / UI / PostProcess
        /// objects never block. Triggers do (patrol path spheres, world trigger volumes).
        /// </summary>
        public static Collider FindZoneBlocker(Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            int mask = ZoneBlockerMask();
            Collider[] hits = blockerHits;
            int count = Physics.OverlapBoxNonAlloc(center, half, blockerHits, Quaternion.identity, mask, QueryTriggerInteraction.Collide);
            if (count >= blockerHits.Length)
            {
                hits = Physics.OverlapBox(center, half, Quaternion.identity, mask, QueryTriggerInteraction.Collide);
                count = hits.Length;
            }

            Collider best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i];
                if (!IsZoneBlocker(collider))
                    continue;
                float distance = (collider.bounds.center - center).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = collider;
            }

            return best;
        }

        static bool IsZoneBlocker(Collider collider)
        {
            if (collider == null || !collider.enabled || collider is TerrainCollider || collider is CharacterController)
                return false;
            if (collider.attachedRigidbody != null)
                return false;
            if (collider.GetComponentInParent<DMBuildingGhost>() != null
                || collider.GetComponentInParent<DMBuildingPlacementController>() != null
                || collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null)
                return false;
            Transform root = collider.transform.root;
            return root == null || !root.name.StartsWith("BuildingGhost_", StringComparison.Ordinal);
        }

        static int ZoneBlockerMask()
        {
            int mask = ~0;
            mask &= ~LayerBit("Ignore Raycast");
            mask &= ~LayerBit("UI");
            mask &= ~LayerBit("Item");
            mask &= ~LayerBit("Player");
            mask &= ~LayerBit("Enemy");
            mask &= ~LayerBit("CompanionAI");
            mask &= ~LayerBit("HeadTrack");
            mask &= ~LayerBit("BodyPart");
            mask &= ~LayerBit("Animal");
            mask &= ~LayerBit("PostProcess");
            mask &= ~LayerBit(DMBuildingGhostProfile.BuildingLayerName);
            return mask;
        }

        static int LayerBit(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? 1 << layer : 0;
        }

        // ---- Lifecycle ----

        void Awake()
        {
            ghost = GetComponent<DMBuildingGhost>();
            EnsureAnchor(); // added at build time, after the piece is posed
        }

        void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
            DMBuildingMode.Changed += RefreshVisibility;
            RefreshVisibility();
            Changed?.Invoke();
        }

        void OnDisable()
        {
            active.Remove(this);
            DMBuildingMode.Changed -= RefreshVisibility;
            if (zoneVisual != null)
                zoneVisual.SetActive(false);
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            if (zoneVisual == null)
                return;
            if (Application.isPlaying)
                Destroy(zoneVisual);
            else
                DestroyImmediate(zoneVisual);
            zoneVisual = null;
        }

        void OnValidate()
        {
            upgradeLevel = Mathf.Max(0, upgradeLevel);
        }

        void LateUpdate()
        {
            // Build mode normally flips through DMBuildingMode.Changed; this also catches a missed event.
            bool show = DMBuildingMode.IsActive;
            bool shown = zoneVisual != null && zoneVisual.activeSelf;
            if (show != shown)
                RefreshVisibility();
            else if (shown)
                SyncVisual(); // live Studio size edits and the creation bounce settling
        }

        // ---- Zone visual (separate root object: never repainted, tinted or measured as part of the piece) ----

        void RefreshVisibility()
        {
            bool show = DMBuildingMode.IsActive && isActiveAndEnabled;
            if (!show)
            {
                if (zoneVisual != null && zoneVisual.activeSelf)
                    zoneVisual.SetActive(false);
                return;
            }

            EnsureVisual();
            if (zoneVisual == null)
                return;
            SyncVisual();
            if (!zoneVisual.activeSelf)
                zoneVisual.SetActive(true);
        }

        void SyncVisual()
        {
            Transform t = zoneVisual.transform;
            t.SetPositionAndRotation(Center, Quaternion.identity);
            t.localScale = ZoneSize;
        }

        void EnsureVisual()
        {
            if (zoneVisual != null)
                return;
            Material material = ResolveZoneMaterial();
            if (material == null)
            {
                if (!warnedMissingMaterial)
                {
                    warnedMissingMaterial = true;
                    Debug.LogWarning("[DM Build Hub] No zone material: Resources/" + ZoneMaterialResource
                        + " is missing and shader " + ZoneShaderName + " was not found. Run Tools > Dark Matter Genesis > Buildings > Add Build Hub (Equipment).");
                }
                return;
            }

            zoneVisual = new GameObject("BuildZone_" + name);
            zoneVisual.layer = 2; // Ignore Raycast; the visual has no collider anyway
            zoneVisual.SetActive(false);
            MeshFilter filter = zoneVisual.AddComponent<MeshFilter>();
            filter.sharedMesh = ZoneMesh();
            MeshRenderer body = zoneVisual.AddComponent<MeshRenderer>();
            body.sharedMaterial = material;
            body.shadowCastingMode = ShadowCastingMode.Off;
            body.receiveShadows = false;
            body.lightProbeUsage = LightProbeUsage.Off;
            body.reflectionProbeUsage = ReflectionProbeUsage.Off;
            body.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            body.allowOcclusionWhenDynamic = false;
        }

        static Material ResolveZoneMaterial()
        {
            if (zoneMaterial != null)
                return zoneMaterial;
            zoneMaterial = Resources.Load<Material>(ZoneMaterialResource);
            if (zoneMaterial != null)
                return zoneMaterial;

            Shader shader = Shader.Find(ZoneShaderName);
            if (shader == null)
                return null;
            zoneMaterial = new Material(shader) { name = "DM_BuildZone (runtime)" };
            zoneMaterial.renderQueue = (int)RenderQueue.Transparent;
            return zoneMaterial;
        }

        /// <summary>Unit cube (-0.5..0.5) with one normal per face; the shader draws grid lines from world position.</summary>
        static Mesh ZoneMesh()
        {
            if (zoneMesh != null)
                return zoneMesh;

            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var vertices = new Vector3[24];
            var normals = new Vector3[24];
            var uvs = new Vector2[24];
            var triangles = new int[36];
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = faceNormals[f];
                Vector3 a = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 b = Vector3.Cross(n, a);
                Vector3 c = n * 0.5f;
                int v = f * 4;
                vertices[v] = c - a * 0.5f - b * 0.5f;
                vertices[v + 1] = c + a * 0.5f - b * 0.5f;
                vertices[v + 2] = c + a * 0.5f + b * 0.5f;
                vertices[v + 3] = c - a * 0.5f + b * 0.5f;
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(1f, 1f);
                uvs[v + 3] = new Vector2(0f, 1f);
                for (int k = 0; k < 4; k++)
                    normals[v + k] = n;
                int tri = f * 6;
                triangles[tri] = v;
                triangles[tri + 1] = v + 2;
                triangles[tri + 2] = v + 1;
                triangles[tri + 3] = v;
                triangles[tri + 4] = v + 3;
                triangles[tri + 5] = v + 2;
            }

            zoneMesh = new Mesh { name = "DM_BuildZoneBox" };
            zoneMesh.vertices = vertices;
            zoneMesh.normals = normals;
            zoneMesh.uv = uvs;
            zoneMesh.triangles = triangles;
            zoneMesh.RecalculateBounds();
            return zoneMesh;
        }
    }
}
