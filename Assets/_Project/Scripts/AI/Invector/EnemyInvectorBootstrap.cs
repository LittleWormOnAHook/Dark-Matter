using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Combat;
using Project.Player.Invector;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Initializes an Invector humanoid enemy cloned from Player_Invector — strips player-only systems.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public class EnemyInvectorBootstrap : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition enemyDefinition;
        [SerializeField] private float hitCapsuleRadius = 0.45f;

        public EnemyDefinition Definition => enemyDefinition;
        [SerializeField] private float hitCapsuleHeight = 2f;
        [SerializeField] private Vector3 hitCapsuleCenter = new Vector3(0f, 1f, 0f);
        [SerializeField] private bool infiniteAmmo = true;

        public vThirdPersonController ThirdPersonController { get; private set; }
        public vShooterManager ShooterManager { get; private set; }
        public vMeleeManager MeleeManager { get; private set; }

        public bool IsActive => isActiveAndEnabled && ThirdPersonController != null;

        private bool _invectorInitialized;

        private void Awake()
        {
            ThirdPersonController = GetComponent<vThirdPersonController>();
            ShooterManager = GetComponent<vShooterManager>();
            MeleeManager = GetComponent<vMeleeManager>();

            if (ShooterManager != null)
            {
                ShooterManager.useAmmoDisplay = false;
                ShooterManager.useLockOn = false;
                ShooterManager.applyRecoilToCamera = false;
                ShooterManager.AllAmmoInfinity = infiniteAmmo;
                PioneerInvectorShooterLayers.ApplyToShooterManager(ShooterManager);
                ShooterManager.onEquipWeapon.AddListener(HandleShooterWeaponEquipped);
            }

            gameObject.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                SetLayerRecursive(transform, enemyLayer);

            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(gameObject);

            EnemyInvectorComponentStripper.StripRuntime(gameObject);
            DestroyFootstepTriggersIfPresent(gameObject);
            EnemyInvectorGameplaySetup.Ensure(gameObject, enemyDefinition);
            EnemyInvectorTargetLayers.Apply(gameObject);
            DisableInvectorHealthDeath();

            EnemyInvectorHitSetup.EnsureRootCapsule(
                gameObject,
                hitCapsuleRadius,
                hitCapsuleHeight,
                hitCapsuleCenter);
            EnsureInvectorInitialized();
            EnemyInvectorHitSetup.Apply(gameObject, hitCapsuleRadius, hitCapsuleHeight, hitCapsuleCenter);
            if (GetComponent<EnemyInvectorPhysicsCache>() == null)
                gameObject.AddComponent<EnemyInvectorPhysicsCache>();
            EnsureInvectorPhysicsReady();
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(gameObject);
            EnemyInvectorRagdollSetup.EnsurePresent(gameObject);
            EnemyInvectorBodySnapSetup.ApplyRuntime(gameObject);

            if (GetComponent<EnemyInvectorLoadoutBridge>() == null)
                gameObject.AddComponent<EnemyInvectorLoadoutBridge>();
            if (GetComponent<EnemyInvectorMotorBridge>() == null)
                gameObject.AddComponent<EnemyInvectorMotorBridge>();
            if (GetComponent<EnemyInvectorCombatBridge>() == null)
                gameObject.AddComponent<EnemyInvectorCombatBridge>();
            if (GetComponent<EnemyInvectorOutgoingDamageBridge>() == null)
                gameObject.AddComponent<EnemyInvectorOutgoingDamageBridge>();
            if (GetComponent<EnemyInvectorProjectileBridge>() == null)
                gameObject.AddComponent<EnemyInvectorProjectileBridge>();
            if (GetComponent<EnemyTerrainRescue>() == null)
                gameObject.AddComponent<EnemyTerrainRescue>();
            if (GetComponent<EnemyInvectorRagdollBridge>() == null)
                gameObject.AddComponent<EnemyInvectorRagdollBridge>();
            if (GetComponent<EnemyInvectorDeathPresenter>() == null)
                gameObject.AddComponent<EnemyInvectorDeathPresenter>();
            if (GetComponent<EnemyDeathSequence>() == null)
                gameObject.AddComponent<EnemyDeathSequence>();
            if (GetComponent<HumanoidPerformanceController>() == null)
                gameObject.AddComponent<HumanoidPerformanceController>();

            EnemyInvectorLoadoutBridge loadout = GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout != null && enemyDefinition != null)
                loadout.ConfigureFromDefinition(enemyDefinition);
        }

        private void Start()
        {
            StartCoroutine(InitializeAfterInvector());
        }

        private System.Collections.IEnumerator InitializeAfterInvector()
        {
            yield return null;
            yield return new WaitForFixedUpdate();
            DisableInvectorHealthDeath();
            EnsureInvectorInitialized();
            EnemyInvectorHitSetup.Apply(gameObject, hitCapsuleRadius, hitCapsuleHeight, hitCapsuleCenter);
            EnsureInvectorPhysicsReady();
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(gameObject);

            EnemyInvectorLoadoutBridge loadout = GetComponent<EnemyInvectorLoadoutBridge>();
            EnemyInvectorBodySnapSetup.ApplyRuntime(gameObject);
            loadout?.EquipStartingWeapon();
        }

        private void OnDestroy()
        {
            if (ShooterManager != null)
                ShooterManager.onEquipWeapon.RemoveListener(HandleShooterWeaponEquipped);
        }

        public void EnsureInvectorInitialized()
        {
            if (_invectorInitialized || ThirdPersonController == null)
                return;

            if (GetComponent<CapsuleCollider>() == null)
            {
                EnemyInvectorHitSetup.EnsureRootCapsule(
                    gameObject,
                    hitCapsuleRadius,
                    hitCapsuleHeight,
                    hitCapsuleCenter);
            }

            DisableInvectorHealthDeath();
            ConfigureTransformDrivenMotor();
            ThirdPersonController.disableAnimations = false;
            ThirdPersonController.Init();
            ThirdPersonController.lockMovement = true;
            ThirdPersonController.useRootMotion = false;
            ThirdPersonController.isGrounded = true;
            ThirdPersonController.fallDamage = 0f;
            ThirdPersonController.ragdollVelocity = 0f;
            _invectorInitialized = true;
        }

        public void EnsureInvectorPhysicsReady()
        {
            EnsureInvectorInitialized();

            bool isCorpse = ThirdPersonController != null &&
                            (ThirdPersonController.isDead || ThirdPersonController.ragdolled);
            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null && health.IsDead)
                isCorpse = true;

            EnemyInvectorRagdollBridge ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            if (ragdollBridge != null && (ragdollBridge.IsHitStaggerActive || ragdollBridge.HasActiveRagdoll))
                isCorpse = true;

            if (!isCorpse)
            {
                EnemyInvectorPhysicsCache cache = GetComponent<EnemyInvectorPhysicsCache>();
                if (cache == null)
                    cache = gameObject.AddComponent<EnemyInvectorPhysicsCache>();
                cache.StabilizeBonesIfNeeded();
            }

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.enabled = true;
                capsule.isTrigger = false;
            }

            Animator animator = ThirdPersonController != null ? ThirdPersonController.animator : GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
                animator.updateMode = AnimatorUpdateMode.Normal;
                // AlwaysAnimate avoids intermittent chase glides when CullUpdateTransforms
                // skips bone writes while NavMesh still moves the root.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
            }

            EnsureVisualMeshesUpdateOffscreen();

            if (ThirdPersonController != null)
            {
                ThirdPersonController.disableAnimations = false;
                ThirdPersonController.lockMovement = true;
                ThirdPersonController.useRootMotion = false;
                ThirdPersonController.isGrounded = true;
                ConfigureTransformDrivenMotor();
            }
        }

        private void ConfigureTransformDrivenMotor()
        {
            if (ThirdPersonController == null)
                return;

            ThirdPersonController.disableCheckGround = true;
            ThirdPersonController.useStepOffset = false;
            ThirdPersonController.useSnapGround = false;
            ThirdPersonController.extraGravity = 0f;
            ThirdPersonController.lockMovement = true;
            ThirdPersonController.useRootMotion = false;
            ThirdPersonController.isGrounded = true;
        }

        private void EnsureVisualMeshesUpdateOffscreen()
        {
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                // Enabled Meshy body only — keep stock VBOT / props untouched.
                renderer.updateWhenOffscreen = true;
            }
        }

        private void HandleShooterWeaponEquipped(vShooterWeapon weapon, bool isLeftWeapon)
        {
            if (weapon == null || ShooterManager == null)
                return;

            weapon.hitLayer = ShooterManager.damageLayer;
        }

        private void DisableInvectorHealthDeath()
        {
            EnemyHealth pioneerHealth = GetComponent<EnemyHealth>();
            if (pioneerHealth != null && pioneerHealth.IsDead)
                return;

            if (ThirdPersonController is vHealthController healthController)
            {
                healthController.isDead = false;
                healthController.ResetHealth();
                healthController.isImmortal = true;
            }
        }

        private static void SetLayerRecursive(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursive(root.GetChild(i), layer);
        }

        private static void DestroyFootstepTriggersIfPresent(GameObject root)
        {
            vFootStepTrigger[] triggers = root.GetComponentsInChildren<vFootStepTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null)
                    UnityEngine.Object.Destroy(triggers[i]);
            }
        }
    }
}
