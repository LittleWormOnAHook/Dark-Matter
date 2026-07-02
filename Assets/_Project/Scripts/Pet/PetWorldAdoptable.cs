using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.UI;
using UnityEngine;

namespace Project.Pet
{
    /// <summary>
    /// Press E near a wild pet to tame and add it to the player's pet list.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PetWorldAdoptable : MonoBehaviour, IWorldUsable
    {
        private const float UsePriorityBase = 92f;

        [SerializeField] private PetController pet;
        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private string instanceId;

        [Header("Taming Override")]
        [SerializeField] private string requiredItemIdOverride;
        [SerializeField] private float tameDurationSecondsOverride = -1f;
        [SerializeField] private float progressPerInteractOverride = -1f;

        public string PromptText => BuildPrompt();
        public float InteractRange => interactRange;
        public string InstanceId => ResolveInstanceId();

        private void Awake()
        {
            if (pet == null)
                pet = GetComponent<PetController>();

            Collider interactCollider = GetComponent<Collider>();
            if (interactCollider != null)
                interactCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            PetTamingProgressUI.Hide();
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!CanInteract(context, out float distance, out Vector3 aimPoint))
                return -1f;

            float aimRadius = distance <= interactRange * 0.75f ? 4.5f : 2.4f;
            float score = WorldUseController.ScorePickupAim(context.ViewRay, aimPoint, distance, context.UseRange, aimRadius);
            if (score < 0f)
            {
                if (distance > interactRange * 0.9f)
                    return -1f;

                score = Mathf.Max(0f, (interactRange - distance) * 55f);
            }

            return UsePriorityBase + score * 0.01f;
        }

        private bool CanInteract(WorldUseContext context, out float distance, out Vector3 aimPoint)
        {
            distance = 0f;
            aimPoint = transform.position + Vector3.up * 0.35f;

            if (!GameSession.HasStarted || pet == null || pet.IsOwned || !isActiveAndEnabled)
                return false;

            distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > Mathf.Max(interactRange, context.UseRange))
                return false;

            Collider col = GetComponent<Collider>();
            if (col != null)
                aimPoint = col.bounds.center;

            return true;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (pet == null || pet.IsOwned)
                return false;

            PetManager manager = PetManager.EnsureExists();
            if (manager == null)
                return false;

            if (GetTamingState(manager) == PetTamingState.Complete)
                return CompleteAdoption(manager);

            if (!TryConsumeRequiredItem(context, out string itemMessage))
            {
                if (!string.IsNullOrEmpty(itemMessage))
                    PickupToastUI.Show(itemMessage);
                return false;
            }

            float progress = manager.GetTamingProgress(InstanceId);
            progress += GetProgressPerInteract();
            progress = Mathf.Clamp01(progress);
            manager.SetTamingProgress(InstanceId, progress);

            PetTamingProgressUI.Show(transform, progress, BuildPrompt());
            if (progress < 1f)
                return true;

