using System.Collections.Generic;
using ECM2;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Companions;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Shelter
{
    /// <summary>
    /// Hides the player and expedition companions while they are inside a deployed Quora Shelter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuoraShelterOccupancy : MonoBehaviour
    {
        [SerializeField] private Transform hiddenCrewHolder;

        private readonly List<GameObject> deactivatedChildren = new List<GameObject>(8);
        private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>(16);
        private readonly List<Collider> disabledColliders = new List<Collider>(16);

        private PlayerController mountedPlayer;
        private CompanionRosterBridge companionBridge;
        private bool isOccupied;
        private Vector3 savedPlayerWorldScale = Vector3.one;

        public bool IsOccupied => isOccupied;

        public void Configure(Transform crewHolder)
        {
            hiddenCrewHolder = crewHolder;
        }

        public bool TryEnter(PlayerController player)
        {
            if (isOccupied || player == null)
                return false;

            mountedPlayer = player;
            isOccupied = true;
            HidePlayer(player);
            HideCompanions(player);
            return true;
        }

        public bool TryExit(PlayerController player, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (!isOccupied || player == null || mountedPlayer != player)
                return false;

            RestorePlayer(player, worldPosition, worldRotation);
            RestoreCompanions();
            mountedPlayer = null;
            isOccupied = false;
            return true;
        }

        private void HidePlayer(PlayerController player)
        {
            if (player.TryGetComponent(out PioneerInvectorWeaponBridge weaponBridge))
                weaponBridge.PrepareForVehicleBoarding();

            Transform holder = hiddenCrewHolder != null ? hiddenCrewHolder : transform;
            savedPlayerWorldScale = player.transform.lossyScale;
            player.transform.SetParent(holder, true);
            player.transform.localPosition = Vector3.zero;

            deactivatedChildren.Clear();
            for (int i = 0; i < player.transform.childCount; i++)
            {
                Transform child = player.transform.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;

                deactivatedChildren.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }

            disabledColliders.Clear();
            player.GetComponentsInChildren(true, disabledColliders);
            for (int i = disabledColliders.Count - 1; i >= 0; i--)
            {
                Collider collider = disabledColliders[i];
                if (collider == null || !collider.enabled)
                {
                    disabledColliders.RemoveAt(i);
                    continue;
                }

                collider.enabled = false;
            }

            disabledBehaviours.Clear();
            DisableBehaviour(player.GetComponent<Animator>());
            DisableBehaviour(player.GetComponent<vThirdPersonController>());
            DisableBehaviour(player.GetComponent<Character>());
            DisableBehaviour(player.GetComponent<PioneerShooterMeleeInput>());
            DisableBehaviour(player.GetComponent<vHeadTrack>());
            DisableBehaviour(player.GetComponent<vRagdoll>());
            DisableBehaviour(player.GetComponent<PioneerTerrainRescue>());
            DisableBehaviour(player.GetComponent<vShooterManager>());
            DisableBehaviour(player.GetComponent<vMeleeManager>());

            NavMeshAgent navAgent = player.GetComponent<NavMeshAgent>();
            DisableBehaviour(navAgent);

            if (player.TryGetComponent(out PioneerInvectorBootstrap bootstrap) &&
                bootstrap.ShooterInput != null &&
                bootstrap.ShooterInput.tpCamera != null)
            {
                GameObject cameraObject = bootstrap.ShooterInput.tpCamera.gameObject;
                if (cameraObject.activeSelf)
                {
                    deactivatedChildren.Add(cameraObject);
                    cameraObject.SetActive(false);
                }
            }
        }

        private void DisableBehaviour(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled)
                return;

            behaviour.enabled = false;
            disabledBehaviours.Add(behaviour);
        }

        private void RestorePlayer(PlayerController player, Vector3 worldPosition, Quaternion worldRotation)
        {
            player.transform.SetParent(null, true);
            player.transform.SetPositionAndRotation(worldPosition, worldRotation);
            player.transform.localScale = savedPlayerWorldScale;

            for (int i = 0; i < deactivatedChildren.Count; i++)
            {
                if (deactivatedChildren[i] != null)
                    deactivatedChildren[i].SetActive(true);
            }

            for (int i = 0; i < disabledColliders.Count; i++)
            {
                if (disabledColliders[i] != null)
                    disabledColliders[i].enabled = true;
            }

            for (int i = 0; i < disabledBehaviours.Count; i++)
            {
                if (disabledBehaviours[i] != null)
                    disabledBehaviours[i].enabled = true;
            }

            if (player.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (player.TryGetComponent(out Character character))
                character.Pause(false);

            if (player.TryGetComponent(out vThirdPersonController controller))
            {
                controller.input = Vector2.zero;
                controller.isSprinting = false;
            }

            if (player.TryGetComponent(out PioneerInvectorBootstrap bootstrap))
                bootstrap.EnsureInvectorPhysicsReady();

            if (player.TryGetComponent(out PioneerInvectorWeaponBridge weaponBridge))
                weaponBridge.ScheduleRestoreAfterVehicleExit();

            deactivatedChildren.Clear();
            disabledColliders.Clear();
            disabledBehaviours.Clear();
        }

        private void HideCompanions(PlayerController player)
        {
            companionBridge = player.GetComponent<CompanionRosterBridge>()
                ?? FindAnyObjectByType<CompanionRosterBridge>();
            companionBridge?.SetCompanionsHiddenForVehicle(true);
        }

        private void RestoreCompanions()
        {
            companionBridge?.SetCompanionsHiddenForVehicle(false);
            companionBridge = null;
        }
    }
}
