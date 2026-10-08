using System;
using System.Collections.Generic;
using Project.AI;
using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Per-bone template hit volumes on layer <c>DMHitbox</c> (26) for ranged + melee (proxy on same child).
    /// Ragdoll bone colliders stay off while alive. Root capsule is locomotion-only and shrinks when the rig is active.
    /// Disabled on death, re-enabled on respawn.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Combat/DM Enemy Hitbox Rig")]
    public sealed class DMEnemyHitboxRig : MonoBehaviour
    {
        public const string HitboxObjectPrefix = "DMHitbox_";

        /// <summary>Humanoid bones that receive per-zone ranged hitboxes (player-like limbs / head / torso).</summary>
        public static readonly HumanBodyBones[] StandardHumanBones =
        {
            HumanBodyBones.Head,
            HumanBodyBones.Neck,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine,
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot
        };

        /// <summary>Tighter than ragdoll audit limits — hit volumes should match limb size, not root capsule scale.</summary>
        public const float MaxHitVolumeRadius = 0.22f;
        public const float MaxHitVolumeLength = 0.55f;

        private static int activeRigCount;

        /// <summary>Number of rigs currently active. Zero = ranged queries take the legacy fast path.</summary>
        public static int ActiveRigCount => activeRigCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeRigCount = 0;
        }

        private DMEnemyHitbox[] hitboxes = Array.Empty<DMEnemyHitbox>();
        private bool built;
        private bool active;
        private bool counted;
        private EnemyHealth health;
        private bool rootCapsuleSnapshotStored;
        private float rootCapsuleRadius;
        private float rootCapsuleHeight;
        private Vector3 rootCapsuleCenter;
        private bool rootCapsuleIsTrigger;

        public bool IsActive => active && hitboxes.Length > 0;
        public int HitboxCount => hitboxes.Length;
        public IReadOnlyList<DMEnemyHitbox> Hitboxes => hitboxes;

        /// <summary>Adds (if missing) and builds the rig on an enemy root. Safe to call repeatedly.</summary>
        public static DMEnemyHitboxRig Ensure(GameObject root)
        {
            if (root == null)
                return null;

            DMEnemyHitboxRig rig = root.GetComponent<DMEnemyHitboxRig>();
            if (rig == null)
                rig = root.AddComponent<DMEnemyHitboxRig>();

            rig.Build();
            return rig;
        }

        public void Build(bool forceRebuild = false)
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();

            if (!built)
            {
                built = true;
                Subscribe();
            }

            if (!forceRebuild && hitboxes.Length > 0)
            {
                WireMeleeReceivers();
                SetActive(isActiveAndEnabled && (health == null || !health.IsDead));
                return;
            }

            RebuildHitboxes();
            SetActive(isActiveAndEnabled && (health == null || !health.IsDead));
        }

        /// <summary>After ragdoll get-up: re-wire melee proxies without destroying per-bone hit volumes.</summary>
        public void RefreshAfterRagdoll()
        {
            WireMeleeReceivers();
            SetActive(isActiveAndEnabled && (health == null || !health.IsDead));
        }

        private void RebuildHitboxes()
        {
            ClearExistingHitboxes();
            BuildHitboxes();
        }

        private void ClearExistingHitboxes()
        {
            List<GameObject> toDestroy = new List<GameObject>(32);
            CollectHitboxObjects(transform, toDestroy);
            for (int i = 0; i < toDestroy.Count; i++)
            {
                GameObject go = toDestroy[i];
                if (go != null)
                    Destroy(go);
            }

            hitboxes = Array.Empty<DMEnemyHitbox>();
        }

        private static void CollectHitboxObjects(Transform root, List<GameObject> toDestroy)
        {
            if (root == null)
                return;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                if (child.name.StartsWith(HitboxObjectPrefix, StringComparison.Ordinal))
                    toDestroy.Add(child.gameObject);
                else
                    CollectHitboxObjects(child, toDestroy);
            }
        }

        private void BuildHitboxes()
        {
            DM_EnemyHitMarkProfile profile = DM_EnemyHitMarkProfile.LiveOrDefault;
            int layer = DMEnemyHitQuery.HitboxLayer;

            Animator animator = GetComponentInChildren<Animator>(true);
            List<DMEnemyHitbox> list = new List<DMEnemyHitbox>(StandardHumanBones.Length + 2);
            HashSet<Transform> covered = new HashSet<Transform>();

            if (animator != null && animator.isHuman)
            {
                for (int i = 0; i < StandardHumanBones.Length; i++)
                {
                    HumanBodyBones human = StandardHumanBones[i];
                    Transform bone = animator.GetBoneTransform(human);
                    if (bone == null)
                        continue;

                    CombatBodyPart zone = MapHumanBoneToZone(human);
                    DMEnemyHitbox hitbox = CreateTemplateHitbox(bone, human, zone, layer, profile);
                    if (hitbox == null)
                        continue;

                    list.Add(hitbox);
                    covered.Add(bone);
                }
            }

            if (list.Count > 0 && profile.addFillerHitboxes && animator != null && animator.isHuman)
                AddNeckFiller(animator, covered, list, layer, profile);

            hitboxes = list.ToArray();
            WireMeleeReceivers();
        }

        /// <summary>One <see cref="DMEnemyHitbox"/> child per bone handles ranged (layer 26) and melee (tag Enemy + proxy).</summary>
        public void WireMeleeReceivers()
        {
            PioneerInvectorDamageReceiver rootReceiver = GetComponent<PioneerInvectorDamageReceiver>();
            if (rootReceiver == null)
                return;

            for (int i = 0; i < hitboxes.Length; i++)
            {
                DMEnemyHitbox hitbox = hitboxes[i];
                if (hitbox == null)
                    continue;

                GameObject go = hitbox.gameObject;
                if (go.CompareTag("Untagged"))
                    go.tag = "Enemy";

                PioneerRagdollBoneDamageProxy proxy = go.GetComponent<PioneerRagdollBoneDamageProxy>();
                if (proxy == null)
                    proxy = go.AddComponent<PioneerRagdollBoneDamageProxy>();

                proxy.Configure(rootReceiver);
            }
        }

        /// <summary>True when a bone collider is small enough to copy or mirror for combat hit volumes.</summary>
        public static bool IsPlausibleHitVolume(Collider collider)
        {
            if (collider == null || collider is TerrainCollider || collider is WheelCollider)
                return false;
            if (collider is MeshCollider mesh && (!mesh.convex || mesh.sharedMesh == null))
                return false;
            if (EnemyInvectorHitSetup.IsOutgoingWeaponCollider(collider))
                return false;

            return !IsOversizedHitVolume(collider, out _);
        }

        public static bool IsOversizedHitVolume(Collider collider, out string sizeDescription)
        {
            Vector3 lossyScale = collider.transform.lossyScale;
            switch (collider)
            {
                case CapsuleCollider capsule:
                {
                    int heightAxis = capsule.direction;
                    int radiusAxisA = heightAxis == 0 ? 1 : 0;
                    int radiusAxisB = heightAxis == 2 ? 1 : 2;
                    float heightScale = AxisScale(lossyScale, heightAxis);
                    float radiusScale = Mathf.Max(AxisScale(lossyScale, radiusAxisA), AxisScale(lossyScale, radiusAxisB));
                    float effectiveRadius = capsule.radius * radiusScale;
                    float effectiveHeight = capsule.height * heightScale;
                    sizeDescription =
                        $"radius={capsule.radius:0.###} (world {effectiveRadius:0.###}), " +
                        $"height={capsule.height:0.###} (world {effectiveHeight:0.###})";
                    return effectiveRadius > MaxHitVolumeRadius || effectiveHeight > MaxHitVolumeLength;
                }
                case BoxCollider box:
                {
                    Vector3 effectiveSize = Vector3.Scale(box.size, lossyScale);
                    sizeDescription = $"size={box.size} (world {effectiveSize})";
                    return effectiveSize.x > MaxHitVolumeLength ||
                           effectiveSize.y > MaxHitVolumeLength ||
                           effectiveSize.z > MaxHitVolumeLength;
                }
                case SphereCollider sphere:
                {
                    float scale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
                    float effectiveRadius = sphere.radius * scale;
                    sizeDescription = $"radius={sphere.radius:0.###} (world {effectiveRadius:0.###})";
                    return effectiveRadius > MaxHitVolumeRadius;
                }
                default:
                    sizeDescription = string.Empty;
                    return true;
            }
        }

        private static float AxisScale(Vector3 lossyScale, int axis)
        {
            switch (axis)
            {
                case 0: return Mathf.Abs(lossyScale.x);
                case 2: return Mathf.Abs(lossyScale.z);
                default: return Mathf.Abs(lossyScale.y);
            }
        }

        private DMEnemyHitbox CreateTemplateHitbox(
            Transform bone,
            HumanBodyBones human,
            CombatBodyPart zone,
            int layer,
            DM_EnemyHitMarkProfile profile)
        {
            GameObject go = new GameObject(HitboxObjectPrefix + bone.name);
            go.layer = layer;
            Transform t = go.transform;
            t.SetParent(bone, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
            float templateScale = Mathf.Max(0.25f, profile.hitboxTemplateScale);
            ApplyTemplateCapsule(capsule, human, bone, templateScale);
            capsule.isTrigger = true;

            DMEnemyHitbox hitbox = go.AddComponent<DMEnemyHitbox>();
            hitbox.Configure(this, bone, zone, capsule, profile.ResolveZoneMultiplier(zone));
            return hitbox;
        }

        /// <summary>Fits a capsule on a bone child from ~1m humanoid world targets (axis-correct lossyScale).</summary>
        public static void ApplyTemplateCapsule(
            CapsuleCollider capsule,
            HumanBodyBones human,
            Transform bone = null,
            float templateScale = 1f)
        {
            if (capsule == null)
                return;

            ResolveHumanTemplateWorld(human, templateScale, out float worldRadius, out float worldHeight, out float centerY);
            ApplyWorldCapsuleOnBone(capsule, bone != null ? bone : capsule.transform, 1, worldRadius, worldHeight, new Vector3(0f, centerY, 0f));
        }

        /// <summary>Player-like limb sizes in world metres (~1m humanoid).</summary>
        public static void ResolveHumanTemplateWorld(
            HumanBodyBones human,
            float templateScale,
            out float worldRadius,
            out float worldHeight,
            out float centerY)
        {
            float s = Mathf.Max(0.25f, templateScale);
            centerY = 0f;
            switch (human)
            {
                case HumanBodyBones.Head:
                    centerY = 0.06f * s;
                    worldRadius = 0.11f * s;
                    worldHeight = 0.22f * s;
                    break;
                case HumanBodyBones.Neck:
                    worldRadius = 0.06f * s;
                    worldHeight = 0.14f * s;
                    break;
                case HumanBodyBones.UpperChest:
                case HumanBodyBones.Chest:
                    worldRadius = 0.14f * s;
                    worldHeight = 0.28f * s;
                    break;
                case HumanBodyBones.Spine:
                case HumanBodyBones.Hips:
                    worldRadius = 0.13f * s;
                    worldHeight = 0.22f * s;
                    break;
                case HumanBodyBones.LeftUpperArm:
                case HumanBodyBones.RightUpperArm:
                    centerY = -0.12f * s;
                    worldRadius = 0.07f * s;
                    worldHeight = 0.26f * s;
                    break;
                case HumanBodyBones.LeftLowerArm:
                case HumanBodyBones.RightLowerArm:
                    centerY = -0.1f * s;
                    worldRadius = 0.06f * s;
                    worldHeight = 0.22f * s;
                    break;
                case HumanBodyBones.LeftHand:
                case HumanBodyBones.RightHand:
                    worldRadius = 0.05f * s;
                    worldHeight = 0.12f * s;
                    break;
                case HumanBodyBones.LeftUpperLeg:
                case HumanBodyBones.RightUpperLeg:
                    centerY = -0.14f * s;
                    worldRadius = 0.09f * s;
                    worldHeight = 0.32f * s;
                    break;
                case HumanBodyBones.LeftLowerLeg:
                case HumanBodyBones.RightLowerLeg:
                    centerY = -0.12f * s;
                    worldRadius = 0.07f * s;
                    worldHeight = 0.28f * s;
                    break;
                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightFoot:
                    centerY = -0.03f * s;
                    worldRadius = 0.06f * s;
                    worldHeight = 0.16f * s;
                    break;
                default:
                    worldRadius = 0.08f * s;
                    worldHeight = 0.2f * s;
                    break;
            }
        }

        /// <summary>Writes local capsule fields so the collider's world size matches the requested world template.</summary>
        public static void ApplyWorldCapsuleOnBone(
            CapsuleCollider capsule,
            Transform bone,
            int direction,
            float worldRadius,
            float worldHeight,
            Vector3 worldCenterInBoneLocalSpace)
        {
            if (capsule == null)
                return;

            Vector3 lossy = bone != null ? bone.lossyScale : Vector3.one;
            float heightScale = AxisScale(lossy, direction);
            int radiusAxisA = direction == 0 ? 1 : 0;
            int radiusAxisB = direction == 2 ? 1 : 2;
            float radiusScale = Mathf.Max(AxisScale(lossy, radiusAxisA), AxisScale(lossy, radiusAxisB));

            capsule.direction = direction;
            capsule.radius = worldRadius / Mathf.Max(radiusScale, 0.001f);
            capsule.height = worldHeight / Mathf.Max(heightScale, 0.001f);
            capsule.center = new Vector3(
                worldCenterInBoneLocalSpace.x / Mathf.Max(Mathf.Abs(lossy.x), 0.001f),
                worldCenterInBoneLocalSpace.y / Mathf.Max(heightScale, 0.001f),
                worldCenterInBoneLocalSpace.z / Mathf.Max(Mathf.Abs(lossy.z), 0.001f));

            capsule.height = Mathf.Max(capsule.height, capsule.radius * 2f);
            ClampCapsuleWorldSize(capsule, bone);
        }

        private static void ClampCapsuleWorldSize(CapsuleCollider capsule, Transform bone)
        {
            if (capsule == null)
                return;

            if (!IsOversizedHitVolume(capsule, out _))
                return;

            Vector3 lossy = bone != null ? bone.lossyScale : Vector3.one;
                int direction = capsule.direction;
                float heightScale = AxisScale(lossy, direction);
                int radiusAxisA = direction == 0 ? 1 : 0;
                int radiusAxisB = direction == 2 ? 1 : 2;
                float radiusScale = Mathf.Max(AxisScale(lossy, radiusAxisA), AxisScale(lossy, radiusAxisB));
                capsule.radius = MaxHitVolumeRadius / Mathf.Max(radiusScale, 0.001f);
            capsule.height = MaxHitVolumeLength / Mathf.Max(heightScale, 0.001f);
            capsule.height = Mathf.Max(capsule.height, capsule.radius * 2f);
        }

        /// <summary>Ragdolls usually skip the neck: bridge chest top to the head with a small capsule.</summary>
        private void AddNeckFiller(
            Animator animator,
            HashSet<Transform> covered,
            List<DMEnemyHitbox> list,
            int layer,
            DM_EnemyHitMarkProfile profile)
        {
            Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (neck == null || head == null || covered.Contains(neck))
                return;

            Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chest == null)
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);

            Vector3 top = head.position;
            Vector3 bottom = chest != null ? Vector3.Lerp(neck.position, chest.position, 0.5f) : neck.position;
            float length = Vector3.Distance(top, bottom);
            if (length < 0.02f || length > 0.6f)
                return;

            float templateScale = Mathf.Max(0.25f, profile.hitboxTemplateScale);
            float worldRadius = Mathf.Clamp(length * 0.6f, 0.05f, 0.09f) * Mathf.Max(0.5f, animator.humanScale) * templateScale;

            GameObject go = new GameObject(HitboxObjectPrefix + neck.name + "_Neck");
            go.layer = layer;
            Transform t = go.transform;
            t.SetParent(neck, false);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            t.position = (top + bottom) * 0.5f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, (top - bottom).normalized);

            CapsuleCollider c = go.AddComponent<CapsuleCollider>();
            ApplyWorldCapsuleOnBone(c, neck, 1, worldRadius, length + worldRadius * 2f, Vector3.zero);
            c.isTrigger = true;

            DMEnemyHitbox hitbox = go.AddComponent<DMEnemyHitbox>();
            hitbox.Configure(this, neck, CombatBodyPart.Torso, c, profile.ResolveZoneMultiplier(CombatBodyPart.Torso));
            list.Add(hitbox);
        }

        public static CombatBodyPart MapHumanBoneToZone(HumanBodyBones human)
        {
            switch (human)
            {
                case HumanBodyBones.Head:
                case HumanBodyBones.Jaw:
                case HumanBodyBones.LeftEye:
                case HumanBodyBones.RightEye:
                    return CombatBodyPart.Head;
                case HumanBodyBones.Hips:
                case HumanBodyBones.Spine:
                case HumanBodyBones.Chest:
                case HumanBodyBones.UpperChest:
                case HumanBodyBones.Neck:
                    return CombatBodyPart.Torso;
                case HumanBodyBones.LeftUpperLeg:
                case HumanBodyBones.RightUpperLeg:
                case HumanBodyBones.LeftLowerLeg:
                case HumanBodyBones.RightLowerLeg:
                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightFoot:
                case HumanBodyBones.LeftToes:
                case HumanBodyBones.RightToes:
                    return CombatBodyPart.Leg;
                default:
                    return CombatBodyPart.Arm;
            }
        }

        private void Subscribe()
        {
            if (health == null)
                return;

            health.Died -= HandleDied;
            health.Died += HandleDied;
            health.Respawned -= HandleRespawned;
            health.Respawned += HandleRespawned;
        }

        private void HandleDied()
        {
            // Corpse shots fall through to the ragdoll bone colliders (no damage, no blood).
            SetActive(false);
        }

        private void HandleRespawned()
        {
            SetActive(isActiveAndEnabled);
        }

        public void SetActive(bool value)
        {
            int layer = DMEnemyHitQuery.HitboxLayer;
            for (int i = 0; i < hitboxes.Length; i++)
            {
                DMEnemyHitbox hitbox = hitboxes[i];
                if (hitbox == null)
                    continue;

                GameObject go = hitbox.gameObject;
                if (go.layer != layer)
                    go.layer = layer;

                Collider shape = hitbox.Shape;
                if (shape != null)
                {
                    shape.isTrigger = true;
                    shape.enabled = value;
                }
            }

            active = value && hitboxes.Length > 0;
            SyncRootFallbackCapsule(active);
            UpdateCount();
        }

        /// <summary>
        /// While per-bone hitboxes are live, shrink the root capsule to a small trigger so melee/ranged
        /// do not prefer the renderer-sized fallback over DMHitbox / bone receivers.
        /// </summary>
        private void SyncRootFallbackCapsule(bool fineHitboxesLive)
        {
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            if (cap == null)
                return;

            if (!rootCapsuleSnapshotStored)
            {
                rootCapsuleSnapshotStored = true;
                rootCapsuleRadius = cap.radius;
                rootCapsuleHeight = cap.height;
                rootCapsuleCenter = cap.center;
                rootCapsuleIsTrigger = cap.isTrigger;
            }

            if (fineHitboxesLive)
            {
                cap.isTrigger = true;
                cap.radius = 0.26f;
                cap.height = 0.38f;
                cap.center = new Vector3(0f, 0.32f, 0f);
                cap.direction = 1;
            }
            else
            {
                cap.radius = rootCapsuleRadius;
                cap.height = rootCapsuleHeight;
                cap.center = rootCapsuleCenter;
                cap.isTrigger = rootCapsuleIsTrigger;
            }
        }

        private void UpdateCount()
        {
            bool shouldCount = active && isActiveAndEnabled;
            if (shouldCount == counted)
                return;

            counted = shouldCount;
            activeRigCount = Mathf.Max(0, activeRigCount + (shouldCount ? 1 : -1));
        }

        private void OnEnable()
        {
            if (built)
                SetActive(health == null || !health.IsDead);
        }

        private void OnDisable()
        {
            active = false;
            UpdateCount();
        }

        private void OnDestroy()
        {
            active = false;
            UpdateCount();
            if (health != null)
            {
                health.Died -= HandleDied;
                health.Respawned -= HandleRespawned;
            }
        }
    }
}