            return CompleteAdoption(manager, wasTamed: true);
        }

        private bool CompleteAdoption(PetManager manager, bool wasTamed = false)
        {
            if (!manager.TryAdoptPet(pet, out string message, wasTamed))
            {
                if (!string.IsNullOrEmpty(message))
                    PickupToastUI.Show(message);
                return false;
            }

            manager.ClearTamingProgress(InstanceId);
            PetTamingProgressUI.Hide();
            PickupToastUI.Show(message);
            enabled = false;
            return true;
        }

        private bool TryConsumeRequiredItem(WorldUseContext context, out string message)
        {
            message = string.Empty;
            string requiredItemId = GetRequiredItemId();
            if (string.IsNullOrEmpty(requiredItemId))
                return true;

            GameObject player = context.PlayerTransform != null
                ? context.PlayerTransform.gameObject
                : PlayerLocator.FindPlayerObject();
            if (player == null)
            {
                message = "No player inventory.";
                return false;
            }

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            if (inventory == null)
            {
                message = "No player inventory.";
                return false;
            }

            if (!inventory.TryConsumeItemById(requiredItemId, 1))
            {
                ItemData item = ItemRegistry.Resolve(requiredItemId);
                string label = item != null ? item.itemName : requiredItemId;
                message = $"Need {label} to tame.";
                return false;
            }

            return true;
        }

        public PetTamingState GetTamingState(PetManager manager = null)
        {
            manager ??= PetManager.EnsureExists();
            if (manager == null)
                return PetTamingState.NotStarted;

            float progress = manager.GetTamingProgress(InstanceId);
            if (progress >= 1f)
                return PetTamingState.Complete;
            if (progress > 0f)
                return PetTamingState.InProgress;
            return PetTamingState.NotStarted;
        }

        private string BuildPrompt()
        {
            PetManager manager = PetManager.EnsureExists();
            PetTamingState state = GetTamingState(manager);
            string petName = pet != null ? pet.DisplayName : "pet";

            if (state == PetTamingState.Complete)
                return $"Press E to Adopt {petName}";

            string required = GetRequiredItemId();
            if (!string.IsNullOrEmpty(required))
            {
                ItemData item = ItemRegistry.Resolve(required);
                string label = item != null ? item.itemName : required;
                return $"Press E to Feed {label} ({Mathf.RoundToInt(manager.GetTamingProgress(InstanceId) * 100f)}%)";
            }

            return $"Press E to Tame {petName} ({Mathf.RoundToInt(manager.GetTamingProgress(InstanceId) * 100f)}%)";
        }

        private string GetRequiredItemId()
        {
            if (!string.IsNullOrEmpty(requiredItemIdOverride))
                return requiredItemIdOverride;

            PetDefinition definition = pet != null ? pet.Definition : null;
            return definition != null ? definition.requiredItemId : string.Empty;
        }

        private float GetProgressPerInteract()
        {
            if (progressPerInteractOverride > 0f)
                return progressPerInteractOverride;

            PetDefinition definition = pet != null ? pet.Definition : null;
            if (definition != null && definition.progressPerInteract > 0f)
                return definition.progressPerInteract;

            float duration = GetTameDurationSeconds();
            if (duration <= 0f)
                return 1f;

            return Mathf.Clamp(1f / Mathf.Max(1f, duration), 0.05f, 1f);
        }

        private float GetTameDurationSeconds()
        {
            if (tameDurationSecondsOverride >= 0f)
                return tameDurationSecondsOverride;

            PetDefinition definition = pet != null ? pet.Definition : null;
            return definition != null ? definition.tameDurationSeconds : 8f;
        }

        private string ResolveInstanceId()
        {
            if (!string.IsNullOrWhiteSpace(instanceId))
                return instanceId;

            instanceId = $"{pet?.PetId ?? name}:{gameObject.GetEntityId()}";
            return instanceId;
        }

        public static PetWorldAdoptable FindAdoptableForPrompt(WorldUseContext context)
        {
            if (context.PlayerTransform == null)
                return null;

            PetWorldAdoptable closest = FindClosestAdoptable(context.PlayerPosition, context.UseRange);
            if (closest != null)
                return closest;

            return FindBestAdoptable(context, 0f);
        }

        public static bool IsWithinImmediateAdoptRange(Vector3 playerPosition, PetWorldAdoptable adoptable)
        {
            if (adoptable == null)
                return false;

            float distance = Vector3.Distance(playerPosition, adoptable.transform.position);
            return distance <= adoptable.interactRange * 0.9f;
        }

        public static PetWorldAdoptable FindClosestAdoptable(Vector3 playerPosition, float range)
        {
            PetWorldAdoptable[] adoptables = Object.FindObjectsByType<PetWorldAdoptable>(FindObjectsInactive.Exclude);
            PetWorldAdoptable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < adoptables.Length; i++)
            {
                PetWorldAdoptable adoptable = adoptables[i];
                if (adoptable == null || adoptable.pet == null || adoptable.pet.IsOwned || !adoptable.isActiveAndEnabled)
                    continue;

                float distance = Vector3.Distance(playerPosition, adoptable.transform.position);
                if (distance > Mathf.Min(range, adoptable.interactRange) || distance >= bestDistance)
                    continue;

                best = adoptable;
                bestDistance = distance;
            }

            return best;
        }

        public static PetWorldAdoptable FindBestAdoptable(WorldUseContext context, float minPriority = 70f)
        {
            PetWorldAdoptable[] adoptables = Object.FindObjectsByType<PetWorldAdoptable>(FindObjectsInactive.Exclude);
            PetWorldAdoptable best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < adoptables.Length; i++)
            {
                PetWorldAdoptable adoptable = adoptables[i];
                if (adoptable == null || !adoptable.isActiveAndEnabled)
                    continue;

                float score = adoptable.GetUsePriority(context);
                if (score < minPriority || score <= bestScore)
                    continue;

                best = adoptable;
                bestScore = score;
            }

            return best;
        }
    }
}
