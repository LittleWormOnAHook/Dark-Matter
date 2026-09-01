using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Project.Achievements;
using Project.Core;
using Project.Pioneers;
using Project.Player;
using Project.Quests;
using Project.Progression;
using Project.Survival;
using Project.Survival.Exposure;

namespace Project.UI
{
    public partial class UIManager : MonoBehaviour
    {
        [Header("Survival Stats UI")]
        public Slider healthSlider;
        public Slider thermalSlider;
        public Slider energySlider;
        public Slider staminaSlider;
        public Slider oxygenSlider;

        public TextMeshProUGUI healthText;
        public TextMeshProUGUI thermalText;
        public TextMeshProUGUI energyText;
        public TextMeshProUGUI staminaText;
        public TextMeshProUGUI oxygenText;

        [Header("Currency")]
        [UnityEngine.Serialization.FormerlySerializedAs("piBalanceText")]
        public TextMeshProUGUI acBalanceText;

        [Header("Interaction Prompt")]
        public TextMeshProUGUI interactionPrompt;

        [Header("Popups")]
        [UnityEngine.Serialization.FormerlySerializedAs("piRewardPopupPrefab")]
        public GameObject acRewardPopupPrefab;
        public Transform popupParent;

        [Header("Combat UI")]
        public GameObject floatingDamagePrefab;
        public Transform combatPopupParent;

        [Header("Layout")]
        [Tooltip("When disabled, AC balance and interaction prompt rects are not repositioned at Start.")]
        [SerializeField] private bool applyRuntimeHudLayout = true;

        private SurvivalStats survivalStats;
        private Camera worldCamera;
        private float aetherCredits;
        private PlayerProgressionManager trackedProgression;

        private readonly System.Collections.Generic.List<(InputAction action, System.Action<InputAction.CallbackContext> handler)> journalInputBindings =
            new System.Collections.Generic.List<(InputAction, System.Action<InputAction.CallbackContext>)>(12);

        private int lastHealthDisplay = int.MinValue;
        private int lastEnergyDisplay = int.MinValue;
        private int lastStaminaDisplay = int.MinValue;
        private int lastOxygenDisplay = int.MinValue;
        private string lastThermalTempText;
        private string lastThermalStatusText;
        private readonly Dictionary<Slider, SurvivalSliderCache> survivalSliderCache =
            new Dictionary<Slider, SurvivalSliderCache>(8);

        private struct SurvivalSliderCache
        {
            public RectTransform RingFill;
            public Image FilledImage;
            public CircularProgressBar Circular;
            public bool Resolved;
        }

        private void Awake()
        {
            GameSession.ResetSession();
            MainCanvasFlow.SanitizeCanvasHost(GetComponent<Canvas>());

            ResolveSurvivalUiReferences();
            BindSurvivalStats();
            EnsureSurvivalPanelBinder();
            EnsureMapUi();
            EnsureToolBarUi();
            EnsureShiftHudBootstrap();
            EnsurePickupProximityDotUi();
            EnsureWorldInteractionDotUi();
            EnsurePickupAimReticleUi();
            EnsureJournalPanelUi();
            EnsureQuestManager();
            EnsureAchievementSystems();
            EnsureCraftingUi();
            EnsurePeakScreenUi();
            EnsureProgressionHud();
            BindJournalInputActions();

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
            {
                SetAetherCredits(roster.AetherCredits);
            }

            if (!GameSession.HasStarted)
                MainMenuController.EnsureExists();
        }

        private void OnEnable()
        {
            GameSession.GameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= HandleGameStarted;
        }

        private void HandleGameStarted()
        {
            BindJournalInputActions();
            StartCoroutine(RefreshGameplayHudNextFrame());
        }

        private IEnumerator RefreshGameplayHudNextFrame()
        {
            yield return null;
            MainCanvasFlow.Refresh();
        }



        public void OnToggleJournal(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.JournalQuest, journalHotkey: true))
                return;

