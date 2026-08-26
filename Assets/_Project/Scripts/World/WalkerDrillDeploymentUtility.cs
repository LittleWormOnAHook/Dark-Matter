using Project.Data;
using Project.Interaction;
using Project.Inventory;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Shared "Store in Inventory" / "Deploy" logic for the Walker Drill.
    /// Mirrors hovercraft placement, but does not use ItemType.Vehicle (no refuel).
    /// </summary>
    public static class WalkerDrillDeploymentUtility
    {
        public const float DeployDistanceMeters = 5f;

        private const string ResourcesPrefabPath = "World/WalkerDrill";
        private const string ResourcesMoveClipPath = "World/WalkerDrill_Move";
        private const string ResourcesSpinClipPath = "World/WalkerDrill_Spin";

        public static bool TryStore(DMWalkerDrillController drill, InventorySystem inventory, ItemData walkerDrillItem, out string message)
        {
            message = string.Empty;

            if (drill == null)
            {
                message = "No Walker Drill to store.";
                return false;
            }

            if (drill.IsMining || drill.IsRetracting)
            {
                message = "Stop the drill before storing it.";
                return false;
            }

            if (walkerDrillItem == null || inventory == null)
            {
                message = "Walker Drill storage is not configured.";
                return false;
            }

            int added = inventory.AddItem(walkerDrillItem, 1);
            if (added <= 0)
            {
                message = "Inventory is full — cannot store the Walker Drill.";
                return false;
            }

            Object.Destroy(drill.gameObject);
            message = "Walker Drill stored in inventory.";
            return true;
        }

        public static bool TryDeploy(InventorySystem inventory, ItemData walkerDrillItem, Transform playerTransform, out string message)
        {
            message = string.Empty;

            if (inventory == null || walkerDrillItem == null || playerTransform == null)
            {
                message = "Cannot deploy the Walker Drill right now.";
                return false;
            }

            if (inventory.CountItem(walkerDrillItem) <= 0)
            {
                message = "No stored Walker Drill to deploy.";
                return false;
            }

            GameObject prefab = ResolveDeployPrefab(walkerDrillItem);
            if (prefab == null)
            {
                message = "Walker Drill prefab is not configured on the item.";
                return false;
            }

            Vector3 flatForward = GetFlatForward(playerTransform.forward);
            Vector3 spawnPosition = playerTransform.position + flatForward * DeployDistanceMeters;
            spawnPosition.y = SnapToGroundY(spawnPosition, playerTransform.position.y);
            Quaternion spawnRotation = Quaternion.LookRotation(flatForward, Vector3.up);

            GameObject instance = Object.Instantiate(prefab, spawnPosition, spawnRotation);
            instance.name = "Walker Drill";
            EnsureGameplayComponents(instance, walkerDrillItem);
            ReactivateDeployedDrill(instance);

            inventory.RemoveItem(walkerDrillItem, 1);
            message = "Walker Drill deployed.";
            return true;
        }

        /// <summary>
        /// Resolve a spawnable prefab even when ItemData.deployedPrefab is a broken YAML fileID.
        /// Order: deployedPrefab, worldPrefab, ItemRegistry copies, Resources/World/WalkerDrill.
        /// </summary>
        public static GameObject ResolveDeployPrefab(ItemData item)
        {
            GameObject prefab = FirstNonNullPrefab(item);
            if (prefab != null)
                return prefab;

            ItemData canonical = ItemRegistry.Resolve("Walker Drill");
            if (canonical != null && canonical != item)
            {
                prefab = FirstNonNullPrefab(canonical);
                if (prefab != null)
                    return prefab;
            }

            return Resources.Load<GameObject>(ResourcesPrefabPath);
        }

        private static GameObject FirstNonNullPrefab(ItemData item)
        {
            if (item == null)
                return null;
            if (item.deployedPrefab != null)
                return item.deployedPrefab;
            if (item.worldPrefab != null)
                return item.worldPrefab;
            return null;
        }

        public static void EnsureGameplayComponents(GameObject instance, ItemData walkerDrillItem)
        {
            if (instance == null)
                return;

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();

            TryAssignAnimatorController(animator);

            bool addedController = false;
            DMWalkerDrillController controller = instance.GetComponent<DMWalkerDrillController>();
            if (controller == null)
                controller = instance.GetComponentInChildren<DMWalkerDrillController>();
            if (controller == null)
            {
                controller = instance.AddComponent<DMWalkerDrillController>();
                addedController = true;
            }

            controller.Configure(animator, 2f);
            controller.EnsureAudioSource();
            if (addedController)
                TryAssignAudioClips(controller);

            DMWalkerDrillUsable usable = instance.GetComponent<DMWalkerDrillUsable>();
            if (usable == null)
                usable = instance.GetComponentInChildren<DMWalkerDrillUsable>();
            if (usable == null)
                usable = instance.AddComponent<DMWalkerDrillUsable>();

            usable.EnsureInteractionCollider();
            if (walkerDrillItem != null)
                usable.SetWalkerDrillItem(walkerDrillItem);
        }

        private static void TryAssignAnimatorController(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController != null)
                return;

            GameObject complete = Resources.Load<GameObject>(ResourcesPrefabPath);
            if (complete == null)
                return;

            Animator source = complete.GetComponent<Animator>();
            if (source == null)
                source = complete.GetComponentInChildren<Animator>();
            if (source != null && source.runtimeAnimatorController != null)
                animator.runtimeAnimatorController = source.runtimeAnimatorController;
        }

        private static void TryAssignAudioClips(DMWalkerDrillController controller)
        {
            if (controller == null)
                return;

            AudioClip move = Resources.Load<AudioClip>(ResourcesMoveClipPath);
            AudioClip spin = Resources.Load<AudioClip>(ResourcesSpinClipPath);
            if (move == null && spin == null)
                return;

            controller.SetAudioClips(move, spin);
        }

        private static void ReactivateDeployedDrill(GameObject instance)
        {
            if (instance == null)
                return;

            if (!instance.activeSelf)
                instance.SetActive(true);

            DMWalkerDrillController controller = instance.GetComponent<DMWalkerDrillController>();
            if (controller == null)
                controller = instance.GetComponentInChildren<DMWalkerDrillController>();
            if (controller != null)
                controller.enabled = true;

            DMWalkerDrillUsable usable = instance.GetComponent<DMWalkerDrillUsable>();
            if (usable == null)
                usable = instance.GetComponentInChildren<DMWalkerDrillUsable>();
            if (usable != null)
                usable.enabled = true;
        }

        private static Vector3 GetFlatForward(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return forward.normalized;
        }

        private static float SnapToGroundY(Vector3 worldPosition, float fallbackY)
        {
            float originY = worldPosition.y + 8f;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                originY = Mathf.Max(originY, terrainY + 4f);
            }

            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float rayLength = Mathf.Max(4f, originY - (fallbackY - 8f));
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    rayLength,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            if (terrain != null)
                return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;

            return fallbackY;
        }
    }
}
