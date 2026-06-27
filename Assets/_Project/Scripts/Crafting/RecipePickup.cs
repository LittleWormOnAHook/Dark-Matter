using Project.Core;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Crafting
{
    [RequireComponent(typeof(Collider))]
    public class RecipePickup : MonoBehaviour, IWorldUsable
    {
        [Header("Recipe")]
        [SerializeField] private string recipeId;

        [Header("Interaction")]
        [SerializeField] private string promptText = "Press E to use";
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private string collectedMessage = "Recipe scroll collected!";

        private UIManager uiManager;
        private CraftingManager craftingManager;
        private bool playerInRange;
        private bool learned;

        public bool IsLearned => learned;
        public float InteractRange => interactRange;

        public void Configure(string id, string prompt = "Press E to use", float range = 2.5f)
        {
            recipeId = id;
            if (!string.IsNullOrEmpty(prompt))
                promptText = prompt;
            interactRange = range;
        }

        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                return;
            }

            collider.isTrigger = true;
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || learned)
                return;

            playerInRange = true;
            ShowPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            playerInRange = false;
            uiManager?.HideInteractionPrompt();
        }

        public static RecipePickup GetInteractable(Vector3 playerPosition, float range)
        {
            RecipePickup[] pickups = FindObjectsByType<RecipePickup>(FindObjectsInactive.Exclude);
            RecipePickup best = null;
            float bestDistance = range;

            for (int i = 0; i < pickups.Length; i++)
            {
                RecipePickup pickup = pickups[i];
                if (pickup == null || pickup.learned || !pickup.playerInRange)
                    continue;

                float distance = Vector3.Distance(playerPosition, pickup.transform.position);
                if (distance <= pickup.interactRange && distance <= bestDistance)
                {
                    best = pickup;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (WorldUseController.IsPlayerFocusedOnPickup(context)
                && !IsFocusedRecipe(context))
                return -1f;

            if (learned || !playerInRange || !GameSession.HasStarted)
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > interactRange)
                return -1f;

            return 92f - distance;
        }

        private bool IsFocusedRecipe(WorldUseContext context)
        {
            return WorldUseController.TryFindFocusedRecipePickup(context, out RecipePickup focused, out _)
                && focused == this;
        }

        public bool TryUse(WorldUseContext context)
        {
            return TryLearn();
        }

        public bool TryLearn()
        {
            if (!playerInRange || learned || !GameSession.HasStarted || string.IsNullOrEmpty(recipeId))
                return false;

            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

            if (craftingManager == null)
            {
                GameObject player = PlayerLocator.FindPlayerObject();
                if (player != null)
                    craftingManager = player.GetComponent<CraftingManager>() ?? player.AddComponent<CraftingManager>();
            }

            if (craftingManager == null)
                return false;

            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);
            if (recipe == null)
            {
                Debug.LogWarning($"RecipePickup: Unknown recipe id '{recipeId}'.");
                return false;
            }

            if (craftingManager.IsDiscovered(recipe.ResolvedId))
            {
                MarkCollected(showToast: false);
                return true;
            }

            if (!craftingManager.AddPendingRecipeScroll(recipe.ResolvedId))
            {
                MarkCollected(showToast: false);
                return true;
            }

            MarkCollected(showToast: true);
            return true;
        }

        private void MarkCollected(bool showToast)
        {
            learned = true;
            playerInRange = false;
            uiManager?.HideInteractionPrompt();

            if (showToast)
            {
                RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);
                string recipeName = recipe != null && !string.IsNullOrEmpty(recipe.displayName)
                    ? recipe.displayName
                    : recipeId;
                PickupToastUI.Show(string.IsNullOrEmpty(collectedMessage)
                    ? $"Collected: {recipeName}"
                    : $"{collectedMessage} {recipeName}");
            }

            gameObject.SetActive(false);
        }

        public bool IsPlayerInRange => playerInRange;

        public string GetInteractionPromptMessage()
        {
            if (learned)
                return null;

            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);
            string label = recipe != null && !string.IsNullOrEmpty(recipe.displayName)
                ? recipe.displayName
                : "Recipe";

            return $"{promptText} — {label}";
        }

        private void ShowPrompt()
        {
            if (uiManager == null || learned)
                return;

            string message = GetInteractionPromptMessage();
            if (string.IsNullOrEmpty(message))
                return;

            uiManager.ShowInteractionPrompt(message);
        }
    }
}
