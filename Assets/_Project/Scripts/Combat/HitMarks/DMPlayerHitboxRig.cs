using System;
using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Survival;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Player version of <see cref="DMEnemyHitboxRig"/>: per-bone trigger hitboxes on layer <c>DMHitbox</c> built at
    /// runtime on whatever player is live in the scene (see <see cref="DMPlayerHitboxBootstrap"/>), so no player
    /// prefab is edited. Independent of EnemyHealth: it follows <see cref="SurvivalStats"/> death / revive instead.
    /// While active, the player's root capsule / ragdoll / weapon colliders no longer count as ranged hits
    /// (<see cref="DMEnemyHitQuery.IsSupersededByHitboxRig"/>), so enemy ranged ammo resolves against a body bone and
    /// <see cref="DMEnemyHitMarks"/> (Humanoid body type, same profile + A16 rules) stamps marks on that bone.
    /// The locomotion capsule is not touched unless <see cref="MatchEnemyCapsule"/> is switched on (off by default).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Combat/DM Player Hitbox Rig")]
    public sealed class DMPlayerHitboxRig : MonoBehaviour
    {
        public const string HitboxObjectPrefix = "DMPlayerHitbox_";

        /// <summary>Kill switch for the runtime bootstrap (read each poll). Off = existing capsule behaviour.</summary>
        public static bool RuntimeBootstrapEnabled = true;

        /// <summary>
        /// Opt-in: resize the player's CapsuleCollider to <see cref="MatchCapsuleRadius"/> / <see cref="MatchCapsuleHeight"/>
        /// at build time (feet kept fixed). OFF by default: the project rules lock the Player_v7 capsule at 0.26 / 1.84
        /// and the per-bone rig already supersedes the capsule for ranged hits.
        /// </summary>
        public static bool MatchEnemyCapsule = false;
        public static float MatchCapsuleRadius = 0.15f;
        public static float MatchCapsuleHeight = 1.88f;

        private static int activeRigCount;

        /// <summary>Number of player rigs currently live (0 or 1 in practice).</summary>
        public static int ActiveRigCount => activeRigCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeRigCount = 0;
            RuntimeBootstrapEnabled = true;
            MatchEnemyCapsule = false;
        }

        [SerializeField] private DMEnemyBodyType bodyType = DMEnemyBodyType.Humanoid;

        private DMEnemyHitbox[] hitboxes = Array.Empty<DMEnemyHitbox>();
        private bool built;
        private bool active;
        private bool counted;
        private bool subscribed;
        private bool lastDead;
        private SurvivalStats survival;
        private DMEnemyHitMarks marks;

        public DMEnemyBodyType BodyType
        {
            get => bodyType;
            set
            {
                bodyType = value;
                if (marks != null)
                    marks.BodyType = value;
            }
        }

        public bool IsActive => active && hitboxes.Length > 0;
        public int HitboxCount => hitboxes.Length;
        public IReadOnlyList<DMEnemyHitbox> Hitboxes => hitboxes;
        public DMEnemyHitMarks Marks => marks;

        /// <summary>Adds (if missing) and builds the rig on the player root. Safe to call repeatedly.</summary>
        public static DMPlayerHitboxRig Ensure(GameObject root)
        {
            if (root == null)
                return null;

            DMPlayerHitboxRig rig = root.GetComponent<DMPlayerHitboxRig>();
            if (rig == null)
                rig = root.AddComponent<DMPlayerHitboxRig>();

            rig.Build();
            return rig;
        }

        public void Build(bool forceRebuild = false)
        {
            if (survival == null)
                survival = GetComponent<SurvivalStats>();

            Subscribe();

            if (!forceRebuild && hitboxes.Length > 0)
            {
                SyncActive();
                return;
            }

            ClearExistingHitboxes();
            BuildHitboxes();
            EnsureMarks();
            if (MatchEnemyCapsule)
                ApplyMatchedCapsule();
            built = true;
            SyncActive();
        }

        private void EnsureMarks()
        {
            marks = DMEnemyHitMarks.Ensure(gameObject);
            if (marks != null)
                marks.BodyType = bodyType;
        }

        private void ClearExistingHitboxes()
        {
            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null)
                    Destroy(hitboxes[i].gameObject);
            }

            hitboxes = Array.Empty<DMEnemyHitbox>();
        }

        private Animator ResolveHumanoidAnimator()
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a != null && a.isHuman && a.avatar != null)
                    return a;
            }

            return null;
        }

        private void BuildHitboxes()
        {
            DM_EnemyHitMarkProfile profile = DM_EnemyHitMarkProfile.LiveOrDefault;
            int layer = DMEnemyHitQuery.HitboxLayer;
            Animator animator = ResolveHumanoidAnimator();
            if (animator == null)
                return;

            float templateScale = Mathf.Max(0.25f, profile.hitboxTemplateScale);
            List<DMEnemyHitbox> list = new List<DMEnemyHitbox>(DMEnemyHitboxRig.StandardHumanBones.Length);
            for (int i = 0; i < DMEnemyHitboxRig.StandardHumanBones.Length; i++)
            {
                HumanBodyBones human = DMEnemyHitboxRig.StandardHumanBones[i];
                Transform bone = animator.GetBoneTransform(human);
                if (bone == null)
                    continue;

                CombatBodyPart zone = DMEnemyHitboxRig.MapHumanBoneToZone(human);
                GameObject go = new GameObject(HitboxObjectPrefix + bone.name);
                go.hideFlags = HideFlags.DontSaveInEditor;
                go.layer = layer;
                Transform t = go.transform;
                t.SetParent(bone, false);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;

                CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
                DMEnemyHitboxRig.ApplyTemplateCapsule(capsule, human, bone, templateScale);
                capsule.isTrigger = true;

                DMEnemyHitbox hitbox = go.AddComponent<DMEnemyHitbox>();
                hitbox.Configure(this, bone, zone, capsule, profile.ResolveZoneMultiplier(zone));
                list.Add(hitbox);
            }

            hitboxes = list.ToArray();
        }

        /// <summary>Opt-in capsule resize (feet fixed). Not called unless <see cref="MatchEnemyCapsule"/> is on.</summary>
        public void ApplyMatchedCapsule()
        {
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            if (cap == null || cap.direction != 1)
                return;

            float feetY = cap.center.y - cap.height * 0.5f;
            cap.radius = MatchCapsuleRadius;
            cap.height = Mathf.Max(MatchCapsuleHeight, MatchCapsuleRadius * 2f);
            Vector3 c = cap.center;
            c.y = feetY + cap.height * 0.5f;
            cap.center = c;
        }

        /// <summary>
        /// Called by <see cref="CombatHitResolver"/> after a ranged hit has been applied to the player.
        /// True when the collider is one of this player's body hitboxes (marks stamped on that bone).
        /// </summary>
        public static bool TryHandleRangedHit(
            Collider collider,
            Vector3 hitPoint,
            Vector3 surfaceNormal,
            Vector3 travelDirection,
            float damage,
            ItemData ammoItem,
            ItemData weapon)
        {
            if (!TryGetPlayerHitbox(collider, out DMEnemyHitbox hitbox))
                return false;

            DMPlayerHitboxRig rig = hitbox.PlayerRig;
            if (rig == null)
                return false;

            DMEnemyHitMarks.HandleRangedHitOnBody(
                rig.gameObject,
                rig.bodyType,
                collider,
                hitPoint,
                surfaceNormal,
                travelDirection,
                damage,
                ammoItem,
                weapon);
            if (rig.marks == null)
                rig.marks = rig.GetComponent<DMEnemyHitMarks>();
            return true;
        }

        /// <summary>True when <paramref name="collider"/> is a live per-bone hitbox of the player rig.</summary>
        public static bool TryGetPlayerHitbox(Collider collider, out DMEnemyHitbox hitbox)
        {
            hitbox = null;
            if (collider == null || activeRigCount <= 0)
                return false;

            if (!DMEnemyHitQuery.TryGetHitbox(collider, out hitbox))
                return false;

            return hitbox.PlayerRig != null;
        }

        /// <summary>True when the receiver object is a player body hitbox (world impact FX is skipped for those).</summary>
        public static bool IsPlayerBodyHitbox(GameObject receiver)
        {
            if (receiver == null || activeRigCount <= 0)
                return false;

            return receiver.TryGetComponent(out DMEnemyHitbox hitbox) && hitbox.PlayerRig != null;
        }

        private void Subscribe()
        {
            if (subscribed || survival == null)
                return;

            subscribed = true;
            survival.PlayerDied += HandleDied;
            survival.PlayerRevived += HandleRevived;
        }

        private void Unsubscribe()
        {
            if (!subscribed || survival == null)
            {
                subscribed = false;
                return;
            }

            survival.PlayerDied -= HandleDied;
            survival.PlayerRevived -= HandleRevived;
            subscribed = false;
        }

        private void HandleDied()
        {
            // Corpse / ragdoll shots fall through to the normal colliders; marks clear like enemies on Died.
            lastDead = true;
            SetActive(false);
            if (marks != null)
                marks.ClearAll();
        }

        private void HandleRevived()
        {
            lastDead = false;
            if (marks != null)
                marks.ClearAll();
            SyncActive();
        }

        private void Update()
        {
            // Some respawn paths clear IsDead without raising PlayerRevived; keep the rig in step.
            if (survival == null || !built)
                return;

            bool dead = survival.IsDead;
            if (dead == lastDead)
                return;

            lastDead = dead;
            if (dead)
                HandleDied();
            else
                HandleRevived();
        }

        private void SyncActive()
        {
            lastDead = survival != null && survival.IsDead;
            SetActive(isActiveAndEnabled && !lastDead);
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
                SyncActive();
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
            Unsubscribe();
        }
    }

    /// <summary>
    /// Builds <see cref="DMPlayerHitboxRig"/> on whatever player is live in the scene (Player_v7 Combat Variant, Variant,
    /// original template, or a respawned copy). Runtime only: prefabs and the scene file are never edited.
    /// </summary>
    public static class DMPlayerHitboxBootstrap
    {
        private const float PollSeconds = 0.5f;

        private static DMPlayerHitboxBootstrapRunner runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            runner = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (runner != null)
                return;

            GameObject go = new GameObject("DMPlayerHitboxBootstrap");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<DMPlayerHitboxBootstrapRunner>();
            runner.Poll();
        }

        internal static float Interval => PollSeconds;

        /// <summary>shakefix: runtime-only Rigidbody interpolation on the live player root (DM_CombatCoreProfile.playerRigidbodyInterpolation, old value logged once).</summary>
        private static void ApplyPlayerRigidbodyInterpolation(GameObject player)
        {
            DM_CombatCoreProfile cc = DM_CombatCoreProfile.Live;
            if (cc == null || cc.playerRigidbodyInterpolation == PlayerRigidbodyInterpolationMode.Keep)
                return;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null)
                return;

            RigidbodyInterpolation wanted = cc.playerRigidbodyInterpolation == PlayerRigidbodyInterpolationMode.None ? RigidbodyInterpolation.None
                : cc.playerRigidbodyInterpolation == PlayerRigidbodyInterpolationMode.Extrapolate ? RigidbodyInterpolation.Extrapolate
                : RigidbodyInterpolation.Interpolate;
            if (body.interpolation == wanted)
                return;

