using System.Collections;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Companions.Abilities;
using Project.AI;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Initializes an Invector-based companion body. No singleton; one instance per spawned pioneer.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public class CompanionInvectorBootstrap : MonoBehaviour
    {
        public vThirdPersonController ThirdPersonController { get; private set; }
        public vShooterManager ShooterManager { get; private set; }
        public vMeleeManager MeleeManager { get; private set; }

        public bool IsActive => isActiveAndEnabled && ThirdPersonController != null;

        private int _kinematicGuardFrames;
        private bool _invectorInitialized;

        public static bool IsInvectorCompanion(Component component)
        {
            return HasInvectorStack(component);
        }

        public static bool HasInvectorStack(Component component)
        {
            return component != null &&
                   component.GetComponent<CompanionInvectorBootstrap>() != null;
        }

        private void Awake()
        {
            ThirdPersonController = GetComponent<vThirdPersonController>();
            ShooterManager = GetComponent<vShooterManager>();
            MeleeManager = GetComponent<vMeleeManager>();

            if (ShooterManager != null)
            {
                ShooterManager.useAmmoDisplay = false;
                PioneerInvectorShooterLayers.ApplyToShooterManager(ShooterManager);
                ShooterManager.onEquipWeapon.AddListener(HandleShooterWeaponEquipped);
            }

            gameObject.tag = "CompanionAI";
            int companionLayer = LayerMask.NameToLayer("CompanionAI");
            if (companionLayer >= 0)
                gameObject.layer = companionLayer;

            StripPlayerOnlyComponents();
            DestroyFootstepTriggers();
            SnapBodyContainersToLocalBones();
            DisableInvectorStandaloneUi();
            DisableInvectorHealthDeath();
            _kinematicGuardFrames = 8;
            EnsureInvectorInitialized();

            if (GetComponent<CompanionAbilityController>() == null)
                gameObject.AddComponent<CompanionAbilityController>();
            if (GetComponent<CompanionInvectorLoadoutBridge>() == null)
                gameObject.AddComponent<CompanionInvectorLoadoutBridge>();
            if (GetComponent<CompanionInvectorMotorBridge>() == null)
                gameObject.AddComponent<CompanionInvectorMotorBridge>();
            if (GetComponent<CompanionInvectorDamageBridge>() == null)
                gameObject.AddComponent<CompanionInvectorDamageBridge>();
            if (GetComponent<CompanionInvectorIncomingDamageBridge>() == null)
                gameObject.AddComponent<CompanionInvectorIncomingDamageBridge>();
            if (GetComponent<CompanionInvectorCombatBridge>() == null)
                gameObject.AddComponent<CompanionInvectorCombatBridge>();
            if (GetComponent<PioneerTerrainRescue>() == null)
                gameObject.AddComponent<PioneerTerrainRescue>();
            if (GetComponent<HumanoidPerformanceController>() == null)
                gameObject.AddComponent<HumanoidPerformanceController>();
        }

        private void Start()
        {
            StartCoroutine(InitializeAfterInvector());
        }

        private IEnumerator InitializeAfterInvector()
        {
            yield return null;
            yield return new WaitForFixedUpdate();
            EnsureInvectorPhysicsReady();
        }

        public void EnsureInvectorPhysicsReady()
        {
            if (ThirdPersonController != null &&
                (ThirdPersonController.isDead || ThirdPersonController.ragdolled))
                return;

            EnsureInvectorInitialized();

            Rigidbody body = GetComponent<Rigidbody>();
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();

            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if (capsule != null)
            {
                capsule.enabled = true;
                capsule.isTrigger = false;
            }

            // Companions are transform-driven at render rate (CompanionFollowController.Update),
            // so the animator must not tick at the fixed physics rate or movement stutters.
            Animator animator = ThirdPersonController != null ? ThirdPersonController.animator : GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.applyRootMotion = false;
            }

            if (ThirdPersonController != null)
            {
                ThirdPersonController.lockMovement = true;
                ThirdPersonController.useRootMotion = false;
                ThirdPersonController.isGrounded = true;
                ThirdPersonController.fallDamage = 0f;
                ThirdPersonController.ragdollVelocity = 0f;
            }
        }

        private void EnsureInvectorInitialized()
        {
            if (_invectorInitialized || ThirdPersonController == null)
                return;

            ThirdPersonController.Init();
            ThirdPersonController.lockMovement = true;
            ThirdPersonController.useRootMotion = false;
            ThirdPersonController.isGrounded = true;
            _invectorInitialized = true;
        }

        private void FixedUpdate()
        {
            if (_kinematicGuardFrames <= 0 || ThirdPersonController == null)
                return;

            if (ThirdPersonController.isDead || ThirdPersonController.ragdolled)
                return;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            _kinematicGuardFrames--;
        }

        private void StripPlayerOnlyComponents()
        {
            // Companions never throw grenades/smoke — remove the whole ThrowManager
            // hierarchy cloned from the player prefab so it can't ever activate.
            foreach (global::Invector.Throw.vThrowManagerBase throwManager in
                     GetComponentsInChildren<global::Invector.Throw.vThrowManagerBase>(true))
            {
                if (throwManager != null)
                    Destroy(throwManager.gameObject);
            }

            DestroyIfPresent<PioneerInvectorBootstrap>();
            DestroyIfPresent<PioneerInvectorInputBridge>();
            DestroyIfPresent<PioneerInvectorWeaponBridge>();
            DestroyIfPresent<PioneerInvectorDamageBridge>();
            DestroyIfPresent<PioneerInvectorAmmoBridge>();
            DestroyIfPresent<PioneerPlayerInputBinder>();
            DestroyIfPresent<PioneerShooterMeleeInput>();
            DestroyIfPresent<vShooterMeleeInput>();
            DestroyIfPresent<PlayerInput>();
            DestroyIfPresent<PlayerController>();
            DestroyIfPresent<InventorySystem>();
            DestroyIfPresent<EquipmentController>();
            DestroyIfPresent<WeaponAmmoState>();
            DestroyIfPresent<RangedCombatHud>();
            DestroyIfPresent<EquippedItemVisual>();
            DestroyIfPresent<MeleeCombatController>();
            DestroyIfPresent<RangedCombatController>();
            DestroyIfPresent<CombatFocusController>();
            DestroyIfPresent<vLockOnShooter>();
            DestroyIfPresent<vFootStep>();
            DestroyFootstepTriggers();
        }

        private void DestroyFootstepTriggers()
        {
            vFootStepTrigger[] triggers = GetComponentsInChildren<vFootStepTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null)
                    Destroy(triggers[i]);
            }
        }

        /// <summary>
        /// vSnapToBody.Start() resolves its snap control via transform.root, which is wrong for
        /// companions spawned under the roster bridge host (it can grab another character's
        /// skeleton, leaving weapon slot containers at the prefab root — weapons at feet).
        /// Reparent the containers onto THIS companion's bones and remove the components.
        /// </summary>
        private void SnapBodyContainersToLocalBones()
        {
            vBodySnappingControl bodySnap = GetComponentInChildren<vBodySnappingControl>(true);
            vSnapToBody[] snaps = GetComponentsInChildren<vSnapToBody>(true);

            for (int i = 0; i < snaps.Length; i++)
            {
                vSnapToBody snap = snaps[i];
                if (snap == null)
                    continue;

                // Prefer live Animator bones — serialized boneToSnap may still point at stock VBOT_.
                Transform bone = null;
                if (bodySnap != null && snap.boneName != vSnapToBody.manuallyAssignBone)
                    bone = bodySnap.GetBone(snap.boneName);
                if (bone == null)
                    bone = snap.boneToSnap;

                if (bone != null)
                    snap.transform.SetParent(bone, true);

                // Destroy so vSnapToBody.Start() can't re-resolve against the wrong root.
                Destroy(snap);
            }
        }

        private void DisableInvectorStandaloneUi()
        {
            vCollectMeleeControl collectControl = GetComponent<vCollectMeleeControl>();
            if (collectControl != null)
            {
                collectControl.controlDisplayPrefab = null;
                collectControl.enabled = false;
            }

            vControlDisplayWeaponStandalone[] displays =
                FindObjectsByType<vControlDisplayWeaponStandalone>(FindObjectsInactive.Include);
            for (int i = 0; i < displays.Length; i++)
            {
                if (displays[i] != null)
                    Destroy(displays[i].gameObject);
            }
        }

        private void DisableInvectorHealthDeath()
        {
            if (ThirdPersonController is vHealthController healthController)
            {
                healthController.isDead = false;
                healthController.ResetHealth();
                healthController.isImmortal = true;
            }
        }

        private void OnDestroy()
        {
            if (ShooterManager != null)
                ShooterManager.onEquipWeapon.RemoveListener(HandleShooterWeaponEquipped);
        }

        private void HandleShooterWeaponEquipped(vShooterWeapon weapon, bool isLeftWeapon)
        {
            if (weapon == null || ShooterManager == null)
                return;

            weapon.hitLayer = ShooterManager.damageLayer;
        }

        private void DestroyIfPresent<T>() where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    Destroy(components[i]);
            }
        }
    }
}
