using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Built door. Press E while build mode is off to swing it 90 degrees on the hinge edge.
    /// Kit phase 3 (0926): double doors and 8 m gates carry hidden "Leaf" children pivoted on their hinges.
    /// The first swing swaps the solid body for the leaves; both leaves then swing away from the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMBuildingDoor : MonoBehaviour, IWorldUsable
    {
        public const string LeafPrefix = "Leaf";

        DMBuildingGhost ghost;
        bool open;
        bool swinging;
        Quaternion closedRotation;
        Vector3 closedPosition;
        bool poseCaptured;
        Collider[] doorColliders;
        bool[] colliderEnabledSnapshot;
        bool collidersDisabledForSwing;

        Transform[] leaves;
        bool leavesChecked;
        bool leavesLive;
        Bounds closedBounds;
        float leafSign = 1f;

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

            return 120f - DistanceTo(context.PlayerPosition);
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!CanSwing(context.PlayerPosition) || swinging)
                return false;

            CaptureClosedPose();
            open = !open;
            StopAllCoroutines();
            RestoreCollidersAfterSwing();
            if (HasLeaves())
            {
                SplitLeaves();
                if (open)
                    leafSign = Vector3.Dot(context.PlayerPosition - closedBounds.center, transform.forward) >= 0f ? 1f : -1f;
                StartCoroutine(SwingLeaves(open));
            }
            else
            {
                StartCoroutine(Swing(open));
            }

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

            return DistanceTo(playerPosition) <= DMBuildingGhostProfile.DoorInteractRangeMeters;
        }

        /// <summary>Single doors measure from their pivot; double doors and gates from the nearest point of the closed door.</summary>
        float DistanceTo(Vector3 playerPosition)
        {
            if (!HasLeaves())
                return Vector3.Distance(playerPosition, transform.position);

            Bounds bounds = ClosedBounds();
            return Vector3.Distance(playerPosition, bounds.ClosestPoint(playerPosition));
        }

        bool HasLeaves()
        {
            if (!leavesChecked)
            {
                leavesChecked = true;
                var found = new List<Transform>();
                Transform[] all = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != transform && all[i].name.StartsWith(LeafPrefix))
                        found.Add(all[i]);
                }

                leaves = found.ToArray();
            }

            return leaves != null && leaves.Length > 0;
        }

        Renderer BodyRenderer()
        {
            if (!HasLeaves() || leaves[0] == null || leaves[0].parent == null)
                return null;
            return leaves[0].parent.GetComponent<Renderer>();
        }

        Bounds ClosedBounds()
        {
            if (leavesLive)
                return closedBounds;

            Renderer body = BodyRenderer();
            return body != null ? body.bounds : new Bounds(transform.position, Vector3.one);
        }

        void SplitLeaves()
        {
            if (leavesLive)
                return;

            Renderer body = BodyRenderer();
            closedBounds = body != null ? body.bounds : new Bounds(transform.position, Vector3.one);
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] == null)
                    continue;

                Renderer leafRenderer = leaves[i].GetComponent<Renderer>();
                if (leafRenderer != null && body != null)
                    leafRenderer.sharedMaterials = body.sharedMaterials;
                leaves[i].localRotation = Quaternion.identity;
                leaves[i].gameObject.SetActive(true);
            }

            if (body != null)
            {
                body.enabled = false;
                Collider[] bodyColliders = body.GetComponents<Collider>();
                for (int i = 0; i < bodyColliders.Length; i++)
                    bodyColliders[i].enabled = false;
            }

            leavesLive = true;
            CacheColliders();
        }

        void CaptureClosedPose()
        {
            if (poseCaptured)
                return;

            closedPosition = transform.position;
            closedRotation = transform.rotation;
            poseCaptured = true;
        }

        IEnumerator SwingLeaves(bool toOpen)
        {
            swinging = true;
            DisableCollidersForSwing();

            bool gate = ghost != null && DMBuildingCatalog.ShapeOf(ghost.PieceId) == DMBuildingShape.Gate;
            float swingDegrees = gate ? DMBuildingGhostProfile.GateSwingDegrees : DMBuildingGhostProfile.DoorSwingDegrees;
            float swingSeconds = gate ? DMBuildingGhostProfile.GateSwingSeconds : DMBuildingGhostProfile.DoorSwingSeconds;
            var from = new Quaternion[leaves.Length];
            var to = new Quaternion[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] == null)
                    continue;

                from[i] = leaves[i].localRotation;
                // A hinge on the left (-x) opens with +yaw, a hinge on the right with -yaw; leafSign flips both away from the player.
                float side = leaves[i].localPosition.x < 0f ? 1f : -1f;
                to[i] = toOpen ? Quaternion.Euler(0f, side * leafSign * swingDegrees, 0f) : Quaternion.identity;
            }

            float elapsed = 0f;
            while (elapsed < swingSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / swingSeconds));
                for (int i = 0; i < leaves.Length; i++)
                {
                    if (leaves[i] != null)
                        leaves[i].localRotation = Quaternion.Slerp(from[i], to[i], t);
                }

                yield return null;
            }

            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] != null)
                    leaves[i].localRotation = to[i];
            }

            RestoreCollidersAfterSwing();
            swinging = false;
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

                float distance = door.DistanceTo(playerPosition);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = door;
            }

            return best;
        }
    }
}
