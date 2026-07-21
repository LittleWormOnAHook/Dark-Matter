using ECM2;
using Project.Core;
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

            CleanupDeathState();
            ResolveInvectorDeathRagdoll()?.ResetForRespawn();

            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            if (character != null)
                character.SetMovementDirection(Vector3.zero);

            survivalStats.ResetStats();
            survivalStats.SetSimulationPaused(false);
            survivalStats.NotifyRevivedAfterRespawn(5f);
            GetComponent<PioneerInvectorSurvivalBridge>()?.PushHealthToInvector();

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
