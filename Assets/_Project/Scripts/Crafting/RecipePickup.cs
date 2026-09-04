using Project.Core;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Crafting
{
    [RequireComponent(typeof(Collider))]
    public class RecipePickup : MonoBehaviour, IWorldUsable, IWorldIndicatorAnchor
    {
        [Header("Blueprint")]
        [SerializeField] private string recipeId;

        [Header("Interaction")]
        [SerializeField] private string promptText = "Press E to use";
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private string collectedMessage = "Blueprint collected!";

        [Header("Indicator")]
        [Tooltip("Optional explicit stem/dot attach point. When empty, uses renderer/collider bounds center.")]
        [SerializeField] private Transform indicatorAnchor;
        [Tooltip("World-up stem length (m) when the player first locks onto this blueprint.")]
        [SerializeField] private float indicatorStemMinHeight = 0.25f;
        [Tooltip("World-up stem length (m) at planar near reach (dot largest, still on tip).")]
        [SerializeField] private float indicatorStemMaxHeight = 0.5f;

        private UIManager uiManager;
        private CraftingManager craftingManager;
        private bool playerInRange;
        private bool learned;
        private Vector3 indicatorLocalOffset;
        private bool hasIndicatorLocalOffset;

        public bool IsLearned => learned;
        public float InteractRange => interactRange;

        public bool IsIndicatorAvailable => !learned && isActiveAndEnabled;

        public float IndicatorStemMinHeight => Mathf.Max(0.05f, indicatorStemMinHeight);

        public float IndicatorStemMaxHeight => Mathf.Max(IndicatorStemMinHeight, indicatorStemMaxHeight);

        public Vector3 GetIndicatorWorldAnchor()
        {
            if (indicatorAnchor != null)
                return indicatorAnchor.position;

            if (hasIndicatorLocalOffset)
                return transform.TransformPoint(indicatorLocalOffset);

            return ResolveIndicatorWorldAnchor();
        }

        public void Configure(string id, string prompt = "Press E to use", float range = 2.5f)
        {
            recipeId = id;
            if (!string.IsNullOrEmpty(prompt))
                promptText = prompt;
            interactRange = range;
        }

        private void Awake()
        {
            // Match ItemPickup trigger size (sphere radius 0.45).
            const float MatchItemPickupRadius = 0.45f;
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = MatchItemPickupRadius;
                return;
            }

            collider.isTrigger = true;
            if (collider is SphereCollider existingSphere)
                existingSphere.radius = Mathf.Max(existingSphere.radius, MatchItemPickupRadius);
            else if (collider is BoxCollider box)
            {
                float diameter = MatchItemPickupRadius * 2f;
                box.size = new Vector3(
                    Mathf.Max(box.size.x, diameter),
                    Mathf.Max(box.size.y, diameter),
                    Mathf.Max(box.size.z, diameter));
            }
            else if (collider is CapsuleCollider capsule)
            {
                capsule.radius = Mathf.Max(capsule.radius, MatchItemPickupRadius);
                capsule.height = Mathf.Max(capsule.height, MatchItemPickupRadius * 2f);
            }
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
            PickupProximityDotUI.RegisterRecipe(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            PickupProximityDotUI.UnregisterRecipe(this);
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            CacheIndicatorLocalOffset();
        }

        private void CacheIndicatorLocalOffset()
        {
            if (indicatorAnchor != null)
            {
                hasIndicatorLocalOffset = false;
                return;
            }

            Vector3 world = ResolveIndicatorWorldAnchor();
            indicatorLocalOffset = transform.InverseTransformPoint(world);
            hasIndicatorLocalOffset = true;
        }

        private Vector3 ResolveIndicatorWorldAnchor()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (TryEncapsulateRendererBounds(renderers, out Bounds rendBounds))
                return rendBounds.center;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            if (TryEncapsulateColliderBounds(colliders, out Bounds colBounds))
                return colBounds.center;

            return transform.position + Vector3.up * 0.2f;
        }

        private static bool TryEncapsulateRendererBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            if (renderers == null)
                return false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || !rend.enabled || !rend.gameObject.activeInHierarchy)
                    continue;
                if (rend is ParticleSystemRenderer)
                    continue;

                if (!any)
                {
                    bounds = rend.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }

            return any;
        }

        private static bool TryEncapsulateColliderBounds(Collider[] colliders, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            if (colliders == null)
                return false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                if (!any)
                {
                    bounds = col.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(col.bounds);
                }
            }

            return any;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || learned)
                return;

            playerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            playerInRange = false;
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

            if (learned || !GameSession.HasStarted)
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, GetIndicatorWorldAnchor());
            if (distance > Mathf.Min(interactRange, WorldUseController.MaxPickupDistance))
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
            if (learned || !GameSession.HasStarted || string.IsNullOrEmpty(recipeId))
                return false;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return false;

            float distance = Vector3.Distance(player.transform.position, GetIndicatorWorldAnchor());
            float maxRange = Mathf.Min(interactRange, WorldUseController.MaxPickupDistance);
            if (distance > maxRange && !playerInRange)
                return false;

            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

            if (craftingManager == null)
            {
                craftingManager = player.GetComponent<CraftingManager>() ?? player.AddComponent<CraftingManager>();
            }

            if (craftingManager == null)
                return false;

            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);
            if (recipe == null)
            {
                Debug.LogWarning($"RecipePickup: Unknown blueprint id '{recipeId}'.");
                return false;
            }

            if (craftingManager.IsDiscovered(recipe.ResolvedId))
            {
                MarkCollected(showToast: false);
                return true;
            }

            if (!craftingManager.AddPendingBlueprintScroll(recipe.ResolvedId))
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
            PickupProximityDotUI.NotifyRecipeCollected(this);

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
                : "Blueprint";

            return $"{promptText} — {label}";
        }
    }
}
