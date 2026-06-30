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

namespace Project.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Survival Stats UI")]
        public Slider healthSlider;
        public Slider energySlider;
        public Slider staminaSlider;
        public Slider oxygenSlider;

        public TextMeshProUGUI healthText;
        public TextMeshProUGUI energyText;
        public TextMeshProUGUI staminaText;
        public TextMeshProUGUI oxygenText;

        [Header("Currency")]
        public TextMeshProUGUI piBalanceText;

        [Header("Interaction Prompt")]
        public TextMeshProUGUI interactionPrompt;

        [Header("Popups")]
        public GameObject piRewardPopupPrefab;
        public Transform popupParent;

        [Header("Combat UI")]
        public GameObject floatingDamagePrefab;
        public Transform combatPopupParent;

        [Header("Layout")]
        [Tooltip("When disabled, Pi balance and interaction prompt rects are not repositioned at Start.")]
        [SerializeField] private bool applyRuntimeHudLayout = true;

        private SurvivalStats survivalStats;
        private Camera worldCamera;
        private float aetherCredits;
        private float piWalletBalance;
        private float piBalance;
        private PlayerProgressionManager trackedProgression;

        private InputAction characterToggleAction;

        private void Awake()
        {
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
            BindCharacterTabInput();

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
            {
                SetAetherCredits(roster.AetherCredits);
                SetPiWalletBalance(roster.PiWalletBalance);
            }
        }

        private void EnsureJournalPanelUi()
        {
            if (GetComponent<JournalPanelUI>() == null)
                gameObject.AddComponent<JournalPanelUI>();

        }

        private void EnsureProgressionHud()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                if (player.GetComponent<PlayerProgressionManager>() == null)
                    player.AddComponent<PlayerProgressionManager>();
                if (player.GetComponent<ProgressionStatScaler>() == null)
                    player.AddComponent<ProgressionStatScaler>();
                return;
            }

            if (GetComponent<PlayerProgressionManager>() == null)
                gameObject.AddComponent<PlayerProgressionManager>();
        }

        public void OnToggleJournal(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.TryToggleJournal();
        }

        public void OnToggleCraft(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Craft);
        }

        public void OnToggleRecipes(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Recipes);
        }

        public void OnTogglePioneers(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Pioneers);
        }

        public void OnToggleCharacter(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.OpenToCharacterTab();
        }

        public void OnToggleSkills(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Skills);
        }

        public void OnToggleEchoes(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            GetJournalPanel()?.TryToggleTab(JournalWindowId.Echoes);
        }

        private JournalPanelUI GetJournalPanel()
        {
            JournalPanelUI journal = GetComponent<JournalPanelUI>();
            if (journal == null)
                journal = gameObject.AddComponent<JournalPanelUI>();
            return journal;
        }

        private void BindCharacterTabInput()
        {
            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput == null)
                return;

            characterToggleAction = playerInput.actions.FindAction("Character", false);
            if (characterToggleAction == null)
                return;

            characterToggleAction.performed -= OnToggleCharacter;
            characterToggleAction.performed += OnToggleCharacter;
        }

        private void EnsureQuestManager()
        {
            QuestManager.EnsureExists();
        }

        private void EnsureAchievementSystems()
        {
            AchievementManager.EnsureExists();
            if (GetComponent<AchievementProgressBridge>() == null)
                gameObject.AddComponent<AchievementProgressBridge>();
        }

        private void EnsureCraftingUi()
        {
            if (GetComponent<CraftingUI>() == null)
                gameObject.AddComponent<CraftingUI>();
        }

        private void EnsurePeakScreenUi()
        {
            EnvironmentalCrisisHudMode.EnsureExists(transform);
        }

        private void EnsurePickupProximityDotUi()
        {
            if (GetComponent<PickupProximityDotUI>() == null)
                gameObject.AddComponent<PickupProximityDotUI>();
        }

        private void EnsureWorldInteractionDotUi()
        {
            if (GetComponent<WorldInteractionDotUI>() == null)
                gameObject.AddComponent<WorldInteractionDotUI>();
        }

        private void EnsurePickupAimReticleUi()
        {
            if (GetComponent<PickupAimReticleUI>() == null)
                gameObject.AddComponent<PickupAimReticleUI>();
        }

        private void EnsureShiftHudBootstrap()
        {
            if (GetComponent<ShiftHudBootstrap>() == null)
                gameObject.AddComponent<ShiftHudBootstrap>();
        }

        private void EnsureMapUi()
        {
            if (GetComponent<MapUI>() == null)
                gameObject.AddComponent<MapUI>();
        }

        private void EnsureToolBarUi()
        {
            if (GetComponent<ToolBarUI>() == null)
                gameObject.AddComponent<ToolBarUI>();
        }

        private void Start()
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
        }

        private void EnsureGameplayUiHelpers()
        {
            PickupToastUI.EnsureExists(transform);
            XpToastUI.EnsureExists(transform);
            AchievementUnlockPopupUI.EnsureExists(transform);
            AchievementProgressBridge.EnsureExists();
            QuestGiverDialogUI.EnsureExists(transform);
            ActiveQuestHudUI.EnsureExists(transform);
            UiFrontLayer.Get(transform);
        }

        private void EnsureProgressionLevelUpFeedback()
        {
            trackedProgression = PlayerProgressionManager.EnsureExists();
            if (trackedProgression == null)
                return;

            trackedProgression.OnLevelUp -= HandleProgressionLevelUp;
            trackedProgression.OnLevelUp += HandleProgressionLevelUp;
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

        private void EnsureInteractionPrompt()
        {
            if (interactionPrompt != null)
                return;

            Transform existing = transform.Find("InteractionPrompt");
            if (existing != null)
            {
                interactionPrompt = existing.GetComponent<TextMeshProUGUI>();
                if (interactionPrompt != null)
                    return;
            }

            GameObject promptObject = new GameObject("InteractionPrompt", typeof(RectTransform));
            promptObject.transform.SetParent(transform, false);
            interactionPrompt = promptObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(interactionPrompt);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(interactionPrompt, semiBold: true);
            interactionPrompt.raycastTarget = false;
            interactionPrompt.gameObject.SetActive(false);
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
            interactionPrompt.color = SurvivalPioneerUiPalette.InteractionPromptText;
        }

        public void SetCurrencyHudVisible(bool visible)
        {
            if (piBalanceText == null)
                return;

            piBalanceText.gameObject.SetActive(visible);
            if (visible)
            {
                ConfigurePiBalancePosition();
                RefreshCurrencyHud();
            }
        }

        private void ConfigurePiBalancePosition()
        {
            if (!applyRuntimeHudLayout || piBalanceText == null)
                return;

            RectTransform piRect = piBalanceText.rectTransform;
            piRect.anchorMin = new Vector2(1f, 1f);
            piRect.anchorMax = new Vector2(1f, 1f);
            piRect.pivot = new Vector2(1f, 1f);
            piRect.anchoredPosition = new Vector2(
                -HudLayoutMetrics.RightHudInset,
                -HudLayoutMetrics.TopHudInset);

            piBalanceText.fontSize = Mathf.Max(12f, piBalanceText.fontSize * 0.5f);
            piBalanceText.alignment = TextAlignmentOptions.TopRight;
        }

        private void EnsureCombatUiReady()
        {
            if (combatPopupParent == null)
                combatPopupParent = popupParent != null ? popupParent : transform;

            if (floatingDamagePrefab == null)
            {
                floatingDamagePrefab = Resources.Load<GameObject>("Combat/FloatingDamageNumber");
#if UNITY_EDITOR
                if (floatingDamagePrefab == null)
                {
                    floatingDamagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Project/Prefabs/UI/FloatingDamageNumber.prefab");
                }
#endif
            }
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

        private void EnsureOxygenDeprivationFx()
        {
            if (GetComponent<OxygenDeprivationFx>() == null)
                gameObject.AddComponent<OxygenDeprivationFx>();
        }

        private void EnsureSurvivalPanelBinder()
        {
            ResolveSurvivalUiReferences();

            if (healthSlider == null)
                return;

            Transform current = healthSlider.transform;
            while (current != null && current.name != "SurvivalStatsPanel")
                current = current.parent;

            if (current == null)
                return;

            if (current.GetComponent<SurvivalStatsPanelBinder>() == null)
                current.gameObject.AddComponent<SurvivalStatsPanelBinder>();

            if (current.GetComponent<CondensedSurvivalStatsHud>() == null)
                current.gameObject.AddComponent<CondensedSurvivalStatsHud>();
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
            SetSliderValue(energySlider, survivalStats.CurrentEnergy / survivalStats.maxEnergy);
            SetSliderValue(staminaSlider, survivalStats.CurrentStamina / survivalStats.maxStamina);
            SetSliderValue(oxygenSlider, survivalStats.GetOxygenNormalized());

            if (healthText != null)
                healthText.text = FormatStatValue(survivalStats.CurrentHealth, "Health");
            if (energyText != null)
                energyText.text = FormatStatValue(survivalStats.CurrentEnergy, "Energy");
            if (staminaText != null)
                staminaText.text = FormatStatValue(survivalStats.CurrentStamina, "Stamina");
            if (oxygenText != null)
            {
                if (CondensedSurvivalStatsHud.IsActive)
                    oxygenText.gameObject.SetActive(true);

                oxygenText.text = FormatOxygenValue(survivalStats.CurrentOxygen);
            }
        }

        private void ResolveSurvivalUiReferences()
        {
            Transform panel = GetSurvivalStatsPanelTransform();
            if (panel == null)
                return;

            healthSlider ??= FindRowSlider(panel, "HealthRow");
            energySlider ??= FindRowSlider(panel, "EnergyRow");
            staminaSlider ??= FindRowSlider(panel, "StaminaRow");
            oxygenSlider ??= FindRowSlider(panel, "OxygenRow");

            healthText ??= FindRowLabel(panel, "HealthRow");
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

        private static void SetSliderValue(Slider slider, float normalizedValue)
        {
            if (slider == null)
                return;

            float clamped = Mathf.Clamp01(normalizedValue);
            slider.SetValueWithoutNotify(clamped);

            Transform fillTransform = slider.transform.Find("RingFill");
            if (fillTransform is RectTransform)
            {
                CondensedSurvivalStatsHud.ApplyBarFill(slider, clamped);
                return;
            }

            if (fillTransform != null && fillTransform.TryGetComponent<Image>(out Image fillImage)
                && fillImage.type == Image.Type.Filled
                && fillImage.fillMethod == Image.FillMethod.Horizontal)
            {
                fillImage.fillAmount = clamped;
                return;
            }

            CircularProgressBar progress = slider.GetComponent<CircularProgressBar>();
            if (progress != null)
                progress.UpdateRadialFill(clamped);
        }

        public float GetAetherCredits() => aetherCredits;

        public void SetAetherCredits(float balance)
        {
            aetherCredits = Mathf.Max(0f, balance);
            RefreshCurrencyHud();
        }

        public float GetPiWalletBalance() => piWalletBalance;

        public void SetPiWalletBalance(float balance)
        {
            piWalletBalance = Mathf.Max(0f, balance);
            RefreshCurrencyHud();
        }

        public float GetPiBalance() => piBalance;

        public void SetPiBalance(float balance)
        {
            piBalance = Mathf.Max(0f, balance);
            RefreshCurrencyHud();
        }

        public void ShowAcReward(int amount, string source = "Reward")
        {
            if (amount <= 0)
                return;

            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster != null)
            {
                roster.AddAetherCredits(amount, source);
                return;
            }

            aetherCredits += amount;
            ShowAcRewardPopup(amount, source);
            RefreshCurrencyHud();
        }

        public void ShowAcRewardPopup(int amount, string source = "Reward")
        {
            if (amount <= 0)
                return;

            ShowCurrencyPopup($"+{amount} AC", source);
        }

        public void ShowLevelUpPopup(int newLevel, int skillPointsGained)
        {
            ShowCurrencyPopup(
                $"Level Up! Lv {newLevel}",
                skillPointsGained > 0 ? $"+{skillPointsGained} skill point(s)" : "Level increased");
        }

        public void ShowPiReward(int amount, string source = "Gathering")
        {
            ShowAcReward(amount, source);
        }

        private void RefreshCurrencyHud()
        {
            if (piBalanceText == null || !piBalanceText.gameObject.activeSelf)
                return;

            piBalanceText.text =
                $"Pi Wallet: {Mathf.RoundToInt(piWalletBalance)}  |  AC: {Mathf.RoundToInt(aetherCredits)}";
        }

        private void ShowCurrencyPopup(string amountLine, string source)
        {
            if (piRewardPopupPrefab != null && popupParent != null)
            {
                GameObject popup = Instantiate(piRewardPopupPrefab, popupParent);
                TextMeshProUGUI txt = popup.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                    txt.text = $"{amountLine}\n{source}";

                if (popup.GetComponent<PiRewardPopup>() == null)
                    StartCoroutine(FadeAndDestroyPopup(popup));
            }
        }

        public void ShowInteractionPrompt(string message)
        {
            EnsureInteractionPrompt();
            if (interactionPrompt == null)
                return;

            ApplyInteractionPromptLayout();
            interactionPrompt.text = message;
            interactionPrompt.gameObject.SetActive(true);
            interactionPrompt.transform.SetAsLastSibling();
        }

        public void ShowPetFetchMessage(string itemName)
        {
            ShowInteractionPrompt($"Your fox found: {itemName}!");
            CancelInvoke(nameof(HideInteractionPrompt));
            Invoke(nameof(HideInteractionPrompt), 2.5f);
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FadeAndDestroyPopup(GameObject popup)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(popup);
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void RespawnPlayer()
        {
            Time.timeScale = 1f;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                PlayerDeathHandler deathHandler = player.GetComponent<PlayerDeathHandler>();
                if (deathHandler != null)
                {
                    deathHandler.Respawn();
                    return;
                }
            }

            RestartScene();
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

        public void HideDeathPopup()
        {
            Transform deathPanel = transform.Find("DeathPopupPanel");
            if (deathPanel != null)
                deathPanel.gameObject.SetActive(false);

            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetInventoryOpen(false);
                GameplayAudioUtility.EnsureListenerOnCamera(playerController.GameplayCamera);
            }
        }

        public void ShowDeathPopup()
        {
            Transform existing = transform.Find("DeathPopupPanel");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.SetAsLastSibling();
                WireDeathPopupButtons(existing);
                ConfigurePopupCursor();
                return;
            }

            // Create Death Popup Panel
            GameObject deathPanel = new GameObject("DeathPopupPanel", typeof(RectTransform));
            deathPanel.transform.SetParent(this.transform, false);
            
            RectTransform panelRt = deathPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            Image bgImage = deathPanel.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.72f);
            bgImage.raycastTarget = true;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            // Inner content panel
            GameObject contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
            contentPanel.transform.SetParent(deathPanel.transform, false);
            RectTransform contentRt = contentPanel.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(520f, 320f);
            contentRt.anchoredPosition = Vector2.zero;

            Image contentBg = contentPanel.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(contentBg);
            SurvivalPioneerUiPalette.ApplyPanelShellBackground(contentBg, 0.98f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(contentPanel);

            // Create title text "GAME OVER"
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(contentPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(titleText, bold: true);
            else
                TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.text = "GAME OVER";
            titleText.fontSize = 64f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = SurvivalPioneerUiPalette.WarningText;
            titleText.alignment = TextAlignmentOptions.Center;
            
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.7f);
            titleRt.anchorMax = new Vector2(0.5f, 0.7f);
            titleRt.sizeDelta = new Vector2(600f, 100f);
            titleRt.anchoredPosition = Vector2.zero;

            // Create retry button
            GameObject retryObj = CreateStyledButton(contentPanel.transform, "RetryButton", "RETRY", new Vector2(0f, 20f));
            Button retryBtn = retryObj.GetComponent<Button>();
            retryBtn.onClick.AddListener(RespawnPlayer);

            // Create exit button
            GameObject exitObj = CreateStyledButton(contentPanel.transform, "ExitButton", "END GAME", new Vector2(0f, -60f));
            Button exitBtn = exitObj.GetComponent<Button>();
            exitBtn.onClick.AddListener(QuitGame);

            deathPanel.transform.SetAsLastSibling();
            ConfigurePopupCursor();
        }

        private void WireDeathPopupButtons(Transform deathPanel)
        {
            Button retryBtn = deathPanel.Find("RetryButton")?.GetComponent<Button>();
            if (retryBtn != null)
            {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RespawnPlayer);
            }

            Button exitBtn = deathPanel.Find("ExitButton")?.GetComponent<Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick.RemoveAllListeners();
                exitBtn.onClick.AddListener(QuitGame);
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ConfigurePopupCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.SetInventoryOpen(true);

            CameraController cam = FindAnyObjectByType<CameraController>();
            if (cam != null) cam.SetInventoryOpen(true);
        }

        private GameObject CreateStyledButton(Transform parent, string name, string labelText, Vector2 pos)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.4f);
            rt.anchorMax = new Vector2(0.5f, 0.4f);
            rt.sizeDelta = new Vector2(220f, 50f);
            rt.anchoredPosition = pos;

            Image img = btnObj.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(img);
            img.color = SurvivalPioneerUiPalette.ButtonNormal;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(btnObj);

            Button btn = btnObj.AddComponent<Button>();
            SurvivalPioneerUiPalette.StylePrimaryButton(btn, img);

            // Add text child
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(tmp, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(tmp);
            tmp.text = labelText;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = SurvivalPioneerUiPalette.BodyText;
            tmp.alignment = TextAlignmentOptions.Center;

            return btnObj;
        }

        private void OnDestroy()
        {
            if (trackedProgression != null)
                trackedProgression.OnLevelUp -= HandleProgressionLevelUp;

            if (characterToggleAction != null)
                characterToggleAction.performed -= OnToggleCharacter;

            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;
        }
    }
}