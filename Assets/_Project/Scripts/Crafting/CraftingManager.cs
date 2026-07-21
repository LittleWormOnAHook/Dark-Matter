using System;
using System.Collections.Generic;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using Project.Quests;
using UnityEngine;

namespace Project.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }

        private readonly HashSet<string> discoveredRecipeIds = new HashSet<string>();
        private readonly List<string> pendingRecipeScrollIds = new List<string>();
        private InventorySystem inventorySystem;

        public CraftingStationType? CurrentStation { get; set; }

        public event Action OnRecipesChanged;
        public event Action<RecipeDefinition> OnCrafted;
        public event Action OnPendingScrollsChanged;

        public IReadOnlyList<string> GetPendingRecipeScrolls() => pendingRecipeScrollIds;

        public bool AddPendingRecipeScroll(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            RecipeDefinition recipe = RecipeRegistry.Resolve(id);
            if (recipe == null)
            {
                Debug.LogWarning($"CraftingManager: Unknown recipe id '{id}'.");
                return false;
            }

            string resolvedId = recipe.ResolvedId;
            if (IsDiscovered(resolvedId) || pendingRecipeScrollIds.Contains(resolvedId))
                return false;

            pendingRecipeScrollIds.Add(resolvedId);
            OnPendingScrollsChanged?.Invoke();
            OnRecipesChanged?.Invoke();
            return true;
        }

        public bool TryLearnPendingScrollAt(int index)
        {
            if (index < 0 || index >= pendingRecipeScrollIds.Count)
                return false;

            string id = pendingRecipeScrollIds[index];
            pendingRecipeScrollIds.RemoveAt(index);
            OnPendingScrollsChanged?.Invoke();

            if (!IsDiscovered(id))
                DiscoverRecipe(id);
            else
                OnRecipesChanged?.Invoke();

            return true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            BindInventorySystem();
        }

        private void OnDestroy()
        {
            UnbindInventorySystem();

            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            BindInventorySystem();
        }

        public void DiscoverRecipe(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            RecipeDefinition recipe = RecipeRegistry.Resolve(id);
            if (recipe == null)
            {
                Debug.LogWarning($"CraftingManager: Unknown recipe id '{id}'.");
                return;
            }

            if (!discoveredRecipeIds.Add(recipe.ResolvedId))
                return;

            ProgressionRewardGranter.GrantXp(
                ProgressionXpDefaults.RecipeLearnXp,
                XpSource.Craft,
                $"recipe-learn:{recipe.ResolvedId}",
                "Recipe");

            OnRecipesChanged?.Invoke();
        }

        public bool IsDiscovered(string id)
        {
            return !string.IsNullOrEmpty(id) && discoveredRecipeIds.Contains(id);
        }

        public IReadOnlyList<RecipeDefinition> GetDiscoveredRecipes(CraftingStationType? stationType = null)
        {
            List<RecipeDefinition> result = new List<RecipeDefinition>();

            foreach (RecipeDefinition recipe in RecipeRegistry.GetAllRecipes())
            {
                if (recipe == null || !IsDiscovered(recipe.ResolvedId))
                    continue;

                if (stationType.HasValue && recipe.stationType != stationType.Value)
                    continue;

                result.Add(recipe);
            }

            return result;
        }

        public int CountItem(ItemData item, InventorySystem inventory)
        {
            return inventory != null ? inventory.CountItem(item) : 0;
        }

        public bool HasIngredients(RecipeDefinition recipe, InventorySystem inventory)
        {
            if (recipe == null || inventory == null || recipe.ingredients == null)
                return false;

            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                RecipeIngredient ingredient = recipe.ingredients[i];
                if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                    continue;

                if (CountItem(ingredient.item, inventory) < ingredient.amount)
                    return false;
            }

            return true;
        }

        public bool CanCraft(RecipeDefinition recipe, InventorySystem inventory)
        {
            if (recipe == null || inventory == null || recipe.outputItem == null || recipe.outputAmount <= 0)
                return false;

            if (!IsDiscovered(recipe.ResolvedId))
                return false;

            if (!CurrentStation.HasValue || recipe.stationType != CurrentStation.Value)
                return false;

            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (!LevelUnlockUtility.CanAccess(progression, recipe.requiredPlayerLevel))
                return false;

            if (!HasIngredients(recipe, inventory))
                return false;

            return inventory.HasSpaceInMainInventory(recipe.outputItem, recipe.outputAmount);
        }

        public bool TryCraft(RecipeDefinition recipe, InventorySystem inventory)
        {
            if (!CanCraft(recipe, inventory))
                return false;

            List<(ItemData item, int amount)> removedIngredients = new List<(ItemData, int)>();

            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                RecipeIngredient ingredient = recipe.ingredients[i];
                if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                    continue;

                if (!inventory.RemoveItem(ingredient.item, ingredient.amount))
                {
                    RollbackRemovedIngredients(inventory, removedIngredients);
                    return false;
                }

                removedIngredients.Add((ingredient.item, ingredient.amount));
            }

            int added = inventory.AddItemToMainInventory(recipe.outputItem, recipe.outputAmount);
            if (added < recipe.outputAmount)
            {
                if (added > 0)
                    inventory.RemoveItem(recipe.outputItem, added);

                RollbackRemovedIngredients(inventory, removedIngredients);
                return false;
            }

            OnCrafted?.Invoke(recipe);

            int craftXp = Mathf.Max(5, 8 + recipe.recipeTier * 4);
            ProgressionRewardGranter.GrantXp(craftXp, XpSource.Craft, $"craft:{recipe.ResolvedId}", "Craft");

            QuestManager questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            questManager?.NotifyItemCrafted(recipe.outputItem, recipe.outputAmount);

            return true;
        }

        private static void RollbackRemovedIngredients(InventorySystem inventory, List<(ItemData item, int amount)> removedIngredients)
        {
            for (int i = 0; i < removedIngredients.Count; i++)
                inventory.AddItem(removedIngredients[i].item, removedIngredients[i].amount);
        }

        public string[] BuildSave()
        {
            string[] ids = new string[discoveredRecipeIds.Count];
            discoveredRecipeIds.CopyTo(ids);
            return ids;
        }

        public string[] BuildPendingSave()
        {
            return pendingRecipeScrollIds.Count > 0
                ? pendingRecipeScrollIds.ToArray()
                : null;
        }

        public void ApplySave(string[] ids)
        {
            discoveredRecipeIds.Clear();

            if (ids != null)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    if (!string.IsNullOrEmpty(ids[i]))
                        discoveredRecipeIds.Add(ids[i]);
                }
            }

            OnRecipesChanged?.Invoke();
        }

        public void ApplyPendingSave(string[] ids)
        {
            pendingRecipeScrollIds.Clear();

            if (ids != null)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    if (string.IsNullOrEmpty(ids[i]))
                        continue;

                    if (discoveredRecipeIds.Contains(ids[i]) || pendingRecipeScrollIds.Contains(ids[i]))
                        continue;

                    pendingRecipeScrollIds.Add(ids[i]);
                }
            }

            OnPendingScrollsChanged?.Invoke();
            OnRecipesChanged?.Invoke();
        }

        private void BindInventorySystem()
        {
            if (inventorySystem != null)
                return;

            inventorySystem = GetComponent<InventorySystem>() ?? FindAnyObjectByType<InventorySystem>();
        }

        private void UnbindInventorySystem()
        {
            inventorySystem = null;
        }
    }
}
