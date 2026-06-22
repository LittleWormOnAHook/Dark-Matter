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
    public class TrainingDummy : MonoBehaviour, IDamageable
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

        private float currentHealth;
        private float lastDamageTime = float.NegativeInfinity;
        private float deathTime;
        private bool isDead;
        private bool pendingReset;
        private Vector3 springOffset;
        private Vector3 springVelocity;
        private Vector3 springRotation;
        private Vector3 springAngularVelocity;
        private BoxCollider hitCollider;
        private DummyCombatUI combatUi;

        public event Action<float, float> HealthChanged;

        public Transform DamageNumberAnchor =>
            damageNumberAnchor != null ? damageNumberAnchor : transform;

        public Transform HealthBarAnchor =>
            healthBarAnchor != null ? healthBarAnchor : DamageNumberAnchor;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            EnsureDummyTargetPrefabLoaded();
            AdoptNearbySceneDummyTarget();
            EnsureDummyTargetVisual();
            ResolveAnchors();
            EnsureHitCollider();
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
            EnsureCombatUi();
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
            ApplyHitReaction(source);
            NotifyHealthChanged();
            ShowDamageFeedback(damage, isCritical);

            if (currentHealth <= 0f)
                HandleDeath();
        }

        private void HandleDeath()
        {
            if (!resetOnDeath)
            {
                isDead = true;
                return;
            }

            isDead = true;
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
        }

        public void ShowDamageFeedback(float damage, bool isCritical = false)
        {
            EnsureCombatUi();
            combatUi?.ShowDamage(damage, isCritical);
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

            FitBoxColliderFromRenderers(hitCollider);
        }

        private void EnsureCombatUi()
        {
            if (combatUi != null)
                return;

            combatUi = GetComponent<DummyCombatUI>();
            if (combatUi == null)
                combatUi = gameObject.AddComponent<DummyCombatUI>();

            combatUi.Initialize(this);
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

        private void ApplyHitReaction(GameObject source)
        {
            if (source == null)
                return;

            Vector3 hitDirection = transform.position - source.transform.position;
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude < 0.0001f)
                hitDirection = -transform.forward;

            hitDirection.Normalize();
            springVelocity += hitDirection * hitImpulse;
            springAngularVelocity += Vector3.Cross(Vector3.up, hitDirection) * hitTorque;
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
                return;

            float deltaTime = Time.deltaTime;

            springVelocity += (-springOffset * positionSpring) * deltaTime;
            springVelocity *= Mathf.Exp(-positionDamping * deltaTime);
            springOffset += springVelocity * deltaTime;
            springOffset = Vector3.ClampMagnitude(springOffset, maxPositionOffset);

            springAngularVelocity += (-springRotation * rotationSpring) * deltaTime;
            springAngularVelocity *= Mathf.Exp(-rotationDamping * deltaTime);
            springRotation += springAngularVelocity * deltaTime;
            springRotation.x = Mathf.Clamp(springRotation.x, -maxRotationOffset, maxRotationOffset);
            springRotation.y = Mathf.Clamp(springRotation.y, -maxRotationOffset, maxRotationOffset);
            springRotation.z = Mathf.Clamp(springRotation.z, -maxRotationOffset, maxRotationOffset);

            visualRoot.localPosition = springOffset;
            visualRoot.localRotation = Quaternion.Euler(springRotation);
        }

        private void ResetSpring()
        {
            springOffset = Vector3.zero;
            springVelocity = Vector3.zero;
            springRotation = Vector3.zero;
            springAngularVelocity = Vector3.zero;

            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
            combatUi?.RefreshHealth(currentHealth, maxHealth);
        }
    }
}
