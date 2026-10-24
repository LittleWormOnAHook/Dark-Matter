using System;
using Project.Interaction;
using Project.UI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Combat
{
    /// <summary>
    /// Practice target using the Blink DummyTarget model. Springs on hit and shows combat UI.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable, IEngagedHealthHudTarget
    {
        private const string DummyTargetAssetPath =
            "Assets/Blink/Art/NPCs/Stylized/DummyTarget/DummyTarget.prefab";

        [Header("Model")]
        [SerializeField] private GameObject dummyTargetPrefab;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform damageNumberAnchor;
        [SerializeField] private Transform healthBarAnchor;

        [Header("Health")]
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private bool resetHealthOnEnable = true;

        [Header("Death Reset")]
        [SerializeField] private bool resetOnDeath = true;
        [SerializeField] private float resetDelayAfterDeath = 2f;

        [Header("Health Regen")]
        [SerializeField] private bool enableHealthRegen = false;
        [SerializeField] private float healthRegenPerSecond = 15f;
        [SerializeField] private float healthRegenDelayAfterDamage = 3f;

        [Header("Spring Reaction")]
        [SerializeField] private float positionSpring = 20f;
        [SerializeField] private float positionDamping = 7f;
        [SerializeField] private float rotationSpring = 24f;
        [SerializeField] private float rotationDamping = 8f;
        [SerializeField] private float hitImpulse = 0.85f;
        [SerializeField] private float hitTorque = 32f;
        [SerializeField] private float maxPositionOffset = 0.45f;
        [SerializeField] private float maxRotationOffset = 24f;
        [SerializeField] private float poiseStaggerImpulseScale = 1f;

        private float currentHealth;
        private float lastDamageTime = float.NegativeInfinity;
        private float deathTime;
        private bool isDead;
        private bool pendingReset;
        // 2026-10-24m: hit kind hint (TakeDamage has no type). Callers set NextHitKind right before damaging; consumed once.
        public enum HitKind { Auto = 0, Melee = 1, Ranged = 2, BeamTick = 3 }
        public static HitKind NextHitKind = HitKind.Auto;
        private Vector2 tilt, tiltVel;           // x = pitch, y = roll (deg, deg/s)
        private float kickWindowStart = -10f, kickWindowUsed;
        private Vector3 basePivotLocal;
        private bool basePivotResolved;
        private Vector3 springOffset;
        private Vector3 springVelocity;
        private Vector3 springRotation;
        private Vector3 springAngularVelocity;
        private BoxCollider hitCollider;
        private TrainingDummyEngagedHudPresenter engagedHudPresenter;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public Transform DamageNumberAnchor =>
            damageNumberAnchor != null ? damageNumberAnchor : transform;

        public Transform HealthBarAnchor =>
            healthBarAnchor != null ? healthBarAnchor : DamageNumberAnchor;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            DummyCombatUI legacyUi = GetComponent<DummyCombatUI>();
            if (legacyUi != null)
                Destroy(legacyUi);

            EnsureDummyTargetPrefabLoaded();
            AdoptNearbySceneDummyTarget();
            EnsureDummyTargetVisual();
            ResolveAnchors();
            EnsureHitCollider();
            EnsureMeleeDamageReceiver();
        }

        private void OnEnable()
        {
            if (resetHealthOnEnable)
                currentHealth = maxHealth;

            ResetSpring();
            NotifyHealthChanged();
        }

        private void Start()
        {
            EnsureEngagedHudPresenter();
            NotifyHealthChanged();
        }

        private void Update()
        {
            UpdateDeathReset();
            UpdateHealthRegen();
        }

        public void TakeDamage(float damage, GameObject source, bool isCritical = false)
        {
            if (damage <= 0f || isDead)
                return;

            lastDamageTime = Time.time;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            HitKind kind = NextHitKind; NextHitKind = HitKind.Auto;
            ApplyHitReaction(source, 1f, kind);
            NotifyHealthChanged();
            ShowDamageFeedback(damage, isCritical);
            engagedHudPresenter?.NotifyDamagedByPlayer(source);

            if (currentHealth <= 0f)
                HandleDeath();
        }

        private void HandleDeath()
        {
            if (!resetOnDeath)
            {
                isDead = true;
                Died?.Invoke();
                return;
            }

            isDead = true;
            Died?.Invoke();
            pendingReset = true;
            deathTime = Time.time;
        }

        private void UpdateDeathReset()
        {
            if (!pendingReset || !resetOnDeath)
                return;

            if (Time.time < deathTime + resetDelayAfterDeath)
                return;

            pendingReset = false;
            ResetDummy();
        }

        private void UpdateHealthRegen()
        {
            if (!enableHealthRegen || isDead || currentHealth >= maxHealth)
                return;

            if (Time.time < lastDamageTime + healthRegenDelayAfterDamage)
                return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenPerSecond * Time.deltaTime);
            NotifyHealthChanged();
        }

        public void ResetDummy()
        {
            isDead = false;
            pendingReset = false;
            currentHealth = maxHealth;
            ResetSpring();
            NotifyHealthChanged();
            EngagedEnemyHealthHud.Instance?.ClearIf(this);
        }

        public void ShowDamageFeedback(float damage, bool isCritical = false)
        {
            if (damage <= 0f)
                return;

            CombatUiSpawner.ShowDamage(damage, DamageNumberAnchor.position, isCritical);
        }

        /// <summary>Visual-only stagger when poise breaks (no extra health loss).</summary>
        public void ApplyPoiseStaggerReaction(GameObject source)
        {
            if (isDead)
                return;

            ApplyHitReaction(source, ResolvePoiseStaggerImpulseScale());
        }

        private void EnsureDummyTargetPrefabLoaded()
        {
            if (dummyTargetPrefab != null)
                return;

            dummyTargetPrefab = Resources.Load<GameObject>("Combat/DummyTarget");

#if UNITY_EDITOR
            if (dummyTargetPrefab == null)
                dummyTargetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyTargetAssetPath);