            GetJournalPanel()?.TryToggleJournal();
        }

        public void OnToggleCraft(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Recipes))
                return;

            GetJournalPanel()?.OpenToBlueprintsTab();
        }

        public void OnToggleBlueprints(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            // Keyboard B is shared: tap = Blueprints (ToolBarUI), hold = binoculars.
            // Ignore the Input System performed pulse on keyboard B so hold can resolve first.
            if (context.control != null && context.control.device is Keyboard)
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Recipes);
        }

        /// <summary>Opens/toggles Blueprints from a confirmed keyboard B tap (not hold).</summary>
        public void ToggleBlueprintsFromTap()
        {
            if (!GameSession.HasStarted)
                return;

            JournalPanelUI journal = GetJournalPanel();
            if (journal != null && journal.IsOpen && journal.ActiveJournalWindow == JournalWindowId.Recipes)
                return;

            if (DMUiToolkitMenus.TrySwitchJournalTab(JournalWindowId.Recipes))
                return;

            journal?.SwitchToTab(JournalWindowId.Recipes);
        }

        /// <summary>Obsolete alias for <see cref="OnToggleBlueprints"/>.</summary>
        public void OnToggleRecipes(InputAction.CallbackContext context) => OnToggleBlueprints(context);

        public void OnTogglePioneers(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pioneers))
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Pioneers);
        }

        public void OnToggleCharacter(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Character))
                return;

            GetJournalPanel()?.OpenToCharacterTab();
        }

        public void OnToggleSkills(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Skills))
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Skills);
        }

        public void OnToggleEchoes(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Echoes))
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Echoes);
        }

        public void OnToggleAchievements(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Achievements))
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Achievements);
        }

        public void OnToggleInventory(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Inventory))
                return;

            GetJournalPanel()?.OpenToInventoryTab();
        }

        public void OnToggleMap(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Map))
                return;

            if (DMUiToolkitConfig.IsEnabled && DMUiToolkitBootstrap.IsRootActive)
            {
                GetJournalPanel()?.TryToggleMapTab();
                return;
            }

            FindAnyObjectByType<MapUI>(FindObjectsInactive.Include)?.OnToggleMap(context);
        }

        public void OnTogglePets(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pet))
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Pet);
        }

        public void OnUiCancel(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            GameplayKeyboardShortcuts.HandleEscapePressed();
        }

        private JournalPanelUI GetJournalPanel()
        {
            JournalPanelUI journal = GetComponent<JournalPanelUI>();
            if (journal == null)
                journal = gameObject.AddComponent<JournalPanelUI>();
            return journal;
        }

        private void BindJournalInputActions()
        {
            UnbindJournalInputActions();

            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            if (playerInput == null || playerInput.actions == null)
                return;

            BindJournalAction(playerInput, "Journal", OnToggleJournal);
            BindJournalAction(playerInput, "Inventory", OnToggleInventory);
            BindJournalAction(playerInput, "Map", OnToggleMap);
            BindJournalAction(playerInput, "Craft", OnToggleCraft);
            BindJournalAction(playerInput, "Blueprints", OnToggleBlueprints);
            BindJournalAction(playerInput, "Pioneers", OnTogglePioneers);
            BindJournalAction(playerInput, "Skills", OnToggleSkills);
            BindJournalAction(playerInput, "Echoes", OnToggleEchoes);
            BindJournalAction(playerInput, "Achievements", OnToggleAchievements);
            BindJournalAction(playerInput, "Character", OnToggleCharacter);
            BindJournalAction(playerInput, "Pets", OnTogglePets);

            InputAction cancel = playerInput.actions.FindAction("Cancel", false);
            if (cancel != null)
            {
                cancel.performed -= OnUiCancel;
                cancel.performed += OnUiCancel;
                journalInputBindings.Add((cancel, OnUiCancel));
            }
        }

        private void BindJournalAction(
            PlayerInput playerInput,
            string actionName,
            System.Action<InputAction.CallbackContext> handler)
        {
            InputAction action = playerInput.actions.FindAction(actionName, false);
            if (action == null)
                return;

            action.performed -= handler;
            action.performed += handler;
            journalInputBindings.Add((action, handler));
        }

        private void UnbindJournalInputActions()
        {
            for (int i = 0; i < journalInputBindings.Count; i++)
            {
                (InputAction action, System.Action<InputAction.CallbackContext> handler) entry = journalInputBindings[i];
                if (entry.action != null)
                    entry.action.performed -= entry.handler;
            }

            journalInputBindings.Clear();
        }











        private IEnumerator Start()
        {
            ResolveSurvivalUiReferences();
            BindSurvivalStats();
            EnsureOxygenDeprivationFx();
            UpdateSurvivalUI();
            SetCurrencyHudVisible(false);
            if (interactionPrompt != null) interactionPrompt.gameObject.SetActive(false);
            ConfigureInteractionPromptPosition();
            EnsureGameplayUiHelpers();
            EnsureProgressionLevelUpFeedback();
            worldCamera = Camera.main;
            EnsureCombatUiReady();

            yield return null;

            if (!GameSession.HasStarted)
                MainCanvasFlow.Refresh();
        }



        private void HandleProgressionLevelUp(int newLevel, int levelsGained)
        {
            ShowLevelUpPopup(newLevel, levelsGained);
        }

        private void ConfigureInteractionPromptPosition()
        {
            EnsureInteractionPrompt();
            ApplyInteractionPromptLayout();
        }


        private void ApplyInteractionPromptLayout()
        {
            if (interactionPrompt == null)
                return;

            RectTransform promptRect = interactionPrompt.rectTransform;
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0f, 96f);
            promptRect.sizeDelta = new Vector2(760f, 56f);

            interactionPrompt.fontSize = 28f;
            interactionPrompt.alignment = TextAlignmentOptions.Center;
            interactionPrompt.textWrappingMode = TextWrappingModes.NoWrap;
            interactionPrompt.overflowMode = TextOverflowModes.Overflow;
            interactionPrompt.color = DarkMatterGenesisUiPalette.InteractionPromptText;
        }




        public void ShowFloatingDamage(float damage, Vector3 worldPosition)
        {
            WorldFloatingDamageNumber.Spawn(damage, worldPosition);
        }

        private Transform GetCombatPopupParent()
        {
            if (combatPopupParent != null)
                return combatPopupParent;

            if (popupParent != null)
                return popupParent;

            Canvas canvas = GetComponent<Canvas>();
            return canvas != null ? canvas.transform : transform;
        }

        private void BindSurvivalStats()
        {
            ResolveSurvivalUiReferences();

            SurvivalStats found = null;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                found = player.GetComponent<SurvivalStats>();

            if (found == null)
                found = FindAnyObjectByType<SurvivalStats>();

            if (found == null)
                return;

            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;

            survivalStats = found;
            survivalStats.OnStatsChanged -= UpdateSurvivalUI;
            survivalStats.OnStatsChanged += UpdateSurvivalUI;
        }



        public void SyncSurvivalBars()
        {
            BindSurvivalStats();
            UpdateSurvivalUI();
        }

        private void UpdateSurvivalUI()
        {
            if (survivalStats == null) return;

            SetSliderValue(healthSlider, survivalStats.CurrentHealth / survivalStats.maxHealth);
            if (thermalSlider != null && thermalSlider.gameObject.activeInHierarchy)
                SetSliderValue(thermalSlider, survivalStats.GetDisplayTemperatureGaugeNormalized());
            SetSliderValue(energySlider, survivalStats.CurrentEnergy / survivalStats.maxEnergy);
            SetSliderValue(staminaSlider, survivalStats.CurrentStamina / survivalStats.maxStamina);
            SetSliderValue(oxygenSlider, survivalStats.GetOxygenNormalized());

            int healthDisplay = Mathf.CeilToInt(survivalStats.CurrentHealth);
            if (healthText != null && healthDisplay != lastHealthDisplay)
            {
                lastHealthDisplay = healthDisplay;
                healthText.text = FormatStatValue(survivalStats.CurrentHealth, "Health");
            }

            if (thermalText != null)
            {
                ExposureStatusSnapshot snapshot = ExposureStatusService.Current;
                string tempText;
                string statusText;
                if (snapshot != null && !ReferenceEquals(snapshot, ExposureStatusSnapshot.Empty))
                {
                    tempText = snapshot.TemperatureText;
                    statusText = snapshot.ThermalStatusLabel;
                }
                else
                {
                    tempText = ExposureTemperatureDisplay.FormatFahrenheit(survivalStats.GetDisplayTemperatureFahrenheit());
                    statusText = survivalStats.GetThermalStatusLabel();
                }

                if (tempText != lastThermalTempText || statusText != lastThermalStatusText)
                {
                    lastThermalTempText = tempText;
                    lastThermalStatusText = statusText;
                    thermalText.text = string.Concat(tempText, "  ", statusText);
                }
            }

            int energyDisplay = Mathf.CeilToInt(survivalStats.CurrentEnergy);
            if (energyText != null && energyDisplay != lastEnergyDisplay)
            {
                lastEnergyDisplay = energyDisplay;
                energyText.text = FormatStatValue(survivalStats.CurrentEnergy, "Energy");
            }

            int staminaDisplay = Mathf.CeilToInt(survivalStats.CurrentStamina);
            if (staminaText != null && staminaDisplay != lastStaminaDisplay)
            {
                lastStaminaDisplay = staminaDisplay;
                staminaText.text = FormatStatValue(survivalStats.CurrentStamina, "Stamina");
            }

            if (oxygenText != null)
            {
                if (CondensedSurvivalStatsHud.IsActive)
                    oxygenText.gameObject.SetActive(true);

                int oxygenDisplay = Mathf.Max(0, Mathf.CeilToInt(survivalStats.CurrentOxygen));
                if (oxygenDisplay != lastOxygenDisplay)
                {
                    lastOxygenDisplay = oxygenDisplay;
                    oxygenText.text = FormatOxygenValue(survivalStats.CurrentOxygen);
                }
            }
        }

        private void ResolveSurvivalUiReferences()
        {
            Transform panel = GetSurvivalStatsPanelTransform();
            if (panel == null)
                return;

            healthSlider ??= FindRowSlider(panel, "HealthRow");
            thermalSlider ??= FindRowSlider(panel, "ThermalRow");
            energySlider ??= FindRowSlider(panel, "EnergyRow");
            staminaSlider ??= FindRowSlider(panel, "StaminaRow");
            oxygenSlider ??= FindRowSlider(panel, "OxygenRow");

            healthText ??= FindRowLabel(panel, "HealthRow");
            thermalText ??= FindRowLabel(panel, "ThermalRow");
            energyText ??= FindRowLabel(panel, "EnergyRow");
            staminaText ??= FindRowLabel(panel, "StaminaRow");
            oxygenText ??= FindRowLabel(panel, "OxygenRow");
        }

        private Transform GetSurvivalStatsPanelTransform()
        {
            if (healthSlider != null)
            {
                Transform current = healthSlider.transform;
                while (current != null)
                {
                    if (current.name == "SurvivalStatsPanel")
                        return current;

                    current = current.parent;
                }
            }

            Transform panel = FindDeepChild(transform, "SurvivalStatsPanel");
            if (panel != null)
                return panel;

            Canvas canvas = GetComponent<Canvas>();
            return canvas != null ? FindDeepChild(canvas.transform, "SurvivalStatsPanel") : null;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Slider FindRowSlider(Transform panel, string rowName)
        {
            Transform row = panel.Find(rowName);
            return row != null ? row.GetComponentInChildren<Slider>(true) : null;
        }

        private static TextMeshProUGUI FindRowLabel(Transform panel, string rowName)
        {
            Transform row = panel.Find(rowName);
            return row != null ? row.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        }

        private static string FormatOxygenValue(float displaySeconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(displaySeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string formatted = $"{minutes:00}:{seconds:00}";

            if (CondensedSurvivalStatsHud.IsActive)
                return formatted;

            return $"Oxygen: {formatted}";
        }

        private static string FormatStatValue(float value, string statName)
        {
            if (CondensedSurvivalStatsHud.IsActive)
                return Mathf.CeilToInt(value).ToString();

            return $"{statName}: {Mathf.Ceil(value)}";
        }

        private void SetSliderValue(Slider slider, float normalizedValue)
        {
            if (slider == null)
                return;

            float clamped = Mathf.Clamp01(normalizedValue);
            slider.SetValueWithoutNotify(clamped);

            if (!survivalSliderCache.TryGetValue(slider, out SurvivalSliderCache cache) || !cache.Resolved)
            {
                cache = ResolveSurvivalSliderCache(slider);
                survivalSliderCache[slider] = cache;
            }

            if (cache.RingFill != null)
            {
                CondensedSurvivalStatsHud.ApplyBarFill(slider, clamped, cache.RingFill);
                return;
            }

            if (cache.FilledImage != null)
            {
                cache.FilledImage.fillAmount = clamped;
                return;
            }

            if (cache.Circular != null)
                cache.Circular.UpdateRadialFill(clamped);
        }

        private static SurvivalSliderCache ResolveSurvivalSliderCache(Slider slider)
        {
            SurvivalSliderCache cache = new SurvivalSliderCache { Resolved = true };
            Transform fillTransform = slider.transform.Find("RingFill");
            if (fillTransform is RectTransform ringFill)
            {
                cache.RingFill = ringFill;
                return cache;
            }

            if (fillTransform != null && fillTransform.TryGetComponent(out Image fillImage)
                && fillImage.type == Image.Type.Filled
                && fillImage.fillMethod == Image.FillMethod.Horizontal)
            {
                cache.FilledImage = fillImage;
                return cache;
            }

            cache.Circular = slider.GetComponent<CircularProgressBar>();
            return cache;
        }













        public void ShowInteractionPrompt(string message)
        {
            if (DMUiToolkitHud.IsDriving)
            {
                DMUiToolkitHud.ShowPrompt(message);
                if (interactionPrompt != null)
                    interactionPrompt.gameObject.SetActive(false);
                return;
            }

            EnsureInteractionPrompt();
            if (interactionPrompt == null)
                return;

            ApplyInteractionPromptLayout();
            interactionPrompt.text = message;
            interactionPrompt.gameObject.SetActive(true);
            interactionPrompt.transform.SetAsLastSibling();
            DMGameLog.Add(message, DMGameLogKind.Prompt);
        }

        public void ShowTimedInteractionPrompt(string message, float durationSeconds = 2.5f)
        {
            ShowInteractionPrompt(message);
            CancelInvoke(nameof(HideInteractionPrompt));
            Invoke(nameof(HideInteractionPrompt), durationSeconds);
        }

        public void ShowPetFetchMessage(string itemName)
        {
            ShowInteractionPrompt($"Your fox found: {itemName}!");
            CancelInvoke(nameof(HideInteractionPrompt));
            Invoke(nameof(HideInteractionPrompt), 2.5f);
        }

        public void HideInteractionPrompt()
        {
            DMUiToolkitHud.HidePrompt();
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);
        }




        public void RefreshSurvivalDisplay()
        {
            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;

            survivalStats = null;
            ResolveSurvivalUiReferences();
            BindSurvivalStats();
            UpdateSurvivalUI();
        }







        private void OnDestroy()
        {
            if (trackedProgression != null)
                trackedProgression.OnLevelUp -= HandleProgressionLevelUp;

            UnbindJournalInputActions();

            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;
        }
    }
}