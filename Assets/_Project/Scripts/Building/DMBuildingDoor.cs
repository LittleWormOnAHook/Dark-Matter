using System.Collections;
using Project.Core;
using Project.Interaction;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Built door. Press E while build mode is off to swing it 90 degrees on the hinge edge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMBuildingDoor : MonoBehaviour, IWorldUsable
    {
        DMBuildingGhost ghost;
        bool open;
        bool swinging;
        Quaternion closedRotation;
        Vector3 closedPosition;
        bool poseCaptured;
        Collider[] doorColliders;
        bool[] colliderEnabledSnapshot;
        bool collidersDisabledForSwing;

        void Awake()
        {
            ghost = GetComponent<DMBuildingGhost>();
            CacheColliders();
        }

        void CacheColliders()
        {
            doorColliders = GetComponentsInChildren<Collider>(true);
            colliderEnabledSnapshot = new bool[doorColliders.Length];
        }

        void DisableCollidersForSwing()
        {
            if (doorColliders == null || doorColliders.Length == 0)
                CacheColliders();

            collidersDisabledForSwing = false;
            for (int i = 0; i < doorColliders.Length; i++)
            {
                Collider collider = doorColliders[i];
                if (collider == null)
                    continue;

                colliderEnabledSnapshot[i] = collider.enabled;
                collider.enabled = false;
                collidersDisabledForSwing = true;
            }
        }

        void RestoreCollidersAfterSwing()
        {
            if (!collidersDisabledForSwing || doorColliders == null)
                return;

            for (int i = 0; i < doorColliders.Length; i++)
            {
                Collider collider = doorColliders[i];
                if (collider == null)
                    continue;

                collider.enabled = colliderEnabledSnapshot[i];
            }

            collidersDisabledForSwing = false;
        }

        void OnEnable()
        {
            WorldUseController.Register(this);
        }

        void OnDisable()
        {
            RestoreCollidersAfterSwing();
            WorldUseController.Unregister(this);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!CanSwing(context.PlayerPosition))
                return -1f;

            return 120f - Vector3.Distance(context.PlayerPosition, transform.position);
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!CanSwing(context.PlayerPosition) || swinging)
                return false;

            CaptureClosedPose();
            open = !open;
            StopAllCoroutines();
            RestoreCollidersAfterSwing();
            StartCoroutine(Swing(open));
            return true;
        }

        public static string TryGetPrompt(WorldUseContext context)
        {
            if (DMBuildingMode.IsActive || context.PlayerTransform == null)
                return null;

            DMBuildingDoor door = FindInRange(context.PlayerPosition);
            if (door == null)
                return null;

            return door.open ? "Press E to close" : "Press E to open";
        }

        bool CanSwing(Vector3 playerPosition)
        {
            if (DMBuildingMode.IsActive || !GameSession.HasStarted)
                return false;
            if (ghost == null)
                ghost = GetComponent<DMBuildingGhost>();
            if (ghost == null || !ghost.Built)
                return false;

            return Vector3.Distance(playerPosition, transform.position) <= DMBuildingGhostProfile.DoorInteractRangeMeters;
        }

        void CaptureClosedPose()
        {
            if (poseCaptured)
                return;

            closedPosition = transform.position;
            closedRotation = transform.rotation;
            poseCaptured = true;
        }

        IEnumerator Swing(bool toOpen)
        {
            swinging = true;
            DisableCollidersForSwing();

            float swingDegrees = DMBuildingGhostProfile.DoorSwingDegrees;
            float swingSeconds = DMBuildingGhostProfile.DoorSwingSeconds;
            float elapsed = 0f;
            Quaternion fromRotation = transform.rotation;
            Vector3 fromPosition = transform.position;
            float angle = toOpen ? swingDegrees : 0f;
            Quaternion yaw = Quaternion.Euler(0f, angle, 0f);
            Vector3 hinge = closedPosition + closedRotation * new Vector3(-DMBuildingCatalog.DoorWidth * 0.5f, 0f, 0f);
            // Kit phase 2 (0926): a hatch lid hinges on its back edge and lifts upward.
            if (ghost != null && DMBuildingCatalog.ShapeOf(ghost.PieceId) == DMBuildingShape.HatchLid)
            {
                MeshFilter filter = GetComponentInChildren<MeshFilter>();
                float halfDepth = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.extents.z : 0.8f;
                yaw = Quaternion.AngleAxis(-angle, closedRotation * Vector3.right);
                hinge = closedPosition + closedRotation * new Vector3(0f, 0f, -halfDepth);
            }
            Quaternion targetRotation = yaw * closedRotation;
            Vector3 targetPosition = hinge + yaw * (closedPosition - hinge);

            while (elapsed < swingSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swingSeconds);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, targetPosition, t),
                    Quaternion.Slerp(fromRotation, targetRotation, t));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            RestoreCollidersAfterSwing();
            swinging = false;
        }

        static DMBuildingDoor FindInRange(Vector3 playerPosition)
        {
            DMBuildingDoor[] doors = FindObjectsByType<DMBuildingDoor>(FindObjectsInactive.Exclude);
            DMBuildingDoor best = null;
            float bestDistance = DMBuildingGhostProfile.DoorInteractRangeMeters;
            for (int i = 0; i < doors.Length; i++)
            {
                DMBuildingDoor door = doors[i];
                if (door == null || !door.CanSwing(playerPosition))
                    continue;

                float distance = Vector3.Distance(playerPosition, door.transform.position);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = door;
            }

            return best;
        }
    }
}
