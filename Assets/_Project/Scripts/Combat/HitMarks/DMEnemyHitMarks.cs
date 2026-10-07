using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Combat
{
    /// <summary>
    /// Per-enemy bullet hit marks (Hit Marks plan B2/B3): body-type splatter / sparks and up to 5 HDRP decal
    /// burn marks parented to the hit bone (oldest rotates out). Marks persist while the enemy lives; they are
    /// cleared on Died (before the death delay / disintegration), Respawned and OnDisable. If health ever goes
    /// back up (future regen), marks stamped at or below the new health fade out. Splatter / sparks / burn look
    /// per body type come from <see cref="DM_EnemyHitMarkProfile.GetResponse"/>; profile edits apply live.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Combat/DM Enemy Hit Marks")]
    public sealed class DMEnemyHitMarks : MonoBehaviour
    {
        public const string SlotObjectPrefix = "DM_HitMark_";
        private const int MaxSlots = DM_EnemyHitMarkProfile.MaxMarksPerEnemy;

        [Tooltip("Picks the body type response on DM_EnemyHitMarkProfile (Genesis Studio → Combat → Hit Marks): Humanoid = red blood + blood-spot char; Android = coolant + sparks + glowing burn; Robot = sparks + glowing burn. Ammo element rules (§16) still override.")]
        [SerializeField] private DMEnemyBodyType bodyType = DMEnemyBodyType.Humanoid;
        [Tooltip("Optional per-type profile (set from EnemyDefinition.hitMarkProfileOverride). Empty = Resources/Combat/DM_EnemyHitMarkProfile.")]
        [SerializeField] private DM_EnemyHitMarkProfile profileOverride;

        private struct Slot
        {
            public GameObject Root;
            public DecalProjector Base;
            public DecalProjector Glow;
            public bool Used;
            public float StampTime;
            public float HealthAtStamp;
            public bool Glowing;
            public float GlowStart;
            public bool FadingOut;
            public float FadeStart;
        }

        private static readonly Vector2[] AtlasBias =
        {
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)
        };

        private static readonly List<Transform> ReleaseBuffer = new List<Transform>(16);
        private static readonly HashSet<string> WarnedMissing = new HashSet<string>();

        private readonly Slot[] slots = new Slot[MaxSlots];
        private EnemyHealth health;
        private DMEnemyHitboxRig rig;
        private bool subscribed;
        private bool renderersTagged;
        private uint markLayerMask;
        private float lastBurstTime = -10f;
        private float lastHealth = -1f;
        private int animatingCount;
        private int appliedProfileRevision = -1;

        public DMEnemyBodyType BodyType
        {
            get => bodyType;
            set => bodyType = value;
        }

        public DM_EnemyHitMarkProfile ProfileOverride
        {
            get => profileOverride;
            set
            {
                profileOverride = value;
                appliedProfileRevision = -1;
            }
        }

        /// <summary>Per-type override when set, else the global Resources profile (or built-in defaults).</summary>
        public DM_EnemyHitMarkProfile Profile => profileOverride != null ? profileOverride : DM_EnemyHitMarkProfile.LiveOrDefault;

        public int ActiveMarkCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i].Used)
                        n++;
                return n;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            WarnedMissing.Clear();
            ReleaseBuffer.Clear();
        }

        /// <summary>Adds (if missing) the marks component on an enemy root. Default body type = Humanoid.</summary>
        public static DMEnemyHitMarks Ensure(GameObject root)
        {
            if (root == null)
                return null;

            DMEnemyHitMarks marks = root.GetComponent<DMEnemyHitMarks>();
            if (marks == null)
                marks = root.AddComponent<DMEnemyHitMarks>();
            marks.Initialize();
            return marks;
        }

        /// <summary>
        /// Single entry from <see cref="CombatHitResolver.ApplyDirectHit"/> for ranged hits on an EnemyHealth target
        /// (after damage was applied).
        /// </summary>
        public static void HandleRangedHit(
            EnemyHealth enemy,
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 surfaceNormal,
            Vector3 travelDirection,
            float damage,
            ItemData ammoItem,
            ItemData weapon)
        {
            if (enemy == null)
                return;

            DMEnemyHitMarks marks = enemy.GetComponent<DMEnemyHitMarks>();
            if (marks == null)
            {
                marks = enemy.gameObject.AddComponent<DMEnemyHitMarks>();
                marks.Initialize();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (WarnedMissing.Add(enemy.name))
                    Debug.LogWarning($"DMEnemyHitMarks: {enemy.name} had no hit-marks component; added at runtime as Humanoid. Add DMEnemyHitMarks with the right body type to the prefab.", enemy);
#endif
            }

            marks.OnRangedHit(hitCollider, hitPoint, surfaceNormal, travelDirection, damage, ammoItem, weapon);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();
            if (rig == null)
                rig = GetComponent<DMEnemyHitboxRig>();
            Subscribe();
            if (health != null && lastHealth < 0f)
                lastHealth = health.CurrentHealth;
        }

        private void Subscribe()
        {
            if (subscribed || health == null)
                return;

            subscribed = true;
            health.Died += HandleDied;
            health.Respawned += HandleRespawned;
            health.HealthChanged += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (!subscribed || health == null)
                return;

            health.Died -= HandleDied;
            health.Respawned -= HandleRespawned;
            health.HealthChanged -= HandleHealthChanged;
            subscribed = false;
        }

        private void OnDisable()
        {
            // No reparenting here: the hierarchy may be mid-deactivation.
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Base != null)
                    slots[i].Base.enabled = false;
                if (slots[i].Glow != null)
                    slots[i].Glow.enabled = false;
                slots[i].Used = false;
                slots[i].Glowing = false;
                slots[i].FadingOut = false;
            }

            animatingCount = 0;
        }

        private void HandleDied()
        {
            ClearAll();
            ReleaseStuckPooledVfx();
        }

        private void HandleRespawned()
        {
            ClearAll();
            if (health != null)
                lastHealth = health.CurrentHealth;
        }

        private void HandleHealthChanged(float current, float max)
        {
            float previous = lastHealth;
            lastHealth = current;
            if (previous < 0f || current <= previous + 0.001f || health == null || health.IsDead)
                return;

            // Regen hook: marks stamped at or below the restored health fade out (newest last).
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Used || slots[i].FadingOut || slots[i].HealthAtStamp > current)
                    continue;

                slots[i].FadingOut = true;
                slots[i].FadeStart = Time.time;
                animatingCount++;
            }
        }

        private void OnRangedHit(
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 surfaceNormal,
            Vector3 travelDirection,
            float damage,
            ItemData ammoItem,
            ItemData weapon)
        {
            DM_EnemyHitMarkProfile profile = Profile;
            DMAmmoFxProfile ammoProfile = DMCombatFx.ResolveProfile(ammoItem, weapon);
            AmmoType ammoType = DMEnemyHitFx.ResolveAmmoType(ammoItem, weapon, ammoProfile);
            bool allowsBlood = DMEnemyHitFx.AllowsBlood(ammoProfile, ammoType);
            bool allowsBurn = DMEnemyHitFx.AllowsBurn(ammoProfile, ammoType);

            Vector3 travel = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.zero;
            Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal.normalized : Vector3.zero;
            if (normal == Vector3.zero)
                normal = travel != Vector3.zero ? -travel : (hitPoint - transform.position - Vector3.up).normalized;
            if (travel == Vector3.zero)
                travel = -normal;

            DMEnemyHitbox hitbox = null;
            DMEnemyHitQuery.TryGetHitbox(hitCollider, out hitbox);
            Transform attach = hitbox != null ? hitbox.transform : (hitCollider != null ? hitCollider.transform : transform);

            if (DMEnemyHitFx.IsWithinFxDistance(hitPoint, profile.fxMaxDistance))
                SpawnSplatter(profile, hitPoint, normal, travel, damage, allowsBlood);

            bool alive = health == null || !health.IsDead;
            if (alive && allowsBurn)
            {
                DMEnemyBodyHitResponse response = profile.GetResponse(bodyType);
                DMHitMarkStyle style = profile.laserAlwaysGlowBurn && ammoType == AmmoType.Laser
                    ? DMHitMarkStyle.GlowBurn
                    : response.burnStyle;
                Transform bone = hitbox != null ? hitbox.Bone : attach;
                if (StampBurn(profile, bone, hitPoint, normal, style)
                    && style == DMHitMarkStyle.GlowBurn
                    && response.embers
                    && DMEnemyHitFx.IsWithinFxDistance(hitPoint, profile.fxMaxDistance))
                {
                    DMEnemyHitFx.SpawnAttached(
                        profile.burnEmberPrefab,
                        hitPoint + normal * 0.004f,
                        normal,
                        attach,
                        profile.emberGlowSeconds + 0.75f);
                }
            }
        }

        private void SpawnSplatter(
            DM_EnemyHitMarkProfile profile,
            Vector3 point,
            Vector3 normal,
            Vector3 travel,
            float damage,
            bool allowsBlood)
        {
            float now = Time.time;
            if (now - lastBurstTime < profile.perEnemyMinInterval)
                return;

            // §16 bans blood only; coolant and sparks stay for every element.
            DMEnemyBodyHitResponse response = profile.GetResponse(bodyType);
            GameObject splatter = response.splatter == DMEnemyHitFxKind.Blood && !allowsBlood
                ? null
                : profile.ResolveFxPrefab(response.splatter);
            GameObject sparks = response.sparks == DMEnemyHitFxKind.Blood && !allowsBlood
                ? null
                : profile.ResolveFxPrefab(response.sparks);

            if (splatter == null && sparks == null)
                return;
            if (!DMEnemyHitFx.TryConsumeBurstBudget(profile.fxBurstBudget))
                return;

            lastBurstTime = now;
            float scale = profile.ResolveSplatterScale(damage);
            Vector3 entry = DMEnemyHitFx.ResolveEntrySprayDirection(normal, travel, profile.bloodEntryNormalBlend);
            Vector3 spawnPoint = point + normal * 0.015f;

            if (splatter != null)
            {
                float splatterScale = scale * Mathf.Max(0.05f, response.splatterScale);
                DMEnemyHitFx.SpawnBurst(splatter, spawnPoint, entry, splatterScale);
                if (response.exitSpray && damage >= profile.exitSprayDamageThreshold && profile.exitSprayStrength > 0.01f)
                    DMEnemyHitFx.SpawnBurst(splatter, point + travel * 0.02f, travel, splatterScale * Mathf.Max(0.3f, profile.exitSprayStrength));
            }

            if (sparks != null)
                DMEnemyHitFx.SpawnBurst(sparks, spawnPoint, Vector3.Slerp(normal, entry, 0.5f), scale * Mathf.Max(0.05f, response.sparkScale));
        }

        private bool StampBurn(DM_EnemyHitMarkProfile profile, Transform bone, Vector3 point, Vector3 normal, DMHitMarkStyle style)
        {
            Material baseMaterial = style == DMHitMarkStyle.BloodChar ? profile.bulletBurnMaterial : profile.glowBurnMaterial;
            if (baseMaterial == null)
                return false;

            EnsureRenderersTagged(profile);
            int capacity = Mathf.Clamp(profile.maxBurnMarksPerEnemy, 1, MaxSlots);
            TrimToCapacity(capacity);
            int index = PickSlot(capacity);
            ref Slot slot = ref slots[index];
            EnsureSlotObjects(ref slot, index);

            if (slot.Glowing || slot.FadingOut)
                animatingCount = Mathf.Max(0, animatingCount - 1);

            Transform parent = bone != null ? bone : transform;
            Transform root = slot.Root.transform;
            if (root.parent != parent)
                root.SetParent(parent, false);
            root.localScale = Vector3.one;

            Vector3 forward = -normal;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(forward, up) * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
            root.SetPositionAndRotation(point, rotation);

            float size = Random.Range(Mathf.Min(profile.burnSize.x, profile.burnSize.y), Mathf.Max(profile.burnSize.x, profile.burnSize.y));
            float depth = Mathf.Max(0.01f, profile.burnProjectionDepth);
            Vector2 bias = AtlasBias[Random.Range(0, AtlasBias.Length)];

            ConfigureProjector(slot.Base, baseMaterial, new Vector3(size, size, depth), bias, profile);
            slot.Base.fadeFactor = 1f;
            slot.Base.enabled = true;

            bool glow = style == DMHitMarkStyle.GlowBurn && profile.glowBurnHotMaterial != null;
            if (glow)
            {
                float glowSize = size * Mathf.Clamp(profile.glowSizeScale, 0.2f, 1.2f);
                ConfigureProjector(slot.Glow, profile.glowBurnHotMaterial, new Vector3(glowSize, glowSize, depth), bias, profile);
                slot.Glow.fadeFactor = 1f;
                slot.Glow.enabled = true;
                slot.Glowing = true;
                slot.GlowStart = Time.time;
                animatingCount++;
            }
            else
            {
                slot.Glow.enabled = false;
                slot.Glowing = false;
            }

            slot.Used = true;
            slot.FadingOut = false;
            slot.StampTime = Time.time;
            slot.HealthAtStamp = health != null ? health.CurrentHealth : 0f;
            if (!slot.Root.activeSelf)
                slot.Root.SetActive(true);
            return true;
        }

        private void ConfigureProjector(DecalProjector projector, Material material, Vector3 size, Vector2 bias, DM_EnemyHitMarkProfile profile)
        {
            if (projector.material != material)
                projector.material = material;
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            projector.pivot = Vector3.zero;
            projector.size = size;
            projector.uvScale = new Vector2(0.5f, 0.5f);
            projector.uvBias = bias;
            projector.drawDistance = Mathf.Max(1f, profile.decalDrawDistance);
            projector.fadeScale = 0.9f;
            projector.startAngleFade = Mathf.Clamp(profile.burnAngleFade.x, 0f, 180f);
            projector.endAngleFade = Mathf.Clamp(profile.burnAngleFade.y, projector.startAngleFade, 180f);
            projector.affectsTransparency = false;
            projector.decalLayerMask = (UnityEngine.Rendering.HighDefinition.RenderingLayerMask)markLayerMask;
        }

        private int PickSlot(int capacity)
        {
            int oldest = 0;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < capacity; i++)
            {
                if (!slots[i].Used)
                    return i;
                if (slots[i].StampTime < oldestTime)
                {
                    oldestTime = slots[i].StampTime;
                    oldest = i;
                }
            }

            return oldest;
        }

        private void EnsureSlotObjects(ref Slot slot, int index)
        {
            if (slot.Root != null && slot.Base != null && slot.Glow != null)
                return;

            if (slot.Root == null)
            {
                slot.Root = new GameObject(SlotObjectPrefix + index);
                slot.Root.transform.SetParent(transform, false);
            }

            if (slot.Base == null)
            {
                slot.Base = slot.Root.GetComponent<DecalProjector>();
                if (slot.Base == null)
                    slot.Base = slot.Root.AddComponent<DecalProjector>();
            }

            if (slot.Glow == null)
            {
                GameObject glow = new GameObject("Glow");
                glow.transform.SetParent(slot.Root.transform, false);
                slot.Glow = glow.AddComponent<DecalProjector>();
                slot.Glow.enabled = false;
            }
        }

        private void EnsureRenderersTagged(DM_EnemyHitMarkProfile profile)
        {
            if (renderersTagged)
                return;

            renderersTagged = true;
            int layerIndex = UnityEngine.RenderingLayerMask.NameToRenderingLayer(profile.enemyDecalRenderingLayer);
            if (layerIndex < 0)
                layerIndex = Mathf.Clamp(profile.enemyDecalRenderingLayerFallbackIndex, 0, 31);
            markLayerMask = 1u << layerIndex;

            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer r = renderers[i];
                if (r == null || IsUnderWeaponHolder(r.transform))
                    continue;
                r.renderingLayerMask |= markLayerMask;
            }
        }

        private bool IsUnderWeaponHolder(Transform t)
        {
            while (t != null && t != transform)
            {
                string n = t.name;
                if (n.IndexOf("Weapon", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Holder", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.StartsWith("Drawn_", System.StringComparison.Ordinal)
                    || n.StartsWith("Holstered_", System.StringComparison.Ordinal))
                    return true;
                t = t.parent;
            }

            return false;
        }

        private void Update()
        {
            if (appliedProfileRevision != DM_EnemyHitMarkProfile.Revision)
                ApplyLiveProfile();

            if (animatingCount <= 0)
                return;

            DM_EnemyHitMarkProfile profile = Profile;
            float now = Time.time;
            int stillAnimating = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Used)
                    continue;

                if (slots[i].FadingOut)
                {
                    float t = Mathf.Clamp01((now - slots[i].FadeStart) / Mathf.Max(0.05f, profile.regenFadeSeconds));
                    float f = 1f - t;
                    if (slots[i].Base != null)
                        slots[i].Base.fadeFactor = f;
                    if (slots[i].Glow != null && slots[i].Glow.enabled)
                        slots[i].Glow.fadeFactor = Mathf.Min(slots[i].Glow.fadeFactor, f);
                    if (t >= 1f)
                        ClearSlot(i);
                    else
                        stillAnimating++;
                    continue;
                }

                if (!slots[i].Glowing || slots[i].Glow == null)
                    continue;

                float cool = Mathf.Clamp01((now - slots[i].GlowStart) / Mathf.Max(0.05f, profile.glowCoolSeconds));
                float eased = cool * cool * (3f - 2f * cool);
                slots[i].Glow.fadeFactor = Mathf.Lerp(1f, profile.glowResidual, eased);
                if (cool >= 1f)
                    slots[i].Glowing = false;
                else
                    stillAnimating++;
            }

            animatingCount = stillAnimating;
        }

        /// <summary>
        /// Profile edited (Genesis Studio / inspector, usually in Play): trim to the new mark limit and push draw
        /// distance, angle fade and cooled-glow residual onto marks already stamped.
        /// </summary>
        private void ApplyLiveProfile()
        {
            appliedProfileRevision = DM_EnemyHitMarkProfile.Revision;
            DM_EnemyHitMarkProfile profile = Profile;
            TrimToCapacity(Mathf.Clamp(profile.maxBurnMarksPerEnemy, 1, MaxSlots));

            float drawDistance = Mathf.Max(1f, profile.decalDrawDistance);
            float startFade = Mathf.Clamp(profile.burnAngleFade.x, 0f, 180f);
            float endFade = Mathf.Clamp(profile.burnAngleFade.y, startFade, 180f);
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Used)
                    continue;

                ApplyLiveProjector(slots[i].Base, drawDistance, startFade, endFade);
                ApplyLiveProjector(slots[i].Glow, drawDistance, startFade, endFade);
                if (!slots[i].Glowing && !slots[i].FadingOut && slots[i].Glow != null && slots[i].Glow.enabled)
                    slots[i].Glow.fadeFactor = profile.glowResidual;
            }
        }

        private static void ApplyLiveProjector(DecalProjector projector, float drawDistance, float startFade, float endFade)
        {
            if (projector == null || !projector.enabled)
                return;

            projector.drawDistance = drawDistance;
            projector.startAngleFade = startFade;
            projector.endAngleFade = endFade;
        }

        /// <summary>Removes marks beyond the limit (oldest first) so a lowered limit takes effect on live enemies.</summary>
        private void TrimToCapacity(int capacity)
        {
            int used = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Used)
                    used++;

            while (used > capacity)
            {
                int oldest = -1;
                float oldestTime = float.MaxValue;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].Used && slots[i].StampTime < oldestTime)
                    {
                        oldestTime = slots[i].StampTime;
                        oldest = i;
                    }
                }

                if (oldest < 0)
                    break;
                if (slots[oldest].Glowing || slots[oldest].FadingOut)
                    animatingCount = Mathf.Max(0, animatingCount - 1);
                ClearSlot(oldest);
                used--;
            }

            // Free slots must sit inside the limit, so pack any survivors beyond it into free low slots.
            for (int i = capacity; i < slots.Length; i++)
            {
                if (!slots[i].Used)
                    continue;

                for (int j = 0; j < capacity; j++)
                {
                    if (slots[j].Used)
                        continue;

                    Slot moved = slots[i];
                    slots[i] = slots[j];
                    slots[j] = moved;
                    break;
                }
            }
        }

        private void ClearSlot(int index)
        {
            ref Slot slot = ref slots[index];
            slot.Used = false;
            slot.Glowing = false;
            slot.FadingOut = false;
            if (slot.Base != null)
                slot.Base.enabled = false;
            if (slot.Glow != null)
                slot.Glow.enabled = false;
            if (slot.Root != null)
            {
                if (slot.Root.transform.parent != transform)
                    slot.Root.transform.SetParent(transform, false);
                slot.Root.SetActive(false);
            }
        }

        /// <summary>Removes every burn mark now (death, respawn, debug).</summary>
        public void ClearAll()
        {
            for (int i = 0; i < slots.Length; i++)
                ClearSlot(i);
            animatingCount = 0;
        }

        /// <summary>Stuck tracers / impact bursts / embers parented to hitboxes go back to the pool at death.</summary>
        private void ReleaseStuckPooledVfx()
        {
            if (rig == null)
                rig = GetComponent<DMEnemyHitboxRig>();
            if (rig == null)
                return;

            ReleaseBuffer.Clear();
            IReadOnlyList<DMEnemyHitbox> boxes = rig.Hitboxes;
            for (int i = 0; i < boxes.Count; i++)
            {
                DMEnemyHitbox box = boxes[i];
                if (box == null)
                    continue;

                Transform t = box.transform;
                for (int c = 0; c < t.childCount; c++)
                {
                    Transform child = t.GetChild(c);
                    if (child != null && child.GetComponent<PooledInstanceTag>() != null)
                        ReleaseBuffer.Add(child);
                }
            }

            for (int i = 0; i < ReleaseBuffer.Count; i++)
            {
                if (ReleaseBuffer[i] != null)
                    PoolManager.Release(ReleaseBuffer[i].gameObject);
            }

            ReleaseBuffer.Clear();
        }
    }
}
