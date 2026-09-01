using System.Collections;
using Invector.vCharacterController;
using ECM2;
using Project.Core;
using Project.Features.Climb;
using Project.Interaction;
using Project.Player.Invector;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SurvivalStats))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [Tooltip("Reserved for automatic respawn. Retry on the death popup respawns immediately.")]
        public float respawnTime;

        private SurvivalStats survivalStats;
        private PlayerController playerController;
        private Character character;
        private PioneerInvectorDeathRagdoll invectorDeathRagdoll;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool spawnCaptured;
        private Coroutine deathMenuRoutine;

        private const float MenuAfterRagdollDownSeconds = 2f;
        private const float MenuSafetyTimeoutSeconds = 5f;
        private const float AssumeDownIfNoRagdollSeconds = 0.8f;
        private const float RagdollDownSpeed = 1.25f;

        private void Awake()
        {
            survivalStats = GetComponent<SurvivalStats>();
            playerController = GetComponent<PlayerController>();
            character = GetComponent<Character>();
            invectorDeathRagdoll = GetComponent<PioneerInvectorDeathRagdoll>();
            CaptureSpawnPoint();
        }

        private void OnEnable()
        {
            if (survivalStats != null)
                survivalStats.PlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (survivalStats != null)
                survivalStats.PlayerDied -= HandlePlayerDied;
        }

        public void Respawn()
        {
            if (survivalStats == null)
                return;

            StopDeathMenuRoutine();
            CleanupDeathState();

            // Clear lethal/climb leftover BEFORE physics restore so landing
            // cannot re-arm ragdoll on the same body.
            GameObject player = gameObject.name == "Player_v7" ? gameObject : GameObject.Find("Player_v7");
            if (player == null)
                player = gameObject;
            player.GetComponent<DMLandingDirector>()?.ResetForRespawn();
            player.GetComponent<DMClimbController>()?.RestoreAfterDeathOrRetry();

            survivalStats.ResetStats();
            survivalStats.SetSimulationPaused(false);
            survivalStats.NotifyRevivedAfterRespawn(5f);
            GetComponent<PioneerInvectorSurvivalBridge>()?.PushHealthToInvector();

            ResolveInvectorDeathRagdoll()?.ResetForRespawn();

            Quaternion upright = spawnRotation;
            Vector3 fwd = spawnRotation * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.001f)
                upright = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            transform.SetPositionAndRotation(spawnPosition, upright);
            if (character != null)
                character.SetMovementDirection(Vector3.zero);

            ResetPlayerSystems();
            GameplayAudioUtility.EnsureListenerOnCamera(playerController != null ? playerController.GameplayCamera : null);

            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
            {
                ui.HideDeathPopup();
                ui.RefreshSurvivalDisplay();
            }
        }

        private void HandlePlayerDied()
        {
            CleanupDeathState();
            survivalStats?.SetSimulationPaused(true);
            GetComponent<PioneerInvectorSurvivalBridge>()?.PushHealthToInvector();
            ResolveInvectorDeathRagdoll()?.ActivateDeathRagdoll();

            StopDeathMenuRoutine();
            deathMenuRoutine = StartCoroutine(ShowDeathMenuAfterRagdoll());
        }

        private void StopDeathMenuRoutine()
        {
            if (deathMenuRoutine == null)
                return;
            StopCoroutine(deathMenuRoutine);
            deathMenuRoutine = null;
        }

        private IEnumerator ShowDeathMenuAfterRagdoll()
        {
            float startedAt = Time.unscaledTime;
            float downAt = -1f;
            ResolveInvectorBody(out vThirdPersonController controller, out vRagdoll ragdoll, out Animator animator);

            while (true)
            {
                float now = Time.unscaledTime;
                bool ragdollStarted = (ragdoll != null && ragdoll.isActive)
                    || (controller != null && controller.ragdolled);
                if (IsRagdollBodyDown(controller, ragdoll, animator) && downAt < 0f)
                    downAt = now;
                else if (downAt < 0f && !ragdollStarted && now - startedAt >= AssumeDownIfNoRagdollSeconds)
                    downAt = now;

                bool ready = downAt >= 0f && now - downAt >= MenuAfterRagdollDownSeconds;
                bool timeout = now - startedAt >= MenuSafetyTimeoutSeconds;
                if (ready || timeout)
                    break;

                yield return null;
            }

            deathMenuRoutine = null;
            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
                ui.ShowDeathPopup();
        }

        private static bool IsRagdollBodyDown(
            vThirdPersonController controller,
            vRagdoll ragdoll,
            Animator animator)
        {
            bool ragdollOn = (ragdoll != null && ragdoll.isActive)
                || (controller != null && controller.ragdolled);
            if (!ragdollOn)
                return false;

            Transform hips = null;
            if (animator != null && animator.isHuman)
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null && ragdoll != null)
                hips = ragdoll.characterHips;
            if (hips == null)
                return ragdoll != null && !ragdoll.inStabilize;

            Rigidbody hipBody = hips.GetComponent<Rigidbody>();
            if (hipBody != null)
                return hipBody.linearVelocity.magnitude <= RagdollDownSpeed;

            return ragdoll == null || !ragdoll.inStabilize;
        }

        private void ResolveInvectorBody(
            out vThirdPersonController controller,
            out vRagdoll ragdoll,
            out Animator animator)
        {
            controller = GetComponent<vThirdPersonController>();
            ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            animator = GetComponentInChildren<Animator>();
            if (controller != null && ragdoll != null && animator != null)
                return;

            GameObject player = gameObject.name == "Player_v7" ? gameObject : GameObject.Find("Player_v7");
            if (player == null || player == gameObject)
                return;

            if (controller == null)
                controller = player.GetComponent<vThirdPersonController>();
            if (ragdoll == null)
                ragdoll = player.GetComponent<vRagdoll>() ?? player.GetComponentInChildren<vRagdoll>(true);
            if (animator == null)
                animator = player.GetComponentInChildren<Animator>();
        }

        private PioneerInvectorDeathRagdoll ResolveInvectorDeathRagdoll()
        {
            if (invectorDeathRagdoll == null)
                invectorDeathRagdoll = GetComponent<PioneerInvectorDeathRagdoll>();

            if (invectorDeathRagdoll != null || !PioneerInvectorBootstrap.IsInvectorPlayer(this))
                return invectorDeathRagdoll;

            PioneerInvectorBootstrap bootstrap = GetComponent<PioneerInvectorBootstrap>();
            if (bootstrap == null)
                bootstrap = gameObject.AddComponent<PioneerInvectorBootstrap>();

            invectorDeathRagdoll = GetComponent<PioneerInvectorDeathRagdoll>();
            return invectorDeathRagdoll;
        }

        private void CleanupDeathState()
        {
            OpticsController optics = GetComponent<OpticsController>();
            optics?.CloseOpticsIfActive();

            OpticsCameraRig cameraRig = GetComponent<OpticsCameraRig>();
            if (cameraRig != null)
            {
                cameraRig.Deactivate();
                cameraRig.ForceRestoreMainCamera();
            }

            MeleeCombatController melee = GetComponent<MeleeCombatController>();
            if (melee != null)
            {
                melee.enabled = false;
                melee.enabled = true;
            }

            if (playerController != null)
            {
                playerController.SetOpticsOpen(false);
                playerController.SetGameplayPaused(false);
            }

            GameplayAudioUtility.EnsureListenerOnCamera(
                playerController != null ? playerController.GameplayCamera : Camera.main);
        }

        private void ResetPlayerSystems()
        {
            if (playerController == null)
                return;

            playerController.SetGameplayPaused(false);
            playerController.SetInventoryOpen(false);
            playerController.SetJournalOpen(false);
            playerController.SetMapOpen(false);
            playerController.SetQuestDialogOpen(false);
            playerController.SetOpticsOpen(false);
            playerController.RefreshCameraFollow();
        }

        private void CaptureSpawnPoint()
        {
            if (spawnCaptured)
                return;

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnCaptured = true;
        }
    }
}