#if UNITY_EDITOR
            Debug.Log("[PlayerInterpolation] " + body.name + " Rigidbody.interpolation " + body.interpolation + " -> " + wanted);
#endif
            body.interpolation = wanted;
        }

        /// <summary>One-shot: build the rig on the live player now (used by Boot and the poll).</summary>
        public static DMPlayerHitboxRig EnsureOnLivePlayer()
        {
            if (!DMPlayerHitboxRig.RuntimeBootstrapEnabled)
                return null;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null || !player.activeInHierarchy)
                return null;
            if (player.GetComponent<SurvivalStats>() == null)
                return null;

            ApplyPlayerRigidbodyInterpolation(player);

            DMPlayerHitboxRig rig = player.GetComponent<DMPlayerHitboxRig>();
            if (rig != null && rig.HitboxCount > 0)
                return rig;

            return DMPlayerHitboxRig.Ensure(player);
        }
    }

    [AddComponentMenu("")]
    internal sealed class DMPlayerHitboxBootstrapRunner : MonoBehaviour
    {
        private float next;

        internal void Poll()
        {
            DMPlayerHitboxBootstrap.EnsureOnLivePlayer();
        }

        private void Update()
        {
            if (Time.unscaledTime < next)
                return;

            next = Time.unscaledTime + DMPlayerHitboxBootstrap.Interval;
            Poll();
        }
    }
}