#endif
        }

        private void AdoptNearbySceneDummyTarget()
        {
            if (HasValidModel(visualRoot))
                return;

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name != "DummyTarget")
                    continue;

                if (candidate.IsChildOf(transform))
                {
                    candidate.gameObject.SetActive(true);
                    visualRoot = candidate;
                    return;
                }

                if (Vector3.Distance(candidate.position, transform.position) > 3f)
                    continue;

                Vector3 worldPosition = candidate.position;
                Quaternion worldRotation = candidate.rotation;
                candidate.SetParent(transform, false);
                candidate.localPosition = Vector3.zero;
                candidate.localRotation = Quaternion.identity;
                candidate.localScale = Vector3.one;
                candidate.name = "Visual";
                candidate.gameObject.SetActive(true);
                transform.SetPositionAndRotation(worldPosition, worldRotation);
                visualRoot = candidate;
                RemoveCapsuleVisual();
                return;
            }
        }

        private void EnsureDummyTargetVisual()
        {
            if (HasValidModel(visualRoot))
                return;

            RemoveCapsuleVisual();

            if (dummyTargetPrefab == null)
            {
                Debug.LogWarning("TrainingDummy: DummyTarget prefab is not assigned.");
                return;
            }

            GameObject instance = Instantiate(dummyTargetPrefab, transform);
            instance.name = "Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            visualRoot = instance.transform;
        }

        private void ResolveAnchors()
        {
            if (visualRoot == null)
            {
                Transform visual = transform.Find("Visual") ?? transform.Find("DummyTarget");
                visualRoot = visual != null ? visual : transform;
            }

            Bounds bounds = GetRendererBounds();
            float topY = bounds.max.y - transform.position.y + 0.05f;

            damageNumberAnchor = EnsureAnchor("DamageNumberAnchor", new Vector3(0f, topY, 0f));
            healthBarAnchor = EnsureAnchor("HealthBarAnchor", new Vector3(0f, topY + 0.05f, 0f));
        }

        private Transform EnsureAnchor(string anchorName, Vector3 localPosition)
        {
            Transform existing = transform.Find(anchorName);
            if (existing == null)
            {
                GameObject anchor = new GameObject(anchorName);
                anchor.transform.SetParent(transform, false);
                existing = anchor.transform;
            }

            existing.localPosition = localPosition;
            return existing;
        }

        private void EnsureHitCollider()
        {
            hitCollider = GetComponent<BoxCollider>();
            if (hitCollider == null)
                hitCollider = gameObject.AddComponent<BoxCollider>();

            hitCollider.isTrigger = false;
            hitCollider.enabled = true;
            FitBoxColliderFromRenderers(hitCollider);
        }

        private void EnsureMeleeDamageReceiver()
        {
            if (CompareTag("Untagged"))
                gameObject.tag = "Enemy";

            if (GetComponent<PioneerInvectorDamageReceiver>() == null)
                gameObject.AddComponent<PioneerInvectorDamageReceiver>();
        }

        private void EnsureEngagedHudPresenter()
        {
            if (engagedHudPresenter != null)
                return;

            engagedHudPresenter = GetComponent<TrainingDummyEngagedHudPresenter>();
            if (engagedHudPresenter == null)
                engagedHudPresenter = gameObject.AddComponent<TrainingDummyEngagedHudPresenter>();
        }

        public static void FitBoxColliderFromRenderers(BoxCollider boxCollider)
        {
            if (boxCollider == null)
                return;

            Renderer[] renderers = boxCollider.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Transform colliderTransform = boxCollider.transform;
            boxCollider.center = colliderTransform.InverseTransformPoint(bounds.center);
            Vector3 lossy = colliderTransform.lossyScale;
            boxCollider.size = new Vector3(
                SafeDivide(bounds.size.x, Mathf.Abs(lossy.x)),
                SafeDivide(bounds.size.y, Mathf.Abs(lossy.y)),
                SafeDivide(bounds.size.z, Mathf.Abs(lossy.z)));
        }

        private Bounds GetRendererBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(transform.position + Vector3.up, Vector3.one * 2f);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }

        private static bool HasValidModel(Transform root)
        {
            if (root == null)
                return false;

            MeshFilter meshFilter = root.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return false;

            return meshFilter.sharedMesh.name != "Capsule";
        }

        private void RemoveCapsuleVisual()
        {
            Transform visual = transform.Find("Visual");
            if (visual == null)
                return;

            MeshFilter meshFilter = visual.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == "Capsule")
                Destroy(visual.gameObject);
        }

        private void ApplyHitReaction(GameObject source, float impulseScale = 1f, HitKind kind = HitKind.Auto)
        {
            if (source == null)
                return;

            DM_CombatCoreProfile pivotProfile = DM_CombatCoreProfile.Live;
            if (pivotProfile != null && pivotProfile.trainingDummyPivotBase)
            {
                ApplyPivotKick(pivotProfile, source, impulseScale, kind);
                return;
            }

            ResolveSpringTuning(
                out _,
                out _,
                out _,
                out _,
                out float resolvedHitImpulse,
                out float resolvedHitTorque,
                out _,
                out _);

            Vector3 hitDirection = transform.position - source.transform.position;
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude < 0.0001f)
                hitDirection = -transform.forward;

            hitDirection.Normalize();
            springVelocity += hitDirection * (resolvedHitImpulse * impulseScale);
            springAngularVelocity += Vector3.Cross(Vector3.up, hitDirection) * (resolvedHitTorque * impulseScale);
        }

        private void ApplyPivotKick(DM_CombatCoreProfile pp, GameObject source, float scale, HitKind kind)
        {
            Vector3 dir = transform.position - source.transform.position; dir.y = 0f;
            if (kind == HitKind.Auto)
                kind = dir.magnitude <= pp.trainingDummyMeleeDistance ? HitKind.Melee : HitKind.Ranged;
            if (dir.sqrMagnitude < 1e-4f) dir = -transform.forward;
            dir.Normalize();
            float kick = (kind == HitKind.Melee ? pp.trainingDummyMeleeTiltKick : kind == HitKind.BeamTick ? pp.trainingDummyBeamTickTiltKick : pp.trainingDummyRangedTiltKick) * scale;
            if (pp.trainingDummyKickWindowSeconds > 0f)
            {
                if (Time.time - kickWindowStart > pp.trainingDummyKickWindowSeconds) { kickWindowStart = Time.time; kickWindowUsed = 0f; }
                kick = Mathf.Min(kick, Mathf.Max(0f, pp.trainingDummyKickWindowCap - kickWindowUsed));
                kickWindowUsed += kick;
            }
            if (kick <= 0f) return;
            // tilt away from the source, around the base: local axis = cross(up, dir)
            Vector3 axisW = Vector3.Cross(Vector3.up, dir);
            Vector3 axisL = transform.InverseTransformDirection(axisW);
            tiltVel += new Vector2(axisL.x, axisL.z) * kick;
        }

        private void ResolveBasePivot()
        {
            basePivotResolved = true;
            basePivotLocal = Vector3.zero;
            if (visualRoot == null) return;
            Quaternion r0 = visualRoot.localRotation; Vector3 p0 = visualRoot.localPosition;
            visualRoot.localRotation = Quaternion.identity; visualRoot.localPosition = Vector3.zero;
            bool has = false; Bounds b = default;
            foreach (var r in visualRoot.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            visualRoot.localRotation = r0; visualRoot.localPosition = p0;
            if (has) { Vector3 w = new Vector3(b.center.x, b.min.y, b.center.z); basePivotLocal = transform.InverseTransformPoint(w); }
        }

        private void UpdatePivot(DM_CombatCoreProfile pp, float dt)
        {
            if (!basePivotResolved) ResolveBasePivot();
            dt = Mathf.Min(dt, 0.05f);
            float w = 2f * Mathf.PI * Mathf.Max(0.01f, pp.trainingDummyTiltFrequency);
            float z = Mathf.Max(0f, pp.trainingDummyTiltDampingRatio);
            // semi-implicit spring-damper: a = -w^2 x - 2 z w v
            tiltVel += (-w * w * tilt - 2f * z * w * tiltVel) * dt;
            tilt += tiltVel * dt;
            float max = pp.trainingDummyMaxTiltDegrees;
            if (tilt.magnitude > max)
            {
                tilt = max > 0f ? tilt.normalized * max : Vector2.zero;
                Vector2 n = tilt.sqrMagnitude > 1e-6f ? tilt.normalized : Vector2.zero;
                float outward = Vector2.Dot(tiltVel, n);
                if (outward > 0f) tiltVel -= n * outward;
            }
            Quaternion q = Quaternion.Euler(tilt.x, 0f, tilt.y);
            // rotate around the base pivot only: no translation of the base
            visualRoot.localRotation = q;
            visualRoot.localPosition = basePivotLocal - q * basePivotLocal;
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
                return;

            DM_CombatCoreProfile pivotProfile = DM_CombatCoreProfile.Live;
            if (pivotProfile != null && pivotProfile.trainingDummyPivotBase)
            {
                UpdatePivot(pivotProfile, Time.deltaTime);
                return;
            }

            ResolveSpringTuning(
                out float resolvedPositionSpring,
                out float resolvedPositionDamping,
                out float resolvedRotationSpring,
                out float resolvedRotationDamping,
                out _,
                out _,
                out float resolvedMaxPositionOffset,
                out float resolvedMaxRotationOffset);

            float deltaTime = Time.deltaTime;

            springVelocity += (-springOffset * resolvedPositionSpring) * deltaTime;
            springVelocity *= Mathf.Exp(-resolvedPositionDamping * deltaTime);
            springOffset += springVelocity * deltaTime;
            springOffset = Vector3.ClampMagnitude(springOffset, resolvedMaxPositionOffset);

            springAngularVelocity += (-springRotation * resolvedRotationSpring) * deltaTime;
            springAngularVelocity *= Mathf.Exp(-resolvedRotationDamping * deltaTime);
            springRotation += springAngularVelocity * deltaTime;
            springRotation.x = Mathf.Clamp(springRotation.x, -resolvedMaxRotationOffset, resolvedMaxRotationOffset);
            springRotation.y = Mathf.Clamp(springRotation.y, -resolvedMaxRotationOffset, resolvedMaxRotationOffset);
            springRotation.z = Mathf.Clamp(springRotation.z, -resolvedMaxRotationOffset, resolvedMaxRotationOffset);

            visualRoot.localPosition = springOffset;
            visualRoot.localRotation = Quaternion.Euler(springRotation);
        }

        private void ResolveSpringTuning(
            out float resolvedPositionSpring,
            out float resolvedPositionDamping,
            out float resolvedRotationSpring,
            out float resolvedRotationDamping,
            out float resolvedHitImpulse,
            out float resolvedHitTorque,
            out float resolvedMaxPositionOffset,
            out float resolvedMaxRotationOffset)
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
            {
                resolvedPositionSpring = positionSpring;
                resolvedPositionDamping = positionDamping;
                resolvedRotationSpring = rotationSpring;
                resolvedRotationDamping = rotationDamping;
                resolvedHitImpulse = hitImpulse;
                resolvedHitTorque = hitTorque;
                resolvedMaxPositionOffset = maxPositionOffset;
                resolvedMaxRotationOffset = maxRotationOffset;
                return;
            }

            resolvedPositionSpring = profile.trainingDummyPositionSpring;
            resolvedPositionDamping = profile.trainingDummyPositionDamping;
            resolvedRotationSpring = profile.trainingDummyRotationSpring;
            resolvedRotationDamping = profile.trainingDummyRotationDamping;
            resolvedHitImpulse = profile.trainingDummyHitImpulse;
            resolvedHitTorque = profile.trainingDummyHitTorque;
            resolvedMaxPositionOffset = profile.trainingDummyMaxPositionOffset;
            resolvedMaxRotationOffset = profile.trainingDummyMaxRotationOffset;
        }

        private float ResolvePoiseStaggerImpulseScale()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            return profile != null ? profile.trainingDummyPoiseStaggerImpulseScale : poiseStaggerImpulseScale;
        }

        private void ResetSpring()
        {
            springOffset = Vector3.zero;
            springVelocity = Vector3.zero;
            springRotation = Vector3.zero;
            springAngularVelocity = Vector3.zero;
            tilt = Vector2.zero; tiltVel = Vector2.zero; kickWindowUsed = 0f;

            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
