using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Shelter
{
    /// <summary>
    /// Shared deploy / store logic for the Quora temporary shelter inventory item.
    /// </summary>
    public static class QuoraShelterDeploymentUtility
    {
        public const float DeployDistanceMeters = 5f;

        private const float PlacementPadding = 0.05f;
        private const float MinPlayerClearance = 0.5f;
        private const float GroundContactClearance = 0.2f;
        private const float HorizontalFootprintScale = 0.92f;

        private static readonly float[] PlacementAngleOffsets =
        {
            0f, -15f, 15f, -30f, 30f, -45f, 45f, -60f, 60f, -75f, 75f, -90f, 90f, 120f, -120f, 150f, -150f, 180f
        };

        private static readonly float[] PlacementDistanceScales = { 1f, 0.92f, 1.08f, 0.85f, 1.15f };

        private static readonly Collider[] OverlapBuffer = new Collider[24];

        public static bool TryDeploy(InventorySystem inventory, ItemData shelterItem, Transform playerTransform, out string message)
        {
            message = string.Empty;

            if (inventory == null || shelterItem == null || playerTransform == null)
            {
                message = "Cannot deploy the shelter right now.";
                return false;
            }

            if (!shelterItem.IsDeployableShelter)
            {
                message = "This item cannot be deployed.";
                return false;
            }

            if (inventory.CountItem(shelterItem) <= 0)
            {
                message = "No Quora Shelter in inventory.";
                return false;
            }

            if (shelterItem.deployedPrefab == null)
            {
                message = "Shelter deploy prefab is not configured.";
                return false;
            }

            if (QuoraShelterController.FindAnyDeployed() != null)
            {
                message = "A Quora Shelter is already deployed.";
                return false;
            }

            if (!TryResolveDeployPlacement(
                    shelterItem.deployedPrefab,
                    playerTransform,
                    out Vector3 spawnPosition,
                    out Quaternion spawnRotation))
            {
                message = "Not enough clear space nearby to deploy the Quora Shelter.";
                return false;
            }

            GameObject instance = Object.Instantiate(shelterItem.deployedPrefab, spawnPosition, spawnRotation);
            QuoraShelterController controller = instance.GetComponent<QuoraShelterController>();
            if (controller == null)
                controller = instance.AddComponent<QuoraShelterController>();

            controller.InitializeDeployed(QuoraShelterStorageState.ConsumeStoredLifetimeOrDefault());

            if (!inventory.RemoveItem(shelterItem, 1))
            {
                Object.Destroy(instance);
                message = "Could not remove shelter from inventory.";
                return false;
            }

            message = "Quora Shelter deployed.";
            return true;
        }

        public static bool TryStore(QuoraShelterController shelter, InventorySystem inventory, ItemData shelterItem, out string message)
        {
            message = string.Empty;

            if (shelter == null)
            {
                message = "No shelter to store.";
                return false;
            }

            if (shelter.IsOccupied)
            {
                message = "Exit the shelter before storing it.";
                return false;
            }

            if (shelterItem == null || inventory == null)
            {
                message = "Shelter storage is not configured.";
                return false;
            }

            int added = inventory.AddItem(shelterItem, 1);
            if (added <= 0)
            {
                message = "Inventory is full — cannot store the shelter.";
                return false;
            }

            shelter.PauseLifetimeTimer();
            QuoraShelterStorageState.SetStoredLifetime(shelter.RemainingLifetimeSeconds);
            Object.Destroy(shelter.gameObject);
            message = "Quora Shelter stored in inventory.";
            return true;
        }

        private static bool TryResolveDeployPlacement(
            GameObject deployedPrefab,
            Transform playerTransform,
            out Vector3 spawnPosition,
            out Quaternion spawnRotation)
        {
            spawnPosition = default;
            spawnRotation = Quaternion.identity;

            if (deployedPrefab == null || playerTransform == null)
                return false;

            if (!TryGetPlacementBounds(deployedPrefab, out Vector3 localCenter, out Vector3 halfExtents))
                return false;

            Vector3 flatForward = GetFlatForward(playerTransform.forward);
            Vector3 playerPosition = playerTransform.position;

            for (int distanceIndex = 0; distanceIndex < PlacementDistanceScales.Length; distanceIndex++)
            {
                float distance = DeployDistanceMeters * PlacementDistanceScales[distanceIndex];

                for (int angleIndex = 0; angleIndex < PlacementAngleOffsets.Length; angleIndex++)
                {
                    Vector3 direction = RotateFlatDirection(flatForward, PlacementAngleOffsets[angleIndex]);
                    Vector3 candidate = playerPosition + direction * distance;

                    if (!TrySnapToGround(candidate, playerTransform, out float groundY))
                        continue;

                    candidate.y = groundY;

                    Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                    if (!IsPlacementClear(candidate, rotation, localCenter, halfExtents, playerTransform))
                        continue;

                    spawnPosition = candidate;
                    spawnRotation = rotation;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPlacementBounds(GameObject deployedPrefab, out Vector3 localCenter, out Vector3 halfExtents)
        {
            localCenter = Vector3.up * 0.5f;
            halfExtents = Vector3.one * 0.5f + Vector3.one * PlacementPadding;

            BoxCollider box = deployedPrefab.GetComponent<BoxCollider>();
            if (box == null)
                return true;

            Vector3 scale = deployedPrefab.transform.localScale;
            Vector3 absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            localCenter = Vector3.Scale(box.center, absScale);
            halfExtents = Vector3.Scale(box.size * 0.5f, absScale) + Vector3.one * PlacementPadding;
            return true;
        }

        private static bool IsPlacementClear(
            Vector3 rootPosition,
            Quaternion rootRotation,
            Vector3 localCenter,
            Vector3 halfExtents,
            Transform playerTransform)
        {
            BuildClearanceVolume(
                rootPosition,
                rootRotation,
                localCenter,
                halfExtents,
                out Vector3 overlapCenter,
                out Vector3 clearanceHalfExtents);

            int hitCount = Physics.OverlapBoxNonAlloc(
                overlapCenter,
                clearanceHalfExtents,
                OverlapBuffer,
                rootRotation,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                if (!IsBlockingCollider(OverlapBuffer[i], playerTransform, rootPosition.y))
                    continue;

                return false;
            }

            if (IsTooCloseToPlayer(rootPosition, playerTransform))
                return false;

            return true;
        }

        /// <summary>
        /// Overlap volume above ground contact so terrain / floor colliders are not treated as obstructions.
        /// </summary>
        private static void BuildClearanceVolume(
            Vector3 rootPosition,
            Quaternion rootRotation,
            Vector3 localCenter,
            Vector3 halfExtents,
            out Vector3 overlapCenter,
            out Vector3 clearanceHalfExtents)
        {
            float bodyHeight = halfExtents.y * 2f;
            float raisedBottom = GroundContactClearance;
            float clearanceHeight = Mathf.Max(0.5f, bodyHeight - raisedBottom);

            clearanceHalfExtents = new Vector3(
                halfExtents.x * HorizontalFootprintScale,
                clearanceHeight * 0.5f,
                halfExtents.z * HorizontalFootprintScale);

            Vector3 localRaisedCenter = localCenter;
            localRaisedCenter.y = raisedBottom + clearanceHeight * 0.5f;
            overlapCenter = rootPosition + rootRotation * localRaisedCenter;
        }

        private static bool TrySnapToGround(Vector3 worldPosition, Transform playerTransform, out float groundY)
        {
            groundY = worldPosition.y;

            float originY = worldPosition.y + 3f;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                originY = Mathf.Max(originY, terrainY + 4f);
            }

            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float rayLength = originY - (worldPosition.y - 8f);
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                Mathf.Max(4f, rayLength),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (IsIgnorableGroundCollider(hitCollider, playerTransform))
                    continue;

                if (hits[i].distance >= closestDistance)
                    continue;

                closestDistance = hits[i].distance;
                groundY = hits[i].point.y;
                foundGround = true;
            }

            if (foundGround)
                return true;

            if (terrain != null)
            {
                groundY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                return true;
            }

            return false;
        }

        private static bool IsBlockingCollider(Collider collider, Transform playerTransform, float groundY)
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
                return false;

            if (collider is TerrainCollider)
                return false;

            Transform hitTransform = collider.transform;
            if (playerTransform != null
                && (hitTransform == playerTransform || hitTransform.IsChildOf(playerTransform)))
            {
                return false;
            }

            if (collider.CompareTag("Player"))
                return false;

            if (IsIgnorableGroundCollider(collider, playerTransform))
                return false;

            // Flat walkable surfaces at or below the shelter footprint are support, not obstruction.
            if (collider.bounds.max.y <= groundY + GroundContactClearance + 0.05f)
                return false;

            return true;
        }

        private static bool IsIgnorableGroundCollider(Collider collider, Transform playerTransform)
        {
            if (collider == null || collider.isTrigger)
                return true;

            if (collider is TerrainCollider)
                return true;

            if (collider.CompareTag("Player"))
                return true;

            Transform hitTransform = collider.transform;
            if (playerTransform != null
                && (hitTransform == playerTransform || hitTransform.IsChildOf(playerTransform)))
            {
                return true;
            }

            int layer = collider.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Item")
                || layer == LayerMask.NameToLayer("Player")
                || layer == LayerMask.NameToLayer("Enemy")
                || layer == LayerMask.NameToLayer("CompanionAI")
                || layer == LayerMask.NameToLayer("Triggers")
                || layer == LayerMask.NameToLayer("BodyPart"))
            {
                return true;
            }

            return false;
        }

        private static bool IsTooCloseToPlayer(Vector3 rootPosition, Transform playerTransform)
        {
            if (playerTransform == null)
                return false;

            Vector3 toShelter = rootPosition - playerTransform.position;
            toShelter.y = 0f;
            return toShelter.sqrMagnitude < MinPlayerClearance * MinPlayerClearance;
        }

        private static Vector3 GetFlatForward(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return forward.normalized;
        }

        private static Vector3 RotateFlatDirection(Vector3 flatForward, float yawDegrees)
        {
            if (Mathf.Approximately(yawDegrees, 0f))
                return flatForward;

            return Quaternion.AngleAxis(yawDegrees, Vector3.up) * flatForward;
        }
    }
}
