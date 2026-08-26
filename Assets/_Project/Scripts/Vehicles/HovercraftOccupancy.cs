using System.Collections.Generic;
using ECM2;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Companions;
using Project.Inventory;
using Project.Player;
using Project.UI;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Vehicles
{
    /// <summary>
    /// Handles boarding: hide player/trio, disable motors, restore on exit.
    /// The Player_v7 root rides with the craft so TLM unloads the mount tiles.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftOccupancy : MonoBehaviour
    {
        [SerializeField] private Transform enterPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private Transform hiddenCrewHolder;

        private readonly List<GameObject> _deactivatedChildren = new List<GameObject>(8);
        private readonly List<Behaviour> _disabledBehaviours = new List<Behaviour>(16);
        private readonly List<Collider> _disabledColliders = new List<Collider>(16);

        private PlayerController _mountedPlayer;
        private Transform _mountedRoot;
        private Rigidbody _mountedBody;
        private bool _mountedBodyWasKinematic;
        private bool _mountedBodyDetectCollisions;
        private CompanionRosterBridge _companionBridge;
        private bool _isOccupied;
        private Vector3 _savedPlayerWorldScale = Vector3.one;

        public bool IsOccupied => _isOccupied;
        public Transform EnterPoint => enterPoint;
        public Transform ExitPoint => exitPoint;

        public void Configure(Transform enter, Transform exit, Transform crewHolder)
        {
            enterPoint = enter;
            exitPoint = exit;
            hiddenCrewHolder = crewHolder;
        }

        public bool TryEnter(PlayerController player)
        {
            if (_isOccupied || player == null)
                return false;

            // Holster drawn weapons so boarding doesn't leave an armed pose under the craft.
            if (player.TryGetComponent(out EquipmentController equipment))
                equipment.HolsterWeapon();

            _mountedPlayer = player;
            _isOccupied = true;

            HidePlayer(player);
            HideCompanions(player);
            PlayerVehicleState.RegisterMount(GetComponent<HovercraftController>(), player);
            player.ApplyCursorState();
            GameplayInputRecovery.QueueCursorRestore();
            return true;
        }

        public bool TryExit(PlayerController player)
        {
            if (!_isOccupied || player == null || _mountedPlayer != player)
                return false;

            Vector3 exitPosition = exitPoint != null ? exitPoint.position : transform.position + transform.right * 2f;
            Quaternion exitRotation = exitPoint != null ? exitPoint.rotation : transform.rotation;

            RestorePlayer(player, exitPosition, exitRotation);
            RestoreCompanions();
            PlayerVehicleState.ClearMount(GetComponent<HovercraftController>(), player);

            _mountedPlayer = null;
            _isOccupied = false;
            return true;
        }

        private void LateUpdate()
        {
            if (!_isOccupied || _mountedRoot == null)
                return;

            Transform holder = ResolveHolder();
            if (_mountedRoot.parent != holder)
                _mountedRoot.SetParent(holder, false);

            _mountedRoot.localPosition = Vector3.zero;
            _mountedRoot.localRotation = Quaternion.identity;
        }

        private void HidePlayer(PlayerController player)
        {
            if (player.TryGetComponent(out PioneerInvectorWeaponBridge weaponBridge))
                weaponBridge.PrepareForVehicleBoarding();

            Transform holder = ResolveHolder();
            _mountedRoot = ResolveRideRoot(player);
            _savedPlayerWorldScale = _mountedRoot.lossyScale;
            _mountedRoot.SetParent(holder, false);
            _mountedRoot.localPosition = Vector3.zero;
            _mountedRoot.localRotation = Quaternion.identity;
            _mountedRoot.localScale = Vector3.one;

            _mountedBody = _mountedRoot.GetComponent<Rigidbody>();
            if (_mountedBody == null)
                _mountedBody = player.GetComponent<Rigidbody>();
            if (_mountedBody != null)
            {
                _mountedBodyWasKinematic = _mountedBody.isKinematic;
                _mountedBodyDetectCollisions = _mountedBody.detectCollisions;
                _mountedBody.linearVelocity = Vector3.zero;
                _mountedBody.angularVelocity = Vector3.zero;
                _mountedBody.isKinematic = true;
                _mountedBody.detectCollisions = false;
            }

            _deactivatedChildren.Clear();
            for (int i = 0; i < player.transform.childCount; i++)
            {
                Transform child = player.transform.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;

                _deactivatedChildren.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }

            _disabledColliders.Clear();
            player.GetComponentsInChildren(true, _disabledColliders);
            for (int i = _disabledColliders.Count - 1; i >= 0; i--)
            {
                Collider collider = _disabledColliders[i];
                if (collider == null || !collider.enabled)
                {
                    _disabledColliders.RemoveAt(i);
                    continue;
                }

                collider.enabled = false;
            }

            _disabledBehaviours.Clear();
            DisableBehaviour(player.GetComponent<Animator>());
            DisableBehaviour(player.GetComponent<vThirdPersonController>());
            DisableBehaviour(player.GetComponent<Character>());
            DisableBehaviour(player.GetComponent<PioneerShooterMeleeInput>());
            DisableBehaviour(player.GetComponent<vHeadTrack>());
            DisableBehaviour(player.GetComponent<vRagdoll>());
            PioneerTerrainRescue[] rescues = _mountedRoot.GetComponentsInChildren<PioneerTerrainRescue>(true);
            for (int r = 0; r < rescues.Length; r++)
                DisableBehaviour(rescues[r]);
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
                    _deactivatedChildren.Add(cameraObject);
                    cameraObject.SetActive(false);
                }
            }
        }

        private void DisableBehaviour(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled)
                return;

            behaviour.enabled = false;
            _disabledBehaviours.Add(behaviour);
        }

        private void RestorePlayer(PlayerController player, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (_mountedBody != null)
            {
                _mountedBody.isKinematic = _mountedBodyWasKinematic;
                _mountedBody.detectCollisions = _mountedBodyDetectCollisions;
                _mountedBody.linearVelocity = Vector3.zero;
                _mountedBody.angularVelocity = Vector3.zero;
            }

            Transform root = _mountedRoot != null ? _mountedRoot : player.transform;
            root.SetParent(null, true);
            root.SetPositionAndRotation(worldPosition, worldRotation);
            root.localScale = _savedPlayerWorldScale;

            for (int i = 0; i < _deactivatedChildren.Count; i++)
            {
                if (_deactivatedChildren[i] != null)
                    _deactivatedChildren[i].SetActive(true);
            }

            for (int i = 0; i < _disabledColliders.Count; i++)
            {
                if (_disabledColliders[i] != null)
                    _disabledColliders[i].enabled = true;
            }

            for (int i = 0; i < _disabledBehaviours.Count; i++)
            {
                if (_disabledBehaviours[i] != null)
                    _disabledBehaviours[i].enabled = true;
            }

            if (player.TryGetComponent(out Rigidbody body) && body != _mountedBody)
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

            player.EnsureGameplayInputReady();
            GameplayInputRecovery.QueueCursorRestore();

            _deactivatedChildren.Clear();
            _disabledColliders.Clear();
            _disabledBehaviours.Clear();
            _mountedRoot = null;
            _mountedBody = null;
        }

        private Transform ResolveHolder()
        {
            if (hiddenCrewHolder != null)
                return hiddenCrewHolder;

            Transform existing = transform.Find("HiddenCrewHolder");
            if (existing != null)
            {
                hiddenCrewHolder = existing;
                return hiddenCrewHolder;
            }

            GameObject go = new GameObject("HiddenCrewHolder");
            hiddenCrewHolder = go.transform;
            hiddenCrewHolder.SetParent(transform, false);
            hiddenCrewHolder.localPosition = new Vector3(0f, 0.8f, 0f);
            return hiddenCrewHolder;
        }

        private static Transform ResolveRideRoot(PlayerController player)
        {
            GameObject named = GameObject.Find("Player_v7");
            if (named != null)
                return named.transform;

            Transform t = player.transform;
            if (t.root != null && t.root.CompareTag("Player"))
                return t.root;

            return t;
        }

        private void HideCompanions(PlayerController player)
        {
            _companionBridge = player.GetComponent<CompanionRosterBridge>();
            if (_companionBridge == null)
                _companionBridge = FindAnyObjectByType<CompanionRosterBridge>();

            _companionBridge?.SetCompanionsHiddenForVehicle(true);
        }

        private void RestoreCompanions()
        {
            _companionBridge?.SetCompanionsHiddenForVehicle(false);
            _companionBridge = null;
        }
    }
}
