using System.Collections.Generic;
using Project.Crafting;
using Project.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK station / standalone craft window. Recipe list + Craft call through CraftingUI.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-370)]
    [DisallowMultipleComponent]
    public class DMUiToolkitCraft : MonoBehaviour
    {
        private static DMUiToolkitCraft instance;

        private UIDocument document;
        private VisualElement root;
        private Label titleLabel;
        private Label statusLabel;
        private ScrollView list;
        private Label detailTitle;
        private Label detailMeta;
        private Label detailIngs;
        private SliderInt amountSlider;
        private Label amountValue;
        private Button confirmButton;
        private Button closeButton;
        private bool bound;
        private bool uguiHidden;
        private bool wired;
        private bool open;
        private CraftingUI source;
        private RecipeDefinition selected;
        private bool wasCrafting;

        public static bool IsOpen => instance != null && instance.open;


        public static DMUiToolkitCraft EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.CraftName,
                DMUiToolkitOverlayDocument.CraftUxml,
                DMUiToolkitOverlayDocument.CraftUss,
                DMUiToolkitOverlayDocument.CraftSort);
            if (doc == null)
                return null;

            DMUiToolkitCraft host = doc.GetComponent<DMUiToolkitCraft>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitCraft>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(CraftingUI crafting)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            if (crafting == null)
                return false;

            DMUiToolkitCraft host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal(crafting);
            return true;
        }

        public static void Hide()
        {
            instance?.HideInternal(closeSource: false);
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!open)
                return;

            if (UiEscapeGate.TryConsumeEscape())
                HideInternal(closeSource: true);
        }

        private void LateUpdate()
        {
            if (!bound)
                return;

            if (open)
            {
                bool crafting = source != null && source.ToolkitIsCrafting;
                if (wasCrafting && !crafting)
                    Rebuild();
                wasCrafting = crafting;
                if (!uguiHidden)
                {
                    HideUgui();
                    uguiHidden = true;
                }
            }
            else
            {
                uguiHidden = false;
            }
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("craft-root") ?? tree;
            titleLabel = tree.Q<Label>("craft-title");
            statusLabel = tree.Q<Label>("craft-status");
            list = tree.Q<ScrollView>("craft-list");
            detailTitle = tree.Q<Label>("craft-detail-title");
            detailMeta = tree.Q<Label>("craft-detail-meta");
            detailIngs = tree.Q<Label>("craft-detail-ings");
            amountSlider = tree.Q<SliderInt>("craft-amount");
            amountValue = tree.Q<Label>("craft-amount-value");
            confirmButton = tree.Q<Button>("craft-confirm");
            closeButton = tree.Q<Button>("craft-close");
            Wire();
            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (confirmButton != null)
                confirmButton.clicked += OnCraft;
            if (closeButton != null)
                closeButton.clicked += () => HideInternal(closeSource: true);
            if (amountSlider != null)
                amountSlider.RegisterValueChangedCallback(OnAmountChanged);
            wired = true;
        }

        private void ShowInternal(CraftingUI crafting)
        {
            BindTree();
            source = crafting;
            open = true;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Rebuild();
        }

        private void HideInternal(bool closeSource)
        {
            open = false;
            selected = null;
            DMUiToolkitOverlayDocument.SetShown(root, false);

            if (closeSource && source != null)
                source.CloseStandalonePanel(true);
        }

        private void Rebuild()
        {
            if (list == null || source == null)
                return;

            list.Clear();
            IReadOnlyList<RecipeDefinition> recipes = source.ToolkitGetRecipes();
            CraftingManager manager = source.ToolkitCraftingManager;
            InventorySystem inventory = source.ToolkitInventory;

            int ready = 0;
            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    RecipeDefinition recipe = recipes[i];
                    if (recipe == null)
                        continue;

                    bool can = manager != null && inventory != null && manager.CanCraft(recipe, inventory, 1);
                    if (can)
                        ready++;

                    RecipeDefinition captured = recipe;
                    Button row = new Button();
                    row.text = ResolveName(recipe);
                    row.AddToClassList("dmg-craft-row");
                    if (can)
                        row.AddToClassList("dmg-craft-row-ready");
                    if (selected == recipe)
                        row.AddToClassList("dmg-craft-row-sel");
                    row.clicked += () => Select(captured);
                    list.Add(row);
                }
            }

            string station = "Crafting";
            if (manager != null && manager.CurrentStation.HasValue)
                station = manager.CurrentStation.Value == CraftingStationType.Cooking ? "Cooking" : "Workbench";
            if (titleLabel != null)
                titleLabel.text = station;
            if (statusLabel != null)
            {
                int count = recipes != null ? recipes.Count : 0;
                statusLabel.text = count == 0
                    ? "No learned blueprints for this station"
                    : $"{station} station  -  {ready} ready  -  {count} for this station";
            }

            RefreshDetail();
        }

        private void Select(RecipeDefinition recipe)
        {
            selected = recipe;
            Rebuild();
        }

        private void RefreshDetail()
        {
            if (selected == null)
            {
                if (detailTitle != null)
                    detailTitle.text = "Select a blueprint";
                if (detailMeta != null)
                    detailMeta.text = "Click a recipe above to craft.";
                if (detailIngs != null)
                    detailIngs.text = string.Empty;
                if (amountSlider != null)
                    amountSlider.SetEnabled(false);
                if (confirmButton != null)
                    confirmButton.SetEnabled(false);
                return;
            }

            if (detailTitle != null)
                detailTitle.text = ResolveName(selected);

            CraftingManager manager = source != null ? source.ToolkitCraftingManager : null;
            InventorySystem inventory = source != null ? source.ToolkitInventory : null;
            int max = manager != null && inventory != null
                ? Mathf.Max(1, manager.GetMaxCraftCount(selected, inventory))
                : 1;
            float duration = CraftingManager.GetCraftDurationSeconds(selected);
            int tier = Mathf.Max(1, selected.recipeTier);
            if (detailMeta != null)
            {
                detailMeta.text = selected.outputAmount > 1
                    ? $"Tier {tier}  -  {duration:0.#}s each  -  {selected.outputAmount}x output"
                    : $"Tier {tier}  -  {duration:0.#}s each  -  1x output";
            }

            if (detailIngs != null)
                detailIngs.text = BuildIngredients(selected);

            if (amountSlider != null)
            {
                amountSlider.lowValue = 1;
                amountSlider.highValue = max;
                if (amountSlider.value < 1 || amountSlider.value > max)
                    amountSlider.SetValueWithoutNotify(1);
                amountSlider.SetEnabled(max > 1 && source != null && !source.ToolkitIsCrafting);
            }

            if (amountValue != null)
                amountValue.text = (amountSlider != null ? amountSlider.value : 1).ToString();

            bool can = manager != null && inventory != null && manager.CanCraft(selected, inventory, GetAmount());
            if (confirmButton != null)
                confirmButton.SetEnabled(can && source != null && !source.ToolkitIsCrafting);
        }

        private void OnAmountChanged(ChangeEvent<int> evt)
        {
            if (amountValue != null)
                amountValue.text = evt.newValue.ToString();
            RefreshDetail();
        }

        private int GetAmount()
        {
            return amountSlider != null ? Mathf.Max(1, amountSlider.value) : 1;
        }

        private void OnCraft()
        {
            if (source == null || selected == null)
                return;

            source.ToolkitTryCraft(selected, GetAmount());
            Rebuild();
        }

        private static string ResolveName(RecipeDefinition recipe)
        {
            if (recipe == null)
                return "Recipe";
            if (!string.IsNullOrWhiteSpace(recipe.displayName))
                return recipe.displayName;
            if (recipe.outputItem != null && !string.IsNullOrEmpty(recipe.outputItem.itemName))
                return recipe.outputItem.itemName;
            return recipe.name;
        }

        private static string BuildIngredients(RecipeDefinition recipe)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
                return "No ingredients";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                RecipeIngredient ingredient = recipe.ingredients[i];
                if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                    continue;

                if (sb.Length > 0)
                    sb.Append("  -  ");
                sb.Append(ingredient.amount);
                sb.Append('x');
                sb.Append(ingredient.item.itemName);
            }

            return sb.Length > 0 ? sb.ToString() : "No ingredients";
        }

        private static void HideUgui()
        {
            CraftingUI ui = Object.FindAnyObjectByType<CraftingUI>(FindObjectsInactive.Include);
            ui?.ToolkitHideUguiShell();
        }
    }
}
