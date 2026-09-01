using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Pioneers;
using Project.Survival;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// In-world Toolkit HUD: condensed bars, hotbar, tools, prompt, radio, popups.
    /// Sibling of UITK_Root, same Panel Settings instance as the shell.
    /// Dual-run: matching uGUI stays in the scene and is hidden while this drives.
    /// UI Builder hosts stay visible in USS; unused pieces hide here at runtime.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public partial class DMUiToolkitHud : MonoBehaviour
    {
        public const string LogStamp = "DMUiToolkit 0901-gamelog";

        private static DMUiToolkitHud instance;
        private static bool stamped;

        private UIDocument document;
        private VisualElement hudRoot;
        private VisualElement healthFill;
        private VisualElement energyFill;
        private VisualElement staminaFill;
        private VisualElement oxygenFill;
        private Label healthLabel;
        private Label energyLabel;
        private Label staminaLabel;
        private Label oxygenLabel;
        private Label promptLabel;
        private VisualElement radioRoot;
        private Label radioTitle;
        private Label radioBody;
        private VisualElement popupsRoot;
        private VisualElement popupTemplate;
        private VisualElement minimapHost;
        private SurvivalStats survivalStats;
        private bool gameplayVisible;
        private bool uguiHidden;
        private Coroutine promptRoutine;
        private bool bound;

        private readonly List<BoundHudSlot> boundSlots = new List<BoundHudSlot>();
        private VisualElement hotbarHost;
        private VisualElement toolsHost;
        private InventorySystem inventorySystem;
        private EquipmentController equipmentController;
        private InventoryItemActions itemActions;
        private BoundHudSlot pointerSlot;
        private Vector2 pointerDownPanelPos;
        private Vector2 lastSlotPointerPanelPos;
        private int capturedPointerId = -1;
        private bool slotDragActive;
        private VisualElement slotDragGhost;
        private int slotDragSourceIndex = -1;
        private readonly Queue<string> popupQueue = new Queue<string>();
        private readonly List<VisualElement> popupVisible = new List<VisualElement>();
        private Coroutine popupRolodexRoutine;
        private bool popupIsFading;
        private string lastLoggedPrompt;
        private string lastLoggedRadio;

        private const int PopupMaxVisible = 4;
        private const float PopupHoldSeconds = 3.0f;
        private const float PopupFadeSeconds = 0.35f;
        private const float PopupFocusFontSize = 40f;
        private const float DragThresholdPx = 8f;
        private static readonly float[] PopupDepthFontSizes = { PopupFocusFontSize, 32f, 23f, 15f };
        private static readonly float[] PopupDepthOpacities = { 1f, 0.78f, 0.56f, 0.40f };

        public static DMUiToolkitHud Instance => instance;

        public static bool IsDriving
        {
            get
            {
                if (!DMUiToolkitConfig.IsEnabled)
                    return false;
                if (!DMUiToolkitBootstrap.IsRootActive)
                    return false;
                if (!GameSession.HasStarted)
                    return false;
                return instance != null && instance.isActiveAndEnabled && instance.bound;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            stamped = false;
        }

        public static void Bind(UIDocument hudDocument)
        {
            if (hudDocument == null)
                return;

            DMUiToolkitHud hud = hudDocument.GetComponent<DMUiToolkitHud>();
            if (hud == null)
                hud = hudDocument.gameObject.AddComponent<DMUiToolkitHud>();

            hud.document = hudDocument;
            hud.BindTree();
        }

        public static void ShowPrompt(string message)
        {
            if (instance == null)
                return;
            instance.SetPrompt(message);
        }

        public static void HidePrompt()
        {
            if (instance == null)
                return;
            instance.SetPrompt(null);
        }

        public static void ShowPopup(string message)
        {
            if (instance == null || string.IsNullOrEmpty(message))
                return;
            DMGameLog.Add(message, DMGameLog.KindFromPopupText(message));
            instance.PushPopup(message);
        }

        public static void SetRadio(string title, string body, bool visible)
        {
            if (instance == null)
                return;
            instance.ApplyRadio(title, body, visible);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            GameSession.GameStarted -= OnGameStarted;
            GameSession.GameStarted += OnGameStarted;
            BindTree();
            RefreshVisibility();
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= OnGameStarted;
            UnbindSurvival();
            UnbindHudSlots();
            UnbindCompanionHud();
            StopPopupRolodex();
            RestoreUguiCounterparts();
            if (instance == this)
                bound = false;
        }

        private void OnDestroy()
        {
            GameSession.GameStarted -= OnGameStarted;
            UnbindSurvival();
            UnbindHudSlots();
            UnbindCompanionHud();
            StopPopupRolodex();
            RestoreUguiCounterparts();
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            RefreshVisibility();
            if (gameplayVisible)
            {
                HideUguiCounterparts();
                TickCompanionHud();
            }
        }

        private void OnGameStarted()
        {
            RefreshVisibility();
            BindSurvival();
            BindInventoryEvents();
            PullStats();
            RefreshSlotIcons();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            hudRoot = root.Q<VisualElement>("hud-root") ?? root;
            healthFill = root.Q<VisualElement>("bar-health-fill");
            energyFill = root.Q<VisualElement>("bar-energy-fill");
            staminaFill = root.Q<VisualElement>("bar-stamina-fill");
            oxygenFill = root.Q<VisualElement>("bar-oxygen-fill");
            healthLabel = root.Q<Label>("bar-health-label");
            energyLabel = root.Q<Label>("bar-energy-label");
            staminaLabel = root.Q<Label>("bar-stamina-label");
            oxygenLabel = root.Q<Label>("bar-oxygen-label");
            promptLabel = root.Q<Label>("prompt");
            radioRoot = root.Q<VisualElement>("radio");
            radioTitle = root.Q<Label>("radio-title");
            radioBody = root.Q<Label>("radio-body");
            popupsRoot = root.Q<VisualElement>("popups");
            popupTemplate = root.Q<VisualElement>("popup-template");
            ApplyPopupRolodexLayout();

            HideBuilderPreviewHosts();
            BindHudSlots(root);
            minimapHost = root.Q<VisualElement>("minimap");
            BindCompanionHud(root);

            bound = hudRoot != null && healthFill != null;
            if (bound && !stamped)
            {
                stamped = true;
                Debug.Log(LogStamp + " hotbar/tools bound, sibling of UITK_Root");
            }

            BindSurvival();
            PullStats();
            RefreshVisibility();
        }

        private void HideBuilderPreviewHosts()
        {
            if (promptLabel != null)
            {
                promptLabel.text = string.Empty;
                promptLabel.style.display = DisplayStyle.None;
            }

            if (radioRoot != null)
                radioRoot.style.display = DisplayStyle.None;

            if (popupTemplate != null)
                popupTemplate.style.display = DisplayStyle.None;
        }

        private void RefreshVisibility()
        {
            bool want = DMUiToolkitConfig.IsEnabled
                && DMUiToolkitBootstrap.IsRootActive
                && GameSession.HasStarted
                && !MainMenuController.BlocksGameplayHud
                && !DMUiToolkitLoadingOverlay.IsShowing;

            if (hudRoot != null)
                hudRoot.style.display = want ? DisplayStyle.Flex : DisplayStyle.None;

            if (want && !gameplayVisible)
            {
                BindSurvival();
                PullStats();
            }

            gameplayVisible = want;

            if (want)
                HideUguiCounterparts();
            else
                RestoreUguiCounterparts();

            ApplyMenuOpenChrome();
        }

        private void ApplyMenuOpenChrome()
        {
            bool hideChrome = gameplayVisible && DMUiToolkitMenus.IsOpen;
            if (minimapHost != null)
                minimapHost.style.display = hideChrome ? DisplayStyle.None : DisplayStyle.Flex;
            if (hideChrome)
            {
                if (enemyFocusRoot != null)
                    enemyFocusRoot.style.display = DisplayStyle.None;
                if (radioRoot != null)
                    radioRoot.style.display = DisplayStyle.None;
                if (popupsRoot != null)
                    popupsRoot.style.display = DisplayStyle.None;
            }
            else if (popupsRoot != null)
                popupsRoot.style.display = DisplayStyle.Flex;
        }

        private void BindSurvival()
        {
            SurvivalStats next = FindAnyObjectByType<SurvivalStats>();
            if (next == survivalStats)
                return;

            UnbindSurvival();
            survivalStats = next;
            if (survivalStats != null)
                survivalStats.OnStatsChanged += PullStats;
        }

        private void UnbindSurvival()
        {
            if (survivalStats != null)
                survivalStats.OnStatsChanged -= PullStats;
            survivalStats = null;
        }

        private void PullStats()
        {
            if (survivalStats == null)
                return;

            SetFill(healthFill, healthLabel, survivalStats.CurrentHealth / Mathf.Max(0.01f, survivalStats.maxHealth), Mathf.CeilToInt(survivalStats.CurrentHealth).ToString());
            SetFill(energyFill, energyLabel, survivalStats.CurrentEnergy / Mathf.Max(0.01f, survivalStats.maxEnergy), Mathf.CeilToInt(survivalStats.CurrentEnergy).ToString());
            SetFill(staminaFill, staminaLabel, survivalStats.CurrentStamina / Mathf.Max(0.01f, survivalStats.maxStamina), Mathf.CeilToInt(survivalStats.CurrentStamina).ToString());

            float oxygen = survivalStats.CurrentOxygen;
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(oxygen));
            string oxygenText = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            SetFill(oxygenFill, oxygenLabel, survivalStats.GetOxygenNormalized(), oxygenText);
        }

        private static void SetFill(VisualElement fill, Label label, float normalized, string text)
        {
            float clamped = Mathf.Clamp01(normalized);
            if (fill != null)
                fill.style.width = Length.Percent(clamped * 100f);
            if (label != null)
                label.text = text ?? string.Empty;
        }

        private void SetPrompt(string message)
        {
            if (promptRoutine != null)
            {
                StopCoroutine(promptRoutine);
                promptRoutine = null;
            }

            if (promptLabel == null)
                return;

            bool show = !string.IsNullOrEmpty(message);
            promptLabel.text = show ? message : string.Empty;
            promptLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (show)
            {
                if (!string.Equals(lastLoggedPrompt, message, System.StringComparison.Ordinal))
                {
                    lastLoggedPrompt = message;
                    DMGameLog.Add(message, DMGameLogKind.Prompt);
                }
            }
            else
            {
                lastLoggedPrompt = null;
            }
        }

        private void ApplyPopupRolodexLayout()
        {
            if (popupsRoot == null)
                return;

            popupsRoot.style.left = 20f;
            popupsRoot.style.top = Length.Percent(50f);
            popupsRoot.style.marginLeft = 0f;
            popupsRoot.style.width = 360f;
            popupsRoot.style.alignItems = Align.FlexStart;
            popupsRoot.style.translate = new Translate(new Length(0f), Length.Percent(-50f));
            popupsRoot.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        private void PushPopup(string message)
        {
            if (popupsRoot == null || string.IsNullOrEmpty(message))
                return;

            popupQueue.Enqueue(message);
            FillPopupVisibleFromQueue();
            if (popupRolodexRoutine == null && isActiveAndEnabled)
                popupRolodexRoutine = StartCoroutine(RunPopupRolodex());
        }

        private void FillPopupVisibleFromQueue()
        {
            if (popupsRoot == null)
                return;

            while (popupVisible.Count < PopupMaxVisible && popupQueue.Count > 0)
            {
                string next = popupQueue.Dequeue();
                VisualElement toast = CreatePopupToast(next);
                if (toast == null)
                    continue;

                popupsRoot.Add(toast);
                popupVisible.Add(toast);
            }

            if (!popupIsFading)
                ApplyPopupRolodexPresentation(null, 1f);
        }

        private VisualElement CreatePopupToast(string message)
        {
            VisualElement toast;
            if (popupTemplate != null)
            {
                toast = CloneAuthored(popupTemplate);
                toast.name = "popup";
                toast.style.display = DisplayStyle.Flex;
                Label text = toast.Q<Label>() ?? toast as Label;
                if (text != null)
                    text.text = message;
                else
                {
                    Label fallback = new Label(message);
                    fallback.AddToClassList("dmg-hud-popup-text");
                    fallback.pickingMode = PickingMode.Ignore;
                    toast.Add(fallback);
                }
            }
            else
            {
                Label label = new Label(message);
                label.AddToClassList("dmg-hud-popup");
                label.pickingMode = PickingMode.Ignore;
                toast = label;
            }

            toast.pickingMode = PickingMode.Ignore;
            return toast;
        }

        private void StopPopupRolodex()
        {
            if (popupRolodexRoutine != null)
            {
                StopCoroutine(popupRolodexRoutine);
                popupRolodexRoutine = null;
            }

            popupIsFading = false;
            popupQueue.Clear();
            for (int i = 0; i < popupVisible.Count; i++)
            {
                if (popupVisible[i] != null)
                    popupVisible[i].RemoveFromHierarchy();
            }

            popupVisible.Clear();
        }

        private IEnumerator RunPopupRolodex()
        {
            while (popupVisible.Count > 0 || popupQueue.Count > 0)
            {
                FillPopupVisibleFromQueue();
                if (popupVisible.Count == 0)
                    break;

                ApplyPopupRolodexPresentation(null, 1f);
                yield return new WaitForSeconds(PopupHoldSeconds);

                if (popupVisible.Count == 0)
                    break;

                VisualElement focused = popupVisible[0];
                popupIsFading = true;
                float startHeight = focused.resolvedStyle.height;
                float startMarginBottom = focused.resolvedStyle.marginBottom;
                if (startHeight < 0f)
                    startHeight = 0f;

                focused.style.overflow = Overflow.Hidden;
                focused.style.minHeight = 0f;

                float elapsed = 0f;
                while (elapsed < PopupFadeSeconds)
                {
                    if (focused.parent == null)
                        break;

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / PopupFadeSeconds);
                    focused.style.opacity = 1f - t;
                    focused.style.height = Mathf.Lerp(startHeight, 0f, t);
                    focused.style.minHeight = 0f;
                    focused.style.marginBottom = Mathf.Lerp(startMarginBottom, 0f, t);
                    ApplyPopupRolodexPresentation(focused, t);
                    yield return null;
                }

                if (focused.parent != null)
                    focused.RemoveFromHierarchy();
                popupVisible.Remove(focused);
                popupIsFading = false;
                FillPopupVisibleFromQueue();
            }

            popupRolodexRoutine = null;
        }

        private void ApplyPopupRolodexPresentation(VisualElement fading, float fadeT)
        {
            for (int i = 0; i < popupVisible.Count; i++)
            {
                VisualElement toast = popupVisible[i];
                if (toast == null)
                    continue;
                if (fading != null && toast == fading)
                    continue;

                int fromDepth = i;
                int toDepth = i;
                float t = 1f;
                if (fading != null)
                {
                    toDepth = Mathf.Max(0, i - 1);
                    t = fadeT;
                }

                float font = Mathf.Lerp(PopupFontAt(fromDepth), PopupFontAt(toDepth), t);
                float opacity = Mathf.Lerp(PopupOpacityAt(fromDepth), PopupOpacityAt(toDepth), t);
                Label text = FindPopupText(toast);
                if (text != null)
                    text.style.fontSize = font;
                toast.style.opacity = opacity;
            }
        }

        private static Label FindPopupText(VisualElement toast)
        {
            if (toast == null)
                return null;
            return toast.Q<Label>() ?? toast as Label;
        }

        private static float PopupFontAt(int depth)
        {
            if (depth < 0)
                depth = 0;
            if (depth >= PopupDepthFontSizes.Length)
                depth = PopupDepthFontSizes.Length - 1;
            return PopupDepthFontSizes[depth];
        }

        private static float PopupOpacityAt(int depth)
        {
            if (depth < 0)
                depth = 0;
            if (depth >= PopupDepthOpacities.Length)
                depth = PopupDepthOpacities.Length - 1;
            return PopupDepthOpacities[depth];
        }

        private static VisualElement CloneAuthored(VisualElement source)
        {
            VisualElement clone = source is Label sourceLabel
                ? new Label(sourceLabel.text)
                : new VisualElement();

            clone.pickingMode = PickingMode.Ignore;
            foreach (string className in source.GetClasses())
                clone.AddToClassList(className);

            CopyInlineStyle(source, clone);

            for (int i = 0; i < source.childCount; i++)
                clone.Add(CloneAuthored(source[i]));

            return clone;
        }

        private static void CopyInlineStyle(VisualElement source, VisualElement dest)
        {
            IStyle from = source.style;
            IStyle to = dest.style;
            CopyIfSet(from.backgroundImage, v => to.backgroundImage = v);
            CopyIfSet(from.unityBackgroundImageTintColor, v => to.unityBackgroundImageTintColor = v);
            CopyIfSet(from.unityBackgroundScaleMode, v => to.unityBackgroundScaleMode = v);
            to.unitySliceLeft = from.unitySliceLeft;
            to.unitySliceRight = from.unitySliceRight;
            to.unitySliceTop = from.unitySliceTop;
            to.unitySliceBottom = from.unitySliceBottom;
            to.backgroundColor = from.backgroundColor;
            CopyIfSet(from.color, v => to.color = v);
            CopyIfSet(from.fontSize, v => to.fontSize = v);
            CopyIfSet(from.unityFontDefinition, v => to.unityFontDefinition = v);
            CopyIfSet(from.unityFontStyleAndWeight, v => to.unityFontStyleAndWeight = v);
            CopyIfSet(from.unityTextAlign, v => to.unityTextAlign = v);
            CopyIfSet(from.paddingLeft, v => to.paddingLeft = v);
            CopyIfSet(from.paddingRight, v => to.paddingRight = v);
            CopyIfSet(from.paddingTop, v => to.paddingTop = v);
            CopyIfSet(from.paddingBottom, v => to.paddingBottom = v);
            CopyIfSet(from.marginLeft, v => to.marginLeft = v);
            CopyIfSet(from.marginRight, v => to.marginRight = v);
            CopyIfSet(from.marginTop, v => to.marginTop = v);
            CopyIfSet(from.marginBottom, v => to.marginBottom = v);
            CopyIfSet(from.width, v => to.width = v);
            CopyIfSet(from.height, v => to.height = v);
            CopyIfSet(from.minWidth, v => to.minWidth = v);
            CopyIfSet(from.minHeight, v => to.minHeight = v);
            CopyIfSet(from.borderTopWidth, v => to.borderTopWidth = v);
            CopyIfSet(from.borderRightWidth, v => to.borderRightWidth = v);
            CopyIfSet(from.borderBottomWidth, v => to.borderBottomWidth = v);
            CopyIfSet(from.borderLeftWidth, v => to.borderLeftWidth = v);
            CopyIfSet(from.borderTopColor, v => to.borderTopColor = v);
            CopyIfSet(from.borderRightColor, v => to.borderRightColor = v);
            CopyIfSet(from.borderBottomColor, v => to.borderBottomColor = v);
            CopyIfSet(from.borderLeftColor, v => to.borderLeftColor = v);
            CopyIfSet(from.borderTopLeftRadius, v => to.borderTopLeftRadius = v);
            CopyIfSet(from.borderTopRightRadius, v => to.borderTopRightRadius = v);
            CopyIfSet(from.borderBottomLeftRadius, v => to.borderBottomLeftRadius = v);
            CopyIfSet(from.borderBottomRightRadius, v => to.borderBottomRightRadius = v);
            CopyIfSet(from.opacity, v => to.opacity = v);
        }

        private static void CopyIfSet<T>(StyleEnum<T> value, System.Action<StyleEnum<T>> assign) where T : struct, System.Enum
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private static void CopyIfSet(StyleBackground value, System.Action<StyleBackground> assign)
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private static void CopyIfSet(StyleColor value, System.Action<StyleColor> assign)
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private static void CopyIfSet(StyleLength value, System.Action<StyleLength> assign)
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private static void CopyIfSet(StyleInt value, System.Action<StyleInt> assign)
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private static void CopyIfSet(StyleFloat value, System.Action<StyleFloat> assign)
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private static void CopyIfSet(StyleFontDefinition value, System.Action<StyleFontDefinition> assign)
        {
            if (value.keyword != StyleKeyword.Null && value.keyword != StyleKeyword.Undefined)
                assign(value);
        }

        private void ApplyRadio(string title, string body, bool visible)
        {
            if (radioRoot == null)
                return;

            if (radioTitle != null && !string.IsNullOrEmpty(title))
                radioTitle.text = title;
            if (radioBody != null)
                radioBody.text = body ?? string.Empty;

            bool show = visible && !string.IsNullOrEmpty(body) && !DMUiToolkitMenus.IsOpen;
            radioRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (show)
            {
                string line = string.IsNullOrEmpty(title) ? body : title + ": " + body;
                if (!string.Equals(lastLoggedRadio, line, System.StringComparison.Ordinal))
                {
                    lastLoggedRadio = line;
                    DMGameLog.Add(line, DMGameLogKind.Radio);
                }
            }
            else
            {
                lastLoggedRadio = null;
            }
        }

        public static bool TryDropOnSlot(Vector2 screenPosition, int sourceAbsoluteIndex)
        {
            if (!IsDriving || instance == null)
                return false;
            return instance.DropOnSlot(screenPosition, sourceAbsoluteIndex);
        }

        public static bool TryGetSlotAbsoluteIndex(Vector2 screenPosition, out int absoluteIndex)
        {
            absoluteIndex = -1;
            if (!IsDriving || instance == null)
                return false;
            return instance.TryGetSlotAbsoluteIndexInternal(screenPosition, out absoluteIndex);
        }

        public static bool IsPointerOverHotbarOrTools(Vector2 screenPosition)
        {
            if (!IsDriving || instance == null)
                return false;
            return instance.IsOverHotbarOrTools(screenPosition);
        }

        private void BindHudSlots(VisualElement root)
        {
            UnbindHudSlots();
            if (root == null)
                return;

            hotbarHost = root.Q<VisualElement>("hotbar");
            toolsHost = root.Q<VisualElement>("tools");

            int hotbarCount = 10;
            int toolCount = 2;
            if (inventorySystem != null)
            {
                hotbarCount = Mathf.Min(10, inventorySystem.hotbarSize);
                toolCount = Mathf.Min(2, inventorySystem.toolbarSize);
            }

            for (int i = 0; i < hotbarCount; i++)
                BindOneSlot(root, "hotbar-slot-" + i, "hotbar-icon-" + i, "hotbar-amount-" + i, i, false);

            for (int i = 0; i < toolCount; i++)
                BindOneSlot(root, "tool-slot-" + i, "tool-icon-" + i, "tool-amount-" + i, i, true);

            BindInventoryEvents();
            RefreshSlotIcons();
        }

        private void BindOneSlot(VisualElement root, string slotName, string iconName, string amountName, int localIndex, bool isToolbar)
        {
            VisualElement slot = root.Q<VisualElement>(slotName);
            if (slot == null)
                return;

            VisualElement icon = root.Q<VisualElement>(iconName);
            if (icon == null)
                icon = slot.Q<VisualElement>(iconName);

            Label amount = slot.Q<Label>(amountName);
            if (amount == null)
            {
                amount = new Label();
                amount.name = amountName;
                amount.AddToClassList("dmg-hud-slot-amount");
                amount.pickingMode = PickingMode.Ignore;
                slot.Add(amount);
            }

            slot.pickingMode = PickingMode.Position;
            if (icon != null)
                icon.pickingMode = PickingMode.Ignore;
            amount.pickingMode = PickingMode.Ignore;

            BoundHudSlot bound = new BoundHudSlot
            {
                LocalIndex = localIndex,
                IsToolbar = isToolbar,
                Slot = slot,
                Icon = icon,
                Amount = amount
            };
            slot.userData = bound;
            slot.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
            slot.RegisterCallback<PointerMoveEvent>(OnSlotPointerMove);
            slot.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
            slot.RegisterCallback<PointerCaptureOutEvent>(OnSlotPointerCaptureOut);
            slot.RegisterCallback<PointerEnterEvent>(OnHudSlotPointerEnter);
            slot.RegisterCallback<PointerLeaveEvent>(OnHudSlotPointerLeave);
            boundSlots.Add(bound);
        }

        private void UnbindHudSlots()
        {
            UnbindInventoryEvents();
            ClearSlotDragGhost();
            for (int i = 0; i < boundSlots.Count; i++)
            {
                BoundHudSlot bound = boundSlots[i];
                if (bound == null || bound.Slot == null)
                    continue;

                bound.Slot.UnregisterCallback<PointerDownEvent>(OnSlotPointerDown);
                bound.Slot.UnregisterCallback<PointerMoveEvent>(OnSlotPointerMove);
                bound.Slot.UnregisterCallback<PointerUpEvent>(OnSlotPointerUp);
                bound.Slot.UnregisterCallback<PointerCaptureOutEvent>(OnSlotPointerCaptureOut);
                bound.Slot.UnregisterCallback<PointerEnterEvent>(OnHudSlotPointerEnter);
                bound.Slot.UnregisterCallback<PointerLeaveEvent>(OnHudSlotPointerLeave);
                bound.Slot.userData = null;
            }

            boundSlots.Clear();
            pointerSlot = null;
            slotDragActive = false;
            capturedPointerId = -1;
        }

        private void BindInventoryEvents()
        {
            UnbindInventoryEvents();
            inventorySystem = FindAnyObjectByType<InventorySystem>();
            if (inventorySystem != null)
            {
                equipmentController = inventorySystem.GetComponent<EquipmentController>();
                itemActions = inventorySystem.GetComponent<InventoryItemActions>();
                inventorySystem.OnInventoryChanged += RefreshSlotIcons;
            }
            else
            {
                equipmentController = FindAnyObjectByType<EquipmentController>();
                itemActions = FindAnyObjectByType<InventoryItemActions>();
            }

            if (equipmentController != null)
            {
                equipmentController.OnSelectedHotbarChanged += HandleHudSelectionChanged;
                equipmentController.OnToolbarSelectionChanged += HandleHudToolbarSelectionChanged;
            }
        }

        private void UnbindInventoryEvents()
        {
            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged -= RefreshSlotIcons;

            if (equipmentController != null)
            {
                equipmentController.OnSelectedHotbarChanged -= HandleHudSelectionChanged;
                equipmentController.OnToolbarSelectionChanged -= HandleHudToolbarSelectionChanged;
            }
        }

        private void HandleHudSelectionChanged(int _)
        {
            RefreshSlotIcons();
        }

        private void HandleHudToolbarSelectionChanged()
        {
            RefreshSlotIcons();
        }

        private int GetAbsoluteIndex(BoundHudSlot bound)
        {
            if (bound == null || inventorySystem == null)
                return -1;
            return bound.IsToolbar
                ? inventorySystem.ToolbarStartIndex + bound.LocalIndex
                : inventorySystem.HotbarStartIndex + bound.LocalIndex;
        }

        private void RefreshSlotIcons()
        {
            if (inventorySystem == null)
                BindInventoryEvents();

            for (int i = 0; i < boundSlots.Count; i++)
            {
                BoundHudSlot bound = boundSlots[i];
                if (bound == null || bound.Slot == null)
                    continue;

                int absoluteIndex = GetAbsoluteIndex(bound);
                InventorySystem.InventorySlot slotData = null;
                if (inventorySystem != null && absoluteIndex >= 0 && absoluteIndex < inventorySystem.slots.Count)
                    slotData = inventorySystem.slots[absoluteIndex];

                bool empty = slotData == null || slotData.IsEmpty || slotData.item == null;
                if (bound.Icon != null)
                {
                    if (empty || slotData.item.icon == null)
                        bound.Icon.style.backgroundImage = StyleKeyword.None;
                    else
                        bound.Icon.style.backgroundImage = new StyleBackground(Background.FromSprite(slotData.item.icon));
                }

                if (bound.Amount != null)
                    bound.Amount.text = !empty && slotData.amount > 1 ? slotData.amount.ToString() : string.Empty;

                bool selected = false;
                if (!empty && equipmentController != null && inventorySystem != null)
                {
                    if (inventorySystem.IsToolbarIndex(absoluteIndex))
                        selected = equipmentController.IsSelectedToolbarAbsoluteIndex(absoluteIndex);
                    else if (equipmentController.IsWeaponHotbarSlot(absoluteIndex - inventorySystem.inventorySize))
                        selected = equipmentController.IsActiveWeaponHotbarIndex(absoluteIndex);
                    else
                        selected = absoluteIndex == equipmentController.SelectedSlotIndex;
                }

                if (selected)
                    bound.Slot.AddToClassList("dmg-hud-slot-selected");
                else
                    bound.Slot.RemoveFromClassList("dmg-hud-slot-selected");
            }
        }

        private bool DropOnSlot(Vector2 screenPosition, int sourceAbsoluteIndex)
        {
            if (!TryGetSlotAbsoluteIndexInternal(screenPosition, out int destIndex))
                return false;

            if (destIndex == sourceAbsoluteIndex)
                return true;

            if (inventorySystem == null)
                BindInventoryEvents();
            if (inventorySystem == null)
                return true;

            if (sourceAbsoluteIndex < 0 || sourceAbsoluteIndex >= inventorySystem.slots.Count)
                return true;

            InventorySystem.InventorySlot from = inventorySystem.slots[sourceAbsoluteIndex];
            if (from == null || from.IsEmpty || from.item == null)
                return true;

            if (inventorySystem.CanAcceptItemAt(destIndex, from.item, showLevelToast: true))
                inventorySystem.MoveOrMergeSlots(sourceAbsoluteIndex, destIndex);

            return true;
        }

        private bool TryGetSlotAbsoluteIndexInternal(Vector2 screenPosition, out int absoluteIndex)
        {
            absoluteIndex = -1;
            Vector2 panelPos = ScreenToPanelPosition(screenPosition);
            BoundHudSlot bound = FindBoundSlotAtPanel(panelPos);
            if (bound == null)
                return false;

            absoluteIndex = GetAbsoluteIndex(bound);
            return absoluteIndex >= 0;
        }

        private bool IsOverHotbarOrTools(Vector2 screenPosition)
        {
            Vector2 panelPos = ScreenToPanelPosition(screenPosition);
            if (FindBoundSlotAtPanel(panelPos) != null)
                return true;
            if (hotbarHost != null && hotbarHost.worldBound.Contains(panelPos))
                return true;
            if (toolsHost != null && toolsHost.worldBound.Contains(panelPos))
                return true;
            return false;
        }

        private BoundHudSlot FindBoundSlotAtPanel(Vector2 panelPos)
        {
            for (int i = 0; i < boundSlots.Count; i++)
            {
                BoundHudSlot bound = boundSlots[i];
                if (bound == null || bound.Slot == null)
                    continue;
                if (bound.Slot.worldBound.Contains(panelPos))
                    return bound;
            }

            return null;
        }

        private Vector2 ScreenToPanelPosition(Vector2 screenPosition)
        {
            IPanel panel = hudRoot != null ? hudRoot.panel : null;
            if (panel == null && document != null && document.rootVisualElement != null)
                panel = document.rootVisualElement.panel;
            if (panel == null)
                return screenPosition;
            return RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        }


        private static Vector2 CurrentPointerScreenPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (UnityEngine.InputSystem.Pointer.current != null)
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            return Vector2.zero;
        }

        private void OnSlotPointerDown(PointerDownEvent evt)
        {
            BoundHudSlot bound = (evt.currentTarget as VisualElement)?.userData as BoundHudSlot;
            if (bound == null)
                return;

            pointerSlot = bound;
            pointerDownPanelPos = (Vector2)evt.position;
            lastSlotPointerPanelPos = (Vector2)evt.position;
            slotDragActive = false;
            capturedPointerId = evt.pointerId;
            bound.Slot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnSlotPointerMove(PointerMoveEvent evt)
        {
            if (pointerSlot == null)
                return;

            lastSlotPointerPanelPos = (Vector2)evt.position;
            if (slotDragActive)
            {
                PositionSlotDragGhost((Vector2)evt.position);
                return;
            }

            if ((evt.pressedButtons & 1) == 0)
                return;

            Vector2 delta = (Vector2)evt.position - pointerDownPanelPos;
            if (delta.sqrMagnitude < DragThresholdPx * DragThresholdPx)
                return;

            BeginSlotDrag(pointerSlot, (Vector2)evt.position);
        }

        private void OnSlotPointerUp(PointerUpEvent evt)
        {
            BoundHudSlot bound = pointerSlot;
            Vector2 panelPos = (Vector2)evt.position;
            int button = evt.button;
            bool dragging = slotDragActive;
            ReleaseSlotPointerCapture(bound);

            if (dragging)
            {
                CompleteSlotDrag(panelPos);
                return;
            }

            if (bound != null)
                HandleSlotClick(bound, button);
        }

        private void OnSlotPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (pointerSlot == null)
                return;

            BoundHudSlot bound = pointerSlot;
            bool dragging = slotDragActive;
            Vector2 panelPos = lastSlotPointerPanelPos;
            pointerSlot = null;
            slotDragActive = false;
            capturedPointerId = -1;

            if (dragging)
                CompleteSlotDrag(panelPos);
        }

        private void ReleaseSlotPointerCapture(BoundHudSlot bound)
        {
            int id = capturedPointerId;
            capturedPointerId = -1;
            pointerSlot = null;
            slotDragActive = false;
            if (bound != null && bound.Slot != null && id >= 0 && bound.Slot.HasPointerCapture(id))
                bound.Slot.ReleasePointer(id);
        }

        private void BeginSlotDrag(BoundHudSlot bound, Vector2 panelPos)
        {
            if (bound == null || inventorySystem == null)
                return;

            int absoluteIndex = GetAbsoluteIndex(bound);
            if (absoluteIndex < 0 || absoluteIndex >= inventorySystem.slots.Count)
                return;

            InventorySystem.InventorySlot slotData = inventorySystem.slots[absoluteIndex];
            if (slotData == null || slotData.IsEmpty || slotData.item == null || slotData.item.icon == null)
                return;

            slotDragActive = true;
            slotDragSourceIndex = absoluteIndex;
            ClearSlotDragGhost();

            slotDragGhost = new VisualElement();
            slotDragGhost.name = "dmg-hud-drag-ghost";
            slotDragGhost.pickingMode = PickingMode.Ignore;
            slotDragGhost.style.position = Position.Absolute;
            float size = 40f;
            if (bound.Icon != null)
            {
                float resolved = bound.Icon.resolvedStyle.width;
                if (resolved > 1f)
                    size = resolved;
            }

            slotDragGhost.style.width = size;
            slotDragGhost.style.height = size;
            slotDragGhost.style.backgroundImage = new StyleBackground(Background.FromSprite(slotData.item.icon));
            slotDragGhost.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            slotDragGhost.style.opacity = 0.75f;
            VisualElement ghostParent = hudRoot != null ? hudRoot : bound.Slot.parent;
            ghostParent?.Add(slotDragGhost);
            PositionSlotDragGhost(panelPos);

            if (bound.Icon != null)
                bound.Icon.style.opacity = 0.35f;
        }

        private void PositionSlotDragGhost(Vector2 panelPos)
        {
            if (slotDragGhost == null)
                return;

            VisualElement parent = slotDragGhost.parent != null ? slotDragGhost.parent : hudRoot;
            Vector2 local = panelPos;
            if (parent != null)
                local = parent.WorldToLocal(panelPos);

            float width = slotDragGhost.resolvedStyle.width;
            float height = slotDragGhost.resolvedStyle.height;
            if (width <= 0f)
                width = 40f;
            if (height <= 0f)
                height = 40f;

            slotDragGhost.style.left = local.x - width * 0.5f;
            slotDragGhost.style.top = local.y - height * 0.5f;
        }

        private void CompleteSlotDrag(Vector2 panelPos)
        {
            int source = slotDragSourceIndex;
            ClearSlotDragGhost();
            RefreshSlotIcons();
            slotDragSourceIndex = -1;

            if (inventorySystem == null || source < 0)
                return;

            Vector2 screenPos = CurrentPointerScreenPosition();

            BoundHudSlot destBound = FindBoundSlotAtPanel(panelPos);
            if (destBound != null)
            {
                int destIndex = GetAbsoluteIndex(destBound);
                if (destIndex >= 0 && destIndex != source)
                {
                    InventorySystem.InventorySlot from = inventorySystem.slots[source];
                    if (from != null && !from.IsEmpty && from.item != null
                        && inventorySystem.CanAcceptItemAt(destIndex, from.item, showLevelToast: true))
                        inventorySystem.MoveOrMergeSlots(source, destIndex);
                }

                return;
            }

            if (IsOverHotbarOrToolsPanel(panelPos))
                return;

            InventorySlotUI ugui = FindUguiSlotUnderScreen(screenPos, out _);
            if (ugui != null && ugui.slotIndex != source && !ugui.IsLocked)
            {
                InventorySystem.InventorySlot from = inventorySystem.slots[source];
                if (from != null && !from.IsEmpty && from.item != null
                    && inventorySystem.CanAcceptItemAt(ugui.slotIndex, from.item, showLevelToast: true))
                    inventorySystem.MoveOrMergeSlots(source, ugui.slotIndex);
                return;
            }

            if (DMUiToolkitMenus.TryDropOnInventorySlot(screenPos, source))
                return;
            if (DMUiToolkitMenus.IsPointerOverInventory(screenPos))
                return;

            if (IsOverInventoryOrToolbarUi(screenPos))
                return;

            if (itemActions != null)
                itemActions.TryDrop(source);
            else
                inventorySystem.DropItemAt(source);
        }

        private bool IsOverHotbarOrToolsPanel(Vector2 panelPos)
        {
            if (hotbarHost != null && hotbarHost.worldBound.Contains(panelPos))
                return true;
            if (toolsHost != null && toolsHost.worldBound.Contains(panelPos))
                return true;
            return false;
        }

        private void ClearSlotDragGhost()
        {
            if (slotDragGhost != null)
            {
                slotDragGhost.RemoveFromHierarchy();
                slotDragGhost = null;
            }
        }


        private void OnHudSlotPointerEnter(PointerEnterEvent evt)
        {
            BoundHudSlot bound = (evt.currentTarget as VisualElement)?.userData as BoundHudSlot;
            if (bound == null || inventorySystem == null)
                return;

            int absoluteIndex = GetAbsoluteIndex(bound);
            if (absoluteIndex < 0 || absoluteIndex >= inventorySystem.slots.Count)
                return;

            InventorySystem.InventorySlot slotData = inventorySystem.slots[absoluteIndex];
            if (slotData == null || slotData.IsEmpty || slotData.item == null)
                return;

            DMUiToolkitWorldMenus.TryShowItemTooltip(slotData.item, slotData.amount, CurrentPointerScreenPosition());
        }

        private void OnHudSlotPointerLeave(PointerLeaveEvent evt)
        {
            DMUiToolkitWorldMenus.HideItemTooltip();
        }

        private void HandleSlotClick(BoundHudSlot bound, int button)
        {
            if (bound == null || inventorySystem == null)
                return;

            int absoluteIndex = GetAbsoluteIndex(bound);
            if (absoluteIndex < 0 || absoluteIndex >= inventorySystem.slots.Count)
                return;

            InventorySystem.InventorySlot slotData = inventorySystem.slots[absoluteIndex];
            if (slotData == null || slotData.IsEmpty || slotData.item == null)
                return;

            if (button == 1)
            {
                itemActions ??= inventorySystem.GetComponent<InventoryItemActions>();
                DMUiToolkitContext.TryShow(absoluteIndex, CurrentPointerScreenPosition(), itemActions);
                return;
            }

            if (button != 0)
                return;

            if (UiInputGuard.BlocksGameplayEquipmentInput)
                return;

            GameAudioManager.Instance?.PlayInventoryItemClick();

            if (equipmentController != null && slotData.item.IsEquippable)
            {
                if (inventorySystem.IsToolbarIndex(absoluteIndex))
                {
                    equipmentController.SelectToolbarSlot(inventorySystem.ToToolbarSlotIndex(absoluteIndex), allowToggleOff: true);
                    return;
                }

                if (inventorySystem.IsHotbarIndex(absoluteIndex))
                {
                    int hotbarIndex = absoluteIndex - inventorySystem.inventorySize;
                    if (equipmentController.IsWeaponHotbarSlot(hotbarIndex))
                    {
                        int weaponSlot = equipmentController.GetWeaponSlotIndexForHotbar(hotbarIndex);
                        if (weaponSlot >= 0)
                            equipmentController.SelectWeaponSlot(weaponSlot);
                    }
                    else
                        equipmentController.SelectInventorySlot(absoluteIndex);
                    return;
                }
            }

            if (itemActions != null)
                itemActions.TryUse(absoluteIndex);
            else
                inventorySystem.UseItemAt(absoluteIndex);
        }

        private static InventorySlotUI FindUguiSlotUnderScreen(Vector2 screenPosition, out bool hitAnyUi)
        {
            hitAnyUi = false;
            if (EventSystem.current == null)
                return null;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            hitAnyUi = results.Count > 0;

            for (int i = 0; i < results.Count; i++)
            {
                InventorySlotUI slot = results[i].gameObject.GetComponentInParent<InventorySlotUI>();
                if (slot != null)
                    return slot;
            }

            return null;
        }

        private static bool IsOverInventoryOrToolbarUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                GameObject hit = results[i].gameObject;
                if (hit.GetComponentInParent<InventorySlotUI>() != null)
                    return true;
                if (hit.GetComponentInParent<ToolBarUI>() != null)
                    return true;

                InventoryUI inventoryUi = hit.GetComponentInParent<InventoryUI>();
                if (inventoryUi == null)
                    continue;

                if (inventoryUi.inventoryPanel != null
                    && (hit == inventoryUi.inventoryPanel || hit.transform.IsChildOf(inventoryUi.inventoryPanel.transform)))
                    return true;

                if (inventoryUi.hotbarParent != null
                    && (hit.transform == inventoryUi.hotbarParent || hit.transform.IsChildOf(inventoryUi.hotbarParent)))
                    return true;
            }

            return false;
        }

        private sealed class BoundHudSlot
        {
            public int LocalIndex;
            public bool IsToolbar;
            public VisualElement Slot;
            public VisualElement Icon;
            public Label Amount;
        }

        private void HideUguiCounterparts()
        {
            if (uguiHidden)
                return;

            CondensedSurvivalStatsHud statsHud = FindAnyObjectByType<CondensedSurvivalStatsHud>(FindObjectsInactive.Include);
            if (statsHud != null && statsHud.gameObject.activeSelf)
                statsHud.gameObject.SetActive(false);

            InventoryUI inventoryUi = FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
            if (inventoryUi != null && inventoryUi.hotbarParent != null && inventoryUi.hotbarParent.gameObject.activeSelf)
                inventoryUi.hotbarParent.gameObject.SetActive(false);

            ToolBarUI toolbar = FindAnyObjectByType<ToolBarUI>(FindObjectsInactive.Include);
            toolbar?.SetGameplayVisible(false);

            ExpeditionPioneerHudUI expeditionHud = FindAnyObjectByType<ExpeditionPioneerHudUI>(FindObjectsInactive.Include);
            expeditionHud?.SetGameplayVisible(false);

            uguiHidden = true;
        }

        private void RestoreUguiCounterparts()
        {
            if (!uguiHidden)
                return;

            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
            {
                CondensedSurvivalStatsHud statsHud = FindAnyObjectByType<CondensedSurvivalStatsHud>(FindObjectsInactive.Include);
                if (statsHud != null && GameSession.HasStarted && !MainMenuController.BlocksGameplayHud)
                    statsHud.gameObject.SetActive(true);

                ExpeditionPioneerHudUI expeditionHud = FindAnyObjectByType<ExpeditionPioneerHudUI>(FindObjectsInactive.Include);
                expeditionHud?.SetGameplayVisible(true);
            }

            uguiHidden = false;
        }
    }
}
