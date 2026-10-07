using System;
using System.Collections.Generic;
using Project.AI;
using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Mesh-accurate ranged hit registration (Hit Marks plan B1). Builds one trigger hitbox per ragdoll bone
    /// collider on layer <c>DMHitbox</c> (26, collides with nothing), parented under the bone so it follows the
    /// animated body, including stagger and knockdown. While the rig is active, ranged queries ignore the
    /// enemy's root capsule, ragdoll bone colliders and weapon colliders (see <see cref="DMEnemyHitQuery"/>);
    /// the capsule stays for movement, blocking and melee. Disabled on death, re-enabled on respawn.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Combat/DM Enemy Hitbox Rig")]
    public sealed class DMEnemyHitboxRig : MonoBehaviour
    {
        public const string HitboxObjectPrefix = "DMHitbox_";

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

        public void Build()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();

            if (!built)
            {
                built = true;
                BuildHitboxes();
                Subscribe();
            }

            SetActive(isActiveAndEnabled && (health == null || !health.IsDead));
        }

        private void BuildHitboxes()
        {
            DM_EnemyHitMarkProfile profile = DM_EnemyHitMarkProfile.LiveOrDefault;
            float inflate = Mathf.Clamp(profile.hitboxInflate, 1f, 1.5f);
            int layer = DMEnemyHitQuery.HitboxLayer;

            Animator animator = GetComponentInChildren<Animator>(true);
            Dictionary<Transform, HumanBodyBones> humanMap = BuildHumanMap(animator);

            List<DMEnemyHitbox> list = new List<DMEnemyHitbox>(16);
            HashSet<Transform> covered = new HashSet<Transform>();
            Rigidbody rootBody = GetComponent<Rigidbody>();
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody || body.transform == transform)
                    continue;

                Collider source = body.GetComponent<Collider>();
                if (source == null || source is TerrainCollider || source is WheelCollider)
                    continue;
                if (source is MeshCollider meshSource && (!meshSource.convex || meshSource.sharedMesh == null))
                    continue;
                if (EnemyInvectorHitSetup.IsOutgoingWeaponCollider(source))
                    continue;
                if (EnemyInvectorPhysicsCache.IsImplausiblyOversized(source, out _))
                    continue;

                Transform bone = body.transform;
                CombatBodyPart zone = ResolveZone(bone, humanMap);
                DMEnemyHitbox hitbox = CreateFromSource(bone, source, zone, inflate, layer, profile);
                if (hitbox != null)
                {
                    list.Add(hitbox);
                    covered.Add(bone);
                }
            }

            if (list.Count > 0 && profile.addFillerHitboxes && animator != null && animator.isHuman)
                AddNeckFiller(animator, covered, list, layer, profile);

            hitboxes = list.ToArray();
        }

        private DMEnemyHitbox CreateFromSource(
            Transform bone,
            Collider source,
            CombatBodyPart zone,
            float inflate,
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

            Collider shape;
            switch (source)
            {
                case CapsuleCollider capsule:
                {
                    CapsuleCollider c = go.AddComponent<CapsuleCollider>();
                    c.center = capsule.center;
                    c.direction = capsule.direction;
                    c.radius = capsule.radius * inflate;
                    c.height = capsule.height * inflate;
                    shape = c;
                    break;
                }
                case SphereCollider sphere:
                {
                    SphereCollider c = go.AddComponent<SphereCollider>();
                    c.center = sphere.center;
                    c.radius = sphere.radius * inflate;
                    shape = c;
                    break;
                }
                case BoxCollider box:
                {
                    BoxCollider c = go.AddComponent<BoxCollider>();
                    c.center = box.center;
                    c.size = box.size * inflate;
                    shape = c;
                    break;
                }
                case MeshCollider mesh:
                {
                    MeshCollider c = go.AddComponent<MeshCollider>();
                    c.sharedMesh = mesh.sharedMesh;
                    c.convex = true;
                    shape = c;
                    break;
                }
                default:
                    Destroy(go);
                    return null;
            }

            shape.isTrigger = true;
            DMEnemyHitbox hitbox = go.AddComponent<DMEnemyHitbox>();
            hitbox.Configure(this, bone, zone, shape, profile.ResolveZoneMultiplier(zone));
            return hitbox;
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

            float scale = Mathf.Max(0.0001f, Mathf.Abs(neck.lossyScale.x));
            float worldRadius = Mathf.Clamp(length * 0.6f, 0.05f, 0.09f) * Mathf.Max(0.5f, animator.humanScale);

            GameObject go = new GameObject(HitboxObjectPrefix + neck.name + "_Neck");
            go.layer = layer;
            Transform t = go.transform;
            t.SetParent(neck, false);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            t.position = (top + bottom) * 0.5f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, (top - bottom).normalized);

            CapsuleCollider c = go.AddComponent<CapsuleCollider>();
            c.direction = 1;
            c.center = Vector3.zero;
            c.radius = worldRadius / scale;
            c.height = (length + worldRadius * 2f) / scale;
            c.isTrigger = true;

            DMEnemyHitbox hitbox = go.AddComponent<DMEnemyHitbox>();
            hitbox.Configure(this, neck, CombatBodyPart.Torso, c, profile.ResolveZoneMultiplier(CombatBodyPart.Torso));
            list.Add(hitbox);
        }

        private static Dictionary<Transform, HumanBodyBones> BuildHumanMap(Animator animator)
        {
            Dictionary<Transform, HumanBodyBones> map = new Dictionary<Transform, HumanBodyBones>(24);
            if (animator == null || !animator.isHuman)
                return map;

            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                Transform bone = animator.GetBoneTransform((HumanBodyBones)i);
                if (bone != null && !map.ContainsKey(bone))
                    map.Add(bone, (HumanBodyBones)i);
            }

            return map;
        }

        private static CombatBodyPart ResolveZone(Transform bone, Dictionary<Transform, HumanBodyBones> map)
        {
            if (bone != null && map.TryGetValue(bone, out HumanBodyBones human))
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

            string n = bone != null ? bone.name.ToLowerInvariant() : string.Empty;
            if (n.Contains("head"))
                return CombatBodyPart.Head;
            if (n.Contains("leg") || n.Contains("foot") || n.Contains("thigh") || n.Contains("calf") || n.Contains("knee"))
                return CombatBodyPart.Leg;
            if (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder") || n.Contains("elbow"))
                return CombatBodyPart.Arm;
            return CombatBodyPart.Torso;
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
            UpdateCount();
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
