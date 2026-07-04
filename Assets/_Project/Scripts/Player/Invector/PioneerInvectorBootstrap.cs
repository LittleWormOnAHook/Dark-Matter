using System.Collections;
using System.Reflection;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Audio;
using Project.Core;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using ECM2;

namespace Project.Player.Invector
{
    /// <summary>
    /// Marks and wires the Invector-based player. Strips legacy ECM2/GKC combat without breaking Invector init.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class PioneerInvectorBootstrap : MonoBehaviour
    {
        public static PioneerInvectorBootstrap Instance { get; private set; }

        [SerializeField] private bool disableLegacyCombatComponents = true;

        public vThirdPersonController ThirdPersonController { get; private set; }
        public PioneerShooterMeleeInput ShooterInput { get; private set; }
        public vShooterManager ShooterManager { get; private set; }
        public vMeleeManager MeleeManager { get; private set; }

        public bool IsActive => isActiveAndEnabled && ThirdPersonController != null;

        private bool _physicsInitialized;
        private int _kinematicGuardFrames;

        public static bool IsInvectorPlayer(Component component)
        {
            if (component == null)
                return false;

            return component.TryGetComponent(out PioneerInvectorBootstrap bootstrap) && bootstrap.IsActive;
        }

        private void Awake()
        {
            Instance = this;
            ThirdPersonController = GetComponent<vThirdPersonController>();
            ShooterInput = GetComponent<PioneerShooterMeleeInput>();
            ShooterManager = GetComponent<vShooterManager>();
            MeleeManager = GetComponent<vMeleeManager>();

            if (ShooterManager != null)
                ShooterManager.useAmmoDisplay = false;

            DisableInvectorStandaloneUi();
            EnsureNullAimCanvasStub();

            if (GetComponent<PioneerPlayerInputBinder>() == null)
                gameObject.AddComponent<PioneerPlayerInputBinder>();

            ResetInvectorHealthState();

            if (!disableLegacyCombatComponents)
                return;

            StripLegacyEcm2Motor();
            _kinematicGuardFrames = 8;
            DisableIfPresent<PlayerGkcAnimatorDriver>();
            DisableIfPresent<EquippedItemVisual>();
            DisableIfPresent<vLockOnShooter>();
            DisableIfPresent<CombatFocusController>();
            DisableIfPresent<FootstepController>();
            DisableIfPresent<LandingAudioController>();
        }

        private void Start()
        {
            StartCoroutine(InitializeAfterInvector());
        }

        private void OnEnable()
        {
            GameSession.GameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= HandleGameStarted;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void HandleGameStarted()
        {
            if (!_physicsInitialized)
                StartCoroutine(InitializeAfterInvector());
            else
                EnsureInvectorPhysicsReady();
        }

        private IEnumerator InitializeAfterInvector()
        {
            // Wait for vThirdPersonInput.Start -> Init() to cache rigidbody/capsule/animator refs.
            yield return null;
            yield return new WaitForFixedUpdate();

            StripLegacyEcm2Motor();
            EnsureInvectorPhysicsReady();
            _physicsInitialized = true;
        }

        public void EnsureInvectorPhysicsReady()
        {
            if (ThirdPersonController != null &&
                (ThirdPersonController.isDead || ThirdPersonController.ragdolled))
                return;

            Rigidbody body = ThirdPersonController != null
                ? ThirdPersonController.GetComponent<Rigidbody>()
                : GetComponent<Rigidbody>();

            CapsuleCollider capsule = ThirdPersonController != null
                ? ThirdPersonController.GetComponent<CapsuleCollider>()
                : GetComponent<CapsuleCollider>();

            if (body != null && (ThirdPersonController == null ||
                                 (!ThirdPersonController.isDead && !ThirdPersonController.ragdolled)))
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if (capsule != null)
            {
                capsule.enabled = true;
                capsule.isTrigger = false;
            }

            Animator animator = ThirdPersonController != null ? ThirdPersonController.animator : GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.updateMode = AnimatorUpdateMode.Fixed;
                if (ThirdPersonController != null && !ThirdPersonController.useRootMotion)
                    animator.applyRootMotion = false;
            }

            if (ThirdPersonController != null &&
                !ThirdPersonController.isDead &&
                !ThirdPersonController.ragdolled &&
                body != null &&
                capsule != null)
            {
                ThirdPersonController.EnableGravityAndCollision();
            }

            if (ShooterInput != null)
                ShooterInput.EnableOnAnimatorMove();
        }

        private void FixedUpdate()
        {
            if (_kinematicGuardFrames <= 0 || ThirdPersonController == null)
                return;

            if (ThirdPersonController.isDead || ThirdPersonController.ragdolled)
                return;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null && body.isKinematic)
            {
                body.isKinematic = false;
                body.useGravity = true;
            }

            _kinematicGuardFrames--;
        }

        private void DisableIfPresent<T>() where T : Behaviour
        {
            T behaviour = GetComponent<T>();
            if (behaviour != null)
                behaviour.enabled = false;
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

        private void EnsureNullAimCanvasStub()
        {
            if (ShooterInput == null)
                return;

            PioneerInvectorNullAimCanvas stub = GetComponent<PioneerInvectorNullAimCanvas>();
            if (stub == null)
                stub = gameObject.AddComponent<PioneerInvectorNullAimCanvas>();

            FieldInfo field = typeof(vShooterMeleeInput).GetField(
                "_controlAimCanvas",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(ShooterInput, stub);
        }

        private void StripLegacyEcm2Motor()
        {
            DisableIfPresent<FootstepController>();
            DisableIfPresent<LandingAudioController>();
            DisableIfPresent<CombatFocusController>();

            CharacterMovement movement = GetComponent<CharacterMovement>();
            if (movement != null)
                movement.enabled = false;

            Character character = GetComponent<Character>();
            if (character != null)
                character.enabled = false;

            if (character != null)
                Destroy(character);

            if (movement != null)
                Destroy(movement);
        }

        private void ResetInvectorHealthState()
        {
            if (ThirdPersonController == null)
                return;

            if (ThirdPersonController is vHealthController healthController)
            {
                healthController.isDead = false;
                healthController.ResetHealth();
            }
        }
    }
}